// this uses scary transpiler... not so scary ?

using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using System.Linq;

namespace RDModifications;

[Modification("If extra information should be displayed on the rank screen.")]
public class ExtraLevelEndDetails : Modification
{
	[Configuration<bool>(false, "If Samurai mode should affect the text.")]
    public static ConfigEntry<bool> SamuraiModeAffects;
	
	[Configuration<string>("\\n", 
		"What each line should be seperated by.\n" +
        "(\\n is a new line.)"
	)]
	public static ConfigEntry<string> LineSeparator;

	[Configuration<TextAnchor>(TextAnchor.UpperLeft, "How the text should be aligned.")]
    public static ConfigEntry<TextAnchor> TextAlignment;

	[Configuration<float>(0, "How much the text should be moved to the right (in pixels).")]
    public static ConfigEntry<float> TextOffsetX;

	[Configuration<float>(0, "How much the text should be moved down (in pixels).")]
    public static ConfigEntry<float> TextOffsetY;

	[Configuration<int>(6, "How big the text should be.", [1, int.MaxValue])]
    public static ConfigEntry<int> TextSize;

	[Configuration<int>(60, 
		"How long a line can be before being cut-off.\n" +
        "(To disable the line length limit, set the value to 0 or lower.)"
	)]
    public static ConfigEntry<int> MaxLineLength;

	[Configuration<bool>(true, "If the song name should be displayed.")]
    public static ConfigEntry<bool> IncludeSong;
	[Configuration<string>("Song:", "What the prefix to the song name should be.")]
    public static ConfigEntry<string> SongPrefix;

	[Configuration<bool>(true, "If the song artist should be displayed.")]
    public static ConfigEntry<bool> IncludeArtist;
	[Configuration<string>("Artist:", "What the prefix to the song artist should be.")]
    public static ConfigEntry<string> ArtistPrefix;

	[Configuration<bool>(true, "If the level author should be displayed.")]
    public static ConfigEntry<bool> IncludeAuthor;
	[Configuration<string>("Author:", "What the prefix to the level author should be.")]
    public static ConfigEntry<string> AuthorPrefix;

	[Configuration<bool>(true, "If the amount of hits that the player has hit should be displayed.")]
    public static ConfigEntry<bool> IncludeHits;
	[Configuration<string>("Hits:", "What the prefix to the hit count should be.")]
    public static ConfigEntry<string> HitsPrefix;

	[Configuration<bool>(true, "If the amount of misses (not mistakes) should be displayed.")]
    public static ConfigEntry<bool> IncludeMisses;
	[Configuration<string>("Misses:", "What the prefix to the miss count should be.")]
    public static ConfigEntry<string> MissesPrefix;

	[Configuration<bool>(true, "If the previous best rank should be displayed.")]
    public static ConfigEntry<bool> IncludeBestPrev;
	[Configuration<string>("Previous Best:", "What the prefix to the previous best rank should be.")]
    public static ConfigEntry<string> BestPrevPrefix;

	[Configuration<bool>(true, "If the other enabled modifications (that affect gameplay) should be displayed.")]
    public static ConfigEntry<bool> IncludeModifications;
	[Configuration<string>("Modifications:", "What the prefix to the shown modifications should be.")]
    public static ConfigEntry<string> ModificationsPrefix;

    [HarmonyPatch(typeof(Rankscreen), nameof(Rankscreen.ShowRankDescription))]
    private class LevelDetailsPatch
    {
        public static void Postfix(Rankscreen __instance)
        {
            bool storyLevel = scnGame.levelToLoadSource != LevelSource.ExternalPath;
            string samuraiText = RDString.SamuraiModeText;

            if (Enabled[typeof(CustomSamuraiMode)].Value)
                samuraiText = CustomSamuraiMode.SamuraiReplacement.Value + ":";

            string songName = LevelStatsPatch.songName;
            string songArtist = LevelStatsPatch.songArtist;
            string levelAuthor = LevelStatsPatch.levelAuthor;
            int hits = LevelStatsPatch.storedIfHits.Count((hit) => hit);
            int misses = LevelStatsPatch.storedIfHits.Count - hits;
            string bestPrev = LevelStatsPatch.bestPrev.ToString();
            List<string> baseMods = [];

            if (Enabled[typeof(DoctorMode)].Value)
                baseMods.Add($"DM ({DoctorMode.LowMultiplier.Value}x-{DoctorMode.HighMultiplier.Value}x)");
            if (Enabled[typeof(CustomDifficulty)].Value && (CustomDifficulty.P1Enabled.Value || (GC.twoPlayerMode && CustomDifficulty.P2Enabled.Value)))
                baseMods.Add($"CD ({CustomDifficulty.HitMargin.Value}ms)");
            if (RDTime.speed != 1.0f)
                baseMods.Add($"{RDTime.speed}x");

            string modifications = string.Join(", ", baseMods);
            string textStr = "";

            if (storyLevel)
            {
                bool sam = RDString.samuraiMode;
                RDString.samuraiMode = false;
                // not here
                songName = RDString.Get("levelSelect." + scnGame.internalIdentifier);
                RDString.samuraiMode = sam;
            }
            if (LevelStatsPatch.bestPrev == Rank.NotAvailable || LevelStatsPatch.bestPrev == Rank.NotFinished
            || LevelStatsPatch.bestPrev == Rank.NeverSelected)
                bestPrev = "N/A";

            // this is er hm
            Text text = Object.Instantiate(__instance.description, __instance.description.gameObject.transform.position, Quaternion.identity);
            // unfortunately doesn't make sense without positioning
            text.alignment = TextAlignment.Value;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.fontSize = TextSize.Value;
            // is there a way to modify this better? i'm too lazy to figure out how to though 😁😁 for now atleast
            // raf from the future: one day i say one day but i'm working on bigger things
			// raf from the future squared: today is that day !
            RectTransform rectTransform = text.GetComponent<RectTransform>();
			rectTransform.anchoredPosition = new(352 / 2 + TextOffsetX.Value, 198 / 2 - TextOffsetY.Value);
			rectTransform.sizeDelta = new(350, 196);
            text.transform.SetParent(__instance.description.transform.parent.transform.parent);

            List<object[]> fields = [
                // pretext -- value -- should exist -- should exist in story mode
                [SongPrefix.Value, songName, IncludeSong.Value, true],
                [ArtistPrefix.Value, songArtist, IncludeArtist.Value, false],
                [AuthorPrefix.Value, levelAuthor, IncludeAuthor.Value, false],
                [HitsPrefix.Value, hits.ToString(), IncludeHits.Value, true],
                [MissesPrefix.Value, misses.ToString(), IncludeMisses.Value, true],
                [BestPrevPrefix.Value, bestPrev, IncludeBestPrev.Value, true],
                [ModificationsPrefix.Value, modifications, IncludeModifications.Value, true]
            ];

            Regex colorlessRegex = new(@"<(\/)?color(=("")?([#A-Za-z0-9 ])*("")?)?>", RegexOptions.Multiline);
            foreach (object[] field in fields)
            {
                // if they've disabled it
                if (!(bool)field[2])
                    continue;
                // if it's storymode
                if (!(bool)field[3] && storyLevel)
                    continue;

                // if it's not the first thing that'll be added
                if (textStr.Length > 0)
                    textStr += Regex.Unescape(LineSeparator.Value);

                // simple enough
                string prefix = RDString.samuraiMode && SamuraiModeAffects.Value ? samuraiText : (string)field[0];
                // hm er yeah
                if (prefix.Length > 0)
                    prefix += " ";

                string value = (string)field[1];
                // empty
                if (value.Length == 0)
                    continue;
                value = colorlessRegex.Replace(value, "");
                value = value.Replace("\r", "");
                value = value.Replace("\n", " ");

                string toAdd = prefix + value;
                if (MaxLineLength.Value > 0 && toAdd.Length > MaxLineLength.Value)
                    toAdd = toAdd[..(MaxLineLength.Value - 1)] + "...";

                textStr += toAdd;
            }

            text.font = RDString.GetAppropiateFontForString(textStr);
            text.text = textStr;
        }
    }

    private class LevelStatsPatch
    {
        public static string songName = "";
        public static string songArtist = "";
        public static string levelAuthor = "";
        public static Rank bestPrev;
        public static bool isEditor = false;
        public static List<bool> storedIfHits = [];

        [HarmonyPostfix]
        [HarmonyPatch(typeof(scnGame), nameof(scnGame.AddHitOffset))]
        public static void TrackHitInfo(scnGame __instance, int rowID, OffsetType offsetType)
        {
            if (__instance.rows[rowID].cpuControlled)
                return;
            storedIfHits.Add(offsetType == OffsetType.Perfect);
        }

        // rest of info
        [HarmonyPostfix]
        [HarmonyPatch(typeof(scnGame), "Start")]
        public static void ResetOnStart(scnGame __instance)
        {
            scnGame game = __instance;

            // init
            storedIfHits.Clear();
            songName = "";
            songArtist = "";
            levelAuthor = "";
            isEditor = game.editorMode;

            bool customLevel = scnGame.levelToLoadSource == LevelSource.ExternalPath;
            bestPrev = isEditor ? Rank.NotAvailable : Persistence.GetLevelRank(game.levelIdentifier);

            if (!customLevel)
                return;
            // local function :grin:
            static string notDefined(string thing)
                => (thing == null || thing.Length <= 0) ? "Not Defined" : thing;

            songName = notDefined(game.currentLevel.data.settings.song);
            songArtist = notDefined(game.currentLevel.data.settings.artist);
            levelAuthor = notDefined(game.currentLevel.data.settings.author);
            if (!isEditor) // hash will be exists! -- unless legacy
            {
                // og hashing system;
                string hash = RDUtils.GetHash(new DirectoryInfo(Path.GetDirectoryName(scnGame.currentLevelPath)).Name);
                if (scnCLS.CachedData.levelFileData != null)
                    hash = scnCLS.CachedData.levelFileData.hash;

                bestPrev = Persistence.GetCustomLevelRank(hash, RDTime.speed);
            }
        }

    }

    [HarmonyPatch(typeof(scnGame), "Start")]
    private class FixScnGamePatch
    {
        // It seems bepinex runs scnGame.Start under this assembly which when paired with GetType causes a few issues
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo concatFunc = AccessTools.Method("System.String:Concat", [typeof(string), typeof(string)]);
            
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Ldstr, "Level_"))
                .MatchForward(false, new CodeMatch(OpCodes.Call, concatFunc)) // should be concat
                .InsertAndAdvance([
                    new(OpCodes.Ldstr, ", " + typeof(scnGame).Assembly.GetName()),
                    new(OpCodes.Call, concatFunc)
                ])
                .InstructionEnumeration();
        }
    }
}

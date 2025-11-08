// this uses scary transpiler... not so scary ?

using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using System.Linq;

namespace RDModifications;

[Modification]
public class ExtraLevelEndDetails
{
    public static ConfigEntry<bool> enabled;
    public static ConfigEntry<bool> samuraiModeAffects;
    public static ConfigEntry<string> lineSeperator;
    public static ConfigEntry<TextAnchor> textAlignment;
    public static ConfigEntry<int> textSize;
    // public static ConfigEntry<List<float>> textPosition;
    public static ConfigEntry<int> maxLineLength;

    public static ConfigEntry<bool> includeSong;
    public static ConfigEntry<string> songPart;

    public static ConfigEntry<bool> includeArtist;
    public static ConfigEntry<string> artistPart;

    public static ConfigEntry<bool> includeAuthor;
    public static ConfigEntry<string> authorPart;

    public static ConfigEntry<bool> includeHits;
    public static ConfigEntry<string> hitsPart;

    public static ConfigEntry<bool> includeMisses;
    public static ConfigEntry<string> missesPart;

    public static ConfigEntry<bool> includeBestPrev;
    public static ConfigEntry<string> bestPrevPart;

    public static ConfigEntry<bool> includeModifications;
    public static ConfigEntry<string> modificationsPart;

    public static ManualLogSource logger;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("ExtraLevelEndDetails", "Enabled", false,
        "Whether extra information should be displayed on the rank screen.");

        samuraiModeAffects = config.Bind("ExtraLevelEndDetails", "SamuraiModeAffects", false,
        "Whether Samurai mode should affect the text.");

        lineSeperator = config.Bind("ExtraLevelEndDetails", "LineSeperator", "\\n",
        "What each line should be seperated by.\n" +
        "(\\n is a new line.)");

        textAlignment = config.Bind("ExtraLevelEndDetails", "TextAlignment", TextAnchor.UpperLeft,
        "How the text should be aligned.");

        textSize = config.Bind("ExtraLevelEndDetails", "TextSize", 6,
        "How big the text should be.");

        maxLineLength = config.Bind("ExtraLevelEndDetails", "MaxLineLength", 60,
        "How long a line can be before being cut-off.\n" +
        "(To disable the line length limit, set the value to 0 or lower.)");


        includeSong = config.Bind("ExtraLevelEndDetails", "IncludeSong", true, "If the song name should be displayed.");
        songPart = config.Bind("ExtraLevelEndDetails", "SongPart", "Song:", "What the prefix to the song name should be.");

        includeArtist = config.Bind("ExtraLevelEndDetails", "IncludeArtist", true, "If the song artist should be displayed.");
        artistPart = config.Bind("ExtraLevelEndDetails", "ArtistPart", "Artist:", "What the prefix to the song artist should be.");

        includeAuthor = config.Bind("ExtraLevelEndDetails", "IncludeAuthor", true, "If the level author should be displayed.");
        authorPart = config.Bind("ExtraLevelEndDetails", "AuthorPart", "Author:", "What the prefix to the level author should be.");

        includeHits = config.Bind("ExtraLevelEndDetails", "IncludeHits", true, "If the amount of hits that the player has hit should be displayed.");
        hitsPart = config.Bind("ExtraLevelEndDetails", "HitsPart", "Hits:", "What the prefix to the hit count should be.");

        includeMisses = config.Bind("ExtraLevelEndDetails", "IncludeMisses", true, "If the amount of misses (not mistakes) should be displayed.");
        missesPart = config.Bind("ExtraLevelEndDetails", "MissesPart", "Misses:", "What the prefix to the miss count should be.");

        includeBestPrev = config.Bind("ExtraLevelEndDetails", "IncludeBestPrev", true, "If the (previous) best rank should be displayed.");
        bestPrevPart = config.Bind("ExtraLevelEndDetails", "BestPrevPart", "Previous Best:", "What the prefix to the (previous) best rank should be.");

        includeModifications = config.Bind("ExtraLevelEndDetails", "IncludeModifications", true, "If the other enabled modifications (that affect gameplay) should be displayed.");
        modificationsPart = config.Bind("ExtraLevelEndDetails", "ModificationsPart", "Modifications:", "What the prefix to the shown modifications should be.");

        if (textSize.Value <= 0)
        {
            textSize.Value = 10;
            logger.LogWarning("ExtraLevelEndDetails: Invalid TextSize, resetting back to 10.");
        }
        return enabled.Value;
    }

    [HarmonyPatch(typeof(HUD), nameof(HUD.ShowRankDescription))]
    private class LevelDetailsPatch
    {
        public static void Postfix(HUD __instance)
        {
            bool storyLevel = scnGame.levelToLoadSource != LevelSource.ExternalPath;
            string samuraiText = RDString.SamuraiModeText;

            if (CustomSamuraiMode.enabled.Value)
                samuraiText = CustomSamuraiMode.samuraiReplacement.Value + ":";

            string songName = LevelStatsPatch.songName;
            string songArtist = LevelStatsPatch.songArtist;
            string levelAuthor = LevelStatsPatch.levelAuthor;
            int hits = LevelStatsPatch.storedHitInfos.Values.Count((type) => type == OffsetType.Perfect);
            int misses = (LevelStatsPatch.storedHitInfos.Count - hits);
            string bestPrev = LevelStatsPatch.bestPrev.ToString();
            List<string> baseMods = [];

            if (DoctorMode.enabled.Value)
                baseMods.Add($"DM ({DoctorMode.lowMult.Value}x-{DoctorMode.highMult.Value}x)");
            if (CustomDifficulty.p1Enabled.Value || (GC.twoPlayerMode && CustomDifficulty.p2Enabled.Value))
                baseMods.Add($"CD ({CustomDifficulty.hitMargin.Value}ms)");
            if (RDTime.speed != 1.0f)
                baseMods.Add($"{RDTime.speed}x");

            // if (PretendFOnMistake.enabled.Value)
            //     baseMods.Add($"PFOM ({PretendFOnMistake.rankToDisplayAndSay.Value})");
            // if (RDString.samuraiMode && CustomSamuraiMode.enabled.Value)
            //     baseMods.Add($"CSM ({CustomSamuraiMode.samuraiReplacement.Value})");

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
            text.alignment = TextAnchor.UpperLeft;//textAlignment.Value;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.fontSize = textSize.Value;
            // is there a way to modify this better? i'm too lazy to figure out how to though 😁😁 for now atleast
            // raf from the future: one day i say one day but i'm working on bigger things
            text.gameObject.transform.position = new(141, 174, __instance.description.transform.position.z);
            text.transform.SetParent(__instance.description.transform.parent.transform.parent);

            List<object[]> fields = [
                // pretext -- value -- should exist -- should exist in story mode
                [songPart.Value, songName, includeSong.Value, true],
                [artistPart.Value, songArtist, includeArtist.Value, false],
                [authorPart.Value, levelAuthor, includeAuthor.Value, false],
                [hitsPart.Value, hits.ToString(), includeHits.Value, true],
                [missesPart.Value, misses.ToString(), includeMisses.Value, true],
                [bestPrevPart.Value, bestPrev, includeBestPrev.Value, true],
                [modificationsPart.Value, modifications, includeModifications.Value, true]
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
                    textStr += Regex.Unescape(lineSeperator.Value);

                // simple enough
                string prefix = RDString.samuraiMode && samuraiModeAffects.Value ? samuraiText : (string)field[0];
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
                if (maxLineLength.Value > 0 && toAdd.Length > maxLineLength.Value)
                    toAdd = toAdd[..(maxLineLength.Value - 1)] + "...";

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
        public static Dictionary<double, OffsetType> storedHitInfos = [];

        [HarmonyPostfix]
        [HarmonyPatch(typeof(scnGame), nameof(scnGame.AddHitOffset))]
        public static void TrackHitInfo(scnGame __instance, OffsetType offsetType)
            => storedHitInfos[__instance.conductor.audioPos] = offsetType;

        // rest of info
        [HarmonyPostfix]
        [HarmonyPatch(typeof(scnGame), "Start")]
        public static void ResetOnStart(scnGame __instance)
        {
            scnGame game = __instance;

            // init
            storedHitInfos.Clear();
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

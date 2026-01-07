// this uses scary transpiler... not so scary ?

using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RDLevelEditor;
using System.Reflection;
using System;
using System.Text.RegularExpressions;

namespace RDModifications;

[Modification("If Samurai. mode to have your own custom text.")]
public class CustomSamuraiMode : Modification
{
	[Configuration<bool>(false, "If Samurai. mode should be enabled by default when playing the game.")]
    public static ConfigEntry<bool> ModeEnabledAtStart;
	
	[Configuration<bool>(false, "If Samurai. mode should replace the rank text.")]
    public static ConfigEntry<bool> ReplaceRank;

	[Configuration<string>("Insomniac.", "What 'Samurai.' should be replaced with.")]
    public static ConfigEntry<string> SamuraiReplacement;

	[Configuration<string>("Insomniac.", 
		"What you need to input for Samurai. mode to be toggled.\n" +
        "The BepinEx console (may only apply to BepinEx 6, unsure) will output the inputs needed, as it may not be obvious at times."
	)]
    public static ConfigEntry<string> SamuraiInputReplacement;

    [HarmonyPatch(typeof(RDString), nameof(RDString.Setup))]
    private class SamuraiModeStartPatch
    {
        public static void Postfix()
            => RDString.samuraiMode = ModeEnabledAtStart.Value;
    }

    private class SamuraiTextPatch
    {
        public static IEnumerable<MethodInfo> TargetMethods()
        {
            List<MethodInfo> methods = [];
            // This sucks
            Type[] makeLyricsTypes = [typeof(string), typeof(TextFont), typeof(Vector2), typeof(int), typeof(float), typeof(Color),
            typeof(int), typeof(int), typeof(float), typeof(bool), typeof(Color), typeof(TextAnchor), typeof(bool), typeof(bool)];
            Type[] makeLyricsTypesNonBeta = [typeof(string), typeof(Vector2), typeof(int), typeof(float), typeof(Color),
            typeof(int), typeof(int), typeof(float), typeof(bool), typeof(Color), typeof(TextAnchor), typeof(bool), typeof(bool)];

            yield return AccessTools.Method(typeof(RDString), nameof(RDString.Get));
            yield return AccessTools.Method(typeof(LyricsGame), nameof(LyricsGame.AdvanceText));

            // compiler generated
            yield return AccessUtils.GetFirstMethodContains(typeof(LevelEvent_TextExplosion), "<Run>");

            // two functions with same name so we need to get this really specific one
			MethodInfo makeLyrics = AccessTools.Method(typeof(scrVfxControl), nameof(scrVfxControl.MakeLyrics), makeLyricsTypes);
			if (makeLyrics == null)
				makeLyrics = AccessTools.Method(typeof(scrVfxControl), nameof(scrVfxControl.MakeLyrics), makeLyricsTypesNonBeta);
            yield return makeLyrics;
			 // due to compiled IEnumerable
            yield return AccessUtils.GetFirstInnerMethodContains(typeof(RDInk), "<Say>", "MoveNext");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SamuraiTranspiler(IEnumerable<CodeInstruction> instructions)
            // This is actually quite useful !
            => TranspilerUtils.ReplaceString(instructions, "Samurai.", SamuraiReplacement.Value);
    }
    [HarmonyPatch(typeof(Rankscreen), nameof(Rankscreen.ShowAndSaveRank))]
    private class SamuraiRankPatch
    {
        public static void Postfix(Rankscreen __instance)
        {
            if (RDString.samuraiMode && ReplaceRank.Value)
                __instance.rank.text = SamuraiReplacement.Value;
        }
    }

    [HarmonyPatch(typeof(scnBase), "Start")]
    private class SamuraiInputPatch
    {
        public static bool hasLogged = false;

        public static void Postfix(ref RDCheatCode.CheatCode ___samuraiModeCheat)
        {
            List<KeyCode> inputs = [];
            string symbolsNeedingShift = ":<>?@{}!$%^&*()_+|";
            // bad code i think
            Dictionary<string, string> symbolsToNames = new(){
                {";", "Semicolon"}, {":", "Semicolon"},
                {",", "Comma"}, {"<", "Comma"},
                {".", "Period"}, {">", "Period"},
                {"/", "Slash"}, {"?", "Slash"},
                {"[", "LeftBracket"}, {"{", "LeftBracket"},
                {"]", "RightBracket"}, {"}", "RightBracket"},
                {"\\", "Backslash"}, {"|", "Backslash"},
                {"-", "Minus"}, {"_", "Minus"},
                {"=", "Equals"}, {"+", "Equals"},
                // number shifts
                {"!", "Alpha1"}, {"£", "Alpha3"}, {"$", "Alpha4"},
                {"%", "Alpha5"}, {"^", "Alpha6"}, {"&", "Alpha7"},
                {"*", "Alpha8"}, {"(", "Alpha9"}, {")", "Alpha0"},
                // singles mostly due to american vs uk (i'm uk but obviously others aren't)
                // one due to no shift for it
                {"`", "BackQuote"}, {"'", "Quote"}, {" ", "Space"}
            };

            string input = SamuraiInputReplacement.Value;
            string upperInput = input.ToUpper();
            string lowerInput = input.ToLower();
            string logOutput = "";
            for (int i = 0; i < input.Length; i++)
            {
                char letter = input[i];
                bool isNumber = new Regex("[0-9]").IsMatch(letter.ToString());
                string enumGet = upperInput[i].ToString();
                bool shiftNeeded = upperInput[i] == input[i] && upperInput[i] != lowerInput[i];
                shiftNeeded |= symbolsNeedingShift.Contains(letter);

                if (symbolsToNames.TryGetValue(letter.ToString(), out string name))
                    enumGet = name;

                if (isNumber)
                    enumGet = "Alpha" + letter;

                if (!Enum.TryParse(typeof(KeyCode), enumGet, out object key))
                    continue;

                // uppercase shift
                if (shiftNeeded)
                {
                    if (!hasLogged)
                        logOutput += "LShift ";
                    inputs.Add(KeyCode.LeftShift);
                }
                inputs.Add((KeyCode)key);

                // no need
                if (hasLogged)
                    continue;

                // Alpha0 might be harder to understand than 0
                string logAdd = enumGet.Replace("Alpha", "");
                // handles symbols e.g. Period => .
                string baseKey = symbolsToNames.FirstOrDefault(kvp => kvp.Value == enumGet).Key;
                if (baseKey != null)
                    logAdd = baseKey;
                // Adds them
                logOutput += logAdd;
                if (i < input.Length - 1 && logAdd.Length > 1)
                    logOutput += " ";
            }

            if (inputs.Count > 0)
            {
                if (inputs.Count <= 2)
                    Log.LogWarning("CustomSamuraiMode: The input length for toggling Samurai mode is quite short, this may lead to slip-ups.");
                RDCheatCode.CheatCode newCheatCode = new(inputs.ToArray());
                ___samuraiModeCheat = newCheatCode;
            }
            if (hasLogged)
                return;

            if (logOutput.Length > 0)
                Log.LogMessage($"CustomSamuraiMode: Input '{logOutput}' to toggle Samurai mode.");
            else if (!hasLogged)
                Log.LogWarning("CustomSamuraiMode: SamuraiInputReplacement as KeyCode[] is empty, 'Samurai.' is still needed to be inputted.");

            hasLogged = true;
        }
    }
}
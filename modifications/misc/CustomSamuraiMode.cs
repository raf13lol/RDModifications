// this uses scary transpiler... not so scary ?

using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RDLevelEditor;
using System.Reflection;
using System;
using System.Text.RegularExpressions;

namespace RDModifications;

[Modification]
public class CustomSamuraiMode
{
    public static ConfigEntry<bool> enabled;
    public static ConfigEntry<bool> modeEnabledAtStart;
    public static ConfigEntry<bool> replaceRank;
    public static ConfigEntry<string> samuraiReplacement;
    public static ConfigEntry<string> samuraiInputReplacement;

    public static ManualLogSource logger;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("CustomSamuraiMode", "Enabled", false, 
        "This will change Samurai mode to have your own custom text.");

        modeEnabledAtStart = config.Bind("CustomSamuraiMode", "ModeEnabledAtStart", false, 
        "If Samurai mode should be enabled by default when playing the game.");

        replaceRank = config.Bind("CustomSamuraiMode", "ReplaceRank", false, 
        "If Samurai mode should replace the rank text.");

        samuraiReplacement = config.Bind("CustomSamuraiMode", "SamuraiReplacement", "Insomniac.", 
        "What 'Samurai.' should be replaced with.");

        samuraiInputReplacement = config.Bind("CustomSamuraiMode", "SamuraiInputReplacement", "Insomniac.",
        "What you need to input for Samurai to be toggled.\n" +
        "The BepinEx log will output the inputs needed, as it may not be obvious at times.");

        return enabled.Value;
    }

    [HarmonyPatch(typeof(RDString), nameof(RDString.Setup))]
    private class SamuraiModeStartPatch
    {
        public static void Postfix()
            => RDString.samuraiMode = modeEnabledAtStart.Value;
    }

    private class SamuraiTextPatch
    {
        public static IEnumerable<MethodInfo> TargetMethods()
        {
            List<MethodInfo> methods = [];
            // This sucks
            Type[] makeLyricsTypes = [typeof(string), typeof(Vector2), typeof(int), typeof(float), typeof(Color),
            typeof(int), typeof(int), typeof(float), typeof(bool), typeof(Color), typeof(TextAnchor), typeof(bool), typeof(bool)];

            methods.Add(AccessUtils.GetMethodCalled(typeof(RDString), nameof(RDString.Get)));
            methods.Add(AccessUtils.GetMethodCalled(typeof(LyricsGame), nameof(LyricsGame.AdvanceText)));

            // compiler generated
            methods.Add(AccessUtils.GetMethodContains(typeof(LevelEvent_TextExplosion), "<Run>"));
            // two functions with same name so we need to get this really specific one
            methods.Add(typeof(scrVfxControl).GetMethod(nameof(scrVfxControl.MakeLyrics), BindingFlags.Public | BindingFlags.Instance, null, makeLyricsTypes, null));

            return methods.AsEnumerable();
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SamuraiTranspiler(IEnumerable<CodeInstruction> instructions)
        { 
            // This is actually quite useful !
            return TranspilerUtils.ReplaceString(instructions, "Samurai.", samuraiReplacement.Value);
        }
    }
    private class DoubleSamuraiPatch
    {
        public static MethodInfo TargetMethod()
        {
            // This is Stupid
            // i believe it's due to compiled IEnumerable ?
            // the IL code looks like it
            Type type = AccessTools.FirstInner(typeof(RDInk), t => t.Name.Contains("<Say>"));
            return AccessUtils.GetInnerMethodContains(typeof(RDInk), "<Say>", "MoveNext");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DoubleSamuraiTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            // This is actually quite useful !
            return TranspilerUtils.ReplaceString(instructions, "Samurai.", samuraiReplacement.Value, 2);
        }
    }

    [HarmonyPatch(typeof(HUD), nameof(HUD.ShowAndSaveRank))]
    private class SamuraiRankPatch
    {
        public static void Postfix(HUD __instance)
        {
            if (RDString.samuraiMode && replaceRank.Value)
                __instance.rank.text = samuraiReplacement.Value;
        }
    }

    [HarmonyPatch(typeof(scnBase), "Start")]
    private class SamuraiInputPatch
    {
        public static bool hasLogged = false;

        public static void Postfix(scnBase __instance)
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
                {"!", "Alpha1"}, {"$", "Alpha4"}, {"%", "Alpha5"}, {"^", "Alpha6"},
                {"&", "Alpha7"}, {"*", "Alpha8"}, {"(", "Alpha9"}, {")", "Alpha0"},
                // singles mostly due to american vs uk (i'm uk but obviously others aren't)
                // one due to no shift for it
                {"`", "BackQuote"}, {"'", "Quote"}, {" ", "Space"}
            };

            string input = samuraiInputReplacement.Value;
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
                    logger.LogWarning("CustomSamuraiMode: The input length for toggling Samurai mode is quite short, this may lead to slip-ups.");
                RDCheatCode.CheatCode newCheatCode = new(inputs.ToArray());
                // private reflection stuff... whatever
                typeof(scnBase).GetField("samuraiModeCheat", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(__instance, newCheatCode);
            }
            if (hasLogged)
                return;

            if (logOutput.Length > 0)
                logger.LogMessage($"CustomSamuraiMode: Input '{logOutput}' to toggle Samurai mode.");
            else if (!hasLogged)
                logger.LogWarning("CustomSamuraiMode: SamuraiInputReplacement as KeyCode[] is empty, 'Samurai.' is still needed to be inputted.");

            hasLogged = true;
        }
    }
}
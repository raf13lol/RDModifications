using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace RDModifications
{
    public class DoctorMode
    {
        public static ConfigEntry<bool> enabled;
        public static ConfigEntry<float> lowMult;
        public static ConfigEntry<float> highMult;
        public static ConfigEntry<bool> auto;

        public static ManualLogSource logger;

        public static void Init(Harmony patcher, ConfigFile config, ManualLogSource logging, ref bool anyEnabled)
        {
            logger = logging;

            enabled = config.Bind("DoctorMode", "Enabled", false,
            "Doctor mode is where the mod will completely destroy the rhythm engine.\n" +
            "It destroys the rhythm engine by multipling the 'crotchet' by a random value between 2 bounds.\n" +
            "With these settings, you can configure how much to destroy it by modifying these 2 bounds.");

            lowMult = config.Bind("DoctorMode", "LowMultipler", 0.75f, "The lowest multipler to use in the random multipler.");
            highMult = config.Bind("DoctorMode", "HighMultipler", 1.25f, "The highest multipler to use in the random multipler.");
            auto = config.Bind("DoctorMode", "Auto", false, "If the songs should be played automatically. Only applies to Doctor mode. (NO RANKS NOR ANY ACHIEVEMENTS WILL BE SAVED)");

            if (enabled.Value)
            {
                if (lowMult.Value == 1.00f && highMult.Value == 1.00f)
                {
                    logger.LogWarning("DoctorMode: LowMultipler and HighMultipler are both 1.00. No Doctor mode patches will be applied.");
                    return;
                }
                if (lowMult.Value > highMult.Value)
                {
                    lowMult.Value = highMult.Value;
                    logger.LogWarning("DoctorMode: LowMultipler was greater than HighMultipler. LowMultipler has been set to HighMultipler.");
                }
                patcher.PatchAll(typeof(ConductorPatch));
                patcher.PatchAll(typeof(TitlescreenPatch));
                if (auto.Value)
                {
                    patcher.PatchAll(typeof(AutoPatch));
                    patcher.PatchAll(typeof(RanksAchievementsPatch));
                }

                anyEnabled = true;
            }
        }

        private class ConductorPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(scrConductor), nameof(scrConductor.crotchet), MethodType.Getter)]
            public static void GetCrotchetPostfix(ref float __result)
            {
                __result *= Random.Range(lowMult.Value, highMult.Value);
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(scrConductor), nameof(scrConductor.BeatToTime))]
            public static IEnumerable<CodeInstruction> BeatToTimeTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                CodeInstruction[] randMult = [
                    new CodeInstruction(OpCodes.Ldc_R4, lowMult.Value),
                    new CodeInstruction(OpCodes.Ldc_R4, highMult.Value),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method("UnityEngine.Random:Range", [typeof(float), typeof(float)])),
                    new CodeInstruction(OpCodes.Mul)
                ];

                // twice?! ... okay then :pensive:
                return new CodeMatcher(instructions)
                    // need to Ignore the first two `mul`'s so
                    
                    // sets it to the max via the Idiotproofing (oob)
                    .Advance(500000)
                    // goes backwards to the last one before ret
                    .MatchBack(false, new CodeMatch(OpCodes.Mul))
                    .InsertAndAdvance(randMult)
                    // back to where we just were
                    .MatchBack(false, new CodeMatch(OpCodes.Mul))
                    // go past that (idk if we can match on the same thing sooo)
                    .Advance(-5)
                    // To the next one after
                    .MatchBack(false, new CodeMatch(OpCodes.Mul))
                    .InsertAndAdvance(randMult)
                    // done
                    .InstructionEnumeration();
            }
        }

        [HarmonyPatch(typeof(scnMenu), "Start")]
        private class TitlescreenPatch
        {
            public static void Postfix(scnMenu __instance)
            {
                __instance.logo.rhythm.image.color = new Color(0, 0, 0, 0);
                __instance.logo.rhythmChinese.image.color = new Color(0, 0, 0, 0);
            }
        }

        private class AutoPatch
        {
            // This is how Otto in the editor works so Do not strike me down mods
            [HarmonyPostfix]
            [HarmonyPatch(typeof(DebugSettings), nameof(DebugSettings.Auto), MethodType.Getter)]
            public static void AutoHitPostfix(ref bool __result)
            {
                // this code is only ran if auto.Value 
                __result = true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HUD), nameof(HUD.ShowAndSaveRank))]
            public static void HUDPostfix(HUD __instance)
            {
                // should be safe to do so
                if (__instance.rank.text.Length > 0)
                    __instance.rank.text += "?";
                if (__instance.customText.text.Length > 0)
                    __instance.customText.text += "?";
            }
        }

        private class RanksAchievementsPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(SteamIntegration), nameof(SteamIntegration.UnlockAchievement), [typeof(string), typeof(bool)])]
            public static bool AchievementPrefix()
            {
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Persistence), nameof(Persistence.SetLevelRank), [typeof(string), typeof(Rank), typeof(bool), typeof(bool)])]
            public static bool RankPrefix()
            {
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Persistence), nameof(Persistence.SetCustomLevelRank), [typeof(string), typeof(Rank), typeof(float)])]
            public static bool CustomRankPrefix(ref Rank rank)
            {
                return rank == -1;
            }

            // Score is really interesting!
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Persistence), nameof(Persistence.SetLevelScore))]
            public static bool ScorePrefix()
            {
                return false;
            }
        }
    }
}
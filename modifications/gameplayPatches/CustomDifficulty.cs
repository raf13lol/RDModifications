using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RDModifications
{
    [Modification]
    public class CustomDifficulty
    {
        public static ManualLogSource logger;

        public static ConfigEntry<bool> p1Enabled;
        public static ConfigEntry<bool> p2Enabled;
        public static ConfigEntry<float> hitMargin;
        public static ConfigEntry<string> name;
        public static ConfigEntry<bool> hitStripWidthLimiting;

        public static bool Init(ConfigFile config, ManualLogSource logging)
        {
            logger = logging;
            p1Enabled = config.Bind("CustomDifficulty", "P1Enabled", false, 
            "Enabling this will make P1 use the custom hit margin defined.");
            p2Enabled = config.Bind("CustomDifficulty", "P2Enabled", false, 
            "Enabling this will make P2 use the custom hit margin defined.");

            hitMargin = config.Bind("CustomDifficulty", "HitMargin", 25f, 
            "How many milliseconds there should be to hit. (e.g. -25ms to +25ms)");

            name = config.Bind("CustomDifficulty", "Name", "Custom", 
            "What the difficulty should be called in the options menu.");

            hitStripWidthLimiting = config.Bind("CustomDifficulty", "HitStripWidthLimiting", true,
            "If the Hit Strip width should be limited.");

            if (hitMargin.Value <= 0f)
            {
                hitMargin.Value = 25f;
                logger.LogWarning("CustomDifficulty: Invalid value for HitMargin, resetted back to 25ms.");
            }
            return p1Enabled.Value || p2Enabled.Value;
        }

        private class DifficultyPatch
        {
            public static IEnumerable<MethodInfo> TargetMethods()
            {
                List<MethodInfo> methods = [];
                methods.Add(AccessUtils.GetMethodCalled(typeof(scnGame), nameof(scnGame.GetHitMargin)));
                methods.Add(AccessUtils.GetMethodCalled(typeof(scnGame), nameof(scnGame.GetReleaseMargin)));
                return methods.AsEnumerable();
            }

            [HarmonyPrefix]
            public static bool HitRleasePrefix(ref float __result, RDPlayer player)
            {
                if ((player != RDPlayer.P2 && p1Enabled.Value)
                || (player == RDPlayer.P2 && p2Enabled.Value))
                {
                    __result = marginMult(hitMargin.Value / 1000);
                    return false;
                }
                return true;
            }

            private static float marginMult(float input)
            {
                float ret = input;
                if (scnGame.instance != null && scnGame.instance.currentLevel != null)
                {
                    float mult = scnGame.instance.currentLevel.hitMarginMultiplier;
                    if (mult > 0f)
                        ret *= mult;
                }
                return ret;
            }
        }

        private class ButtonHitStripPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(scnGame), "Awake")]
            public static void GameAwakePostfix()
            {
                if (p1Enabled.Value)
                    scnGame.p1DefibMode = defibViaHitMargins();
                if (p2Enabled.Value)
                    scnGame.p2DefibMode = defibViaHitMargins();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(RDHitStrip), "Setup")]
            public static void HitstripSetup(ref RDHitStrip __result, RDPlayer player)
            {
                if (!(player == RDPlayer.P1 && p1Enabled.Value)
                && !(player == RDPlayer.P2 && p2Enabled.Value))
                    return;

                float hitmar = hitMargin.Value;
                float width;
                if (hitmar >= 80f)
                {
                    // forced easy-unmissable width
                    width = 39f;
                    if (hitmar > 400f)
                        width += (hitmar - 400f) / 25f;
                }
                else if (hitmar > 40f && hitmar < 80f)
                    width = 11f + (13f * ((hitmar - 40f) / 40f));
                else
                {
                    float baseVal = hitStripWidthLimiting.Value ? 8f : 1f;
                    width = baseVal + ((11f - baseVal) * (hitmar / 40f));
                }
                if (hitStripWidthLimiting.Value && width > 50f)
                    width = 50f;

                // stupid reflection shit for private var
                __result.GetType()
                .GetField("width", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(__result, (int)Math.Round(width));
            }

            private static DefibMode defibViaHitMargins()
            {
                float hitmar = hitMargin.Value;
                if (hitmar <= 40f)
                    return DefibMode.Hard;
                if (hitmar <= 80f)
                    return DefibMode.Normal;
                if (hitmar <= 120f)
                    return DefibMode.Easy;
                if (hitmar <= 200f)
                    return DefibMode.VeryEasy;
                return DefibMode.Unmissable;
            }
        }

        private class SettingsPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PauseModeContentArrows), nameof(PauseModeContentArrows.ChangeContentValue))]
            public static bool ContentValuePrefix(PauseModeContentArrows __instance)
            {
                if ((__instance.contentData.name == PauseContentName.DefibrillatorP1 && p1Enabled.Value)
                || (__instance.contentData.name == PauseContentName.DefibrillatorP2 && p2Enabled.Value))
                    return false;
                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PauseModeContentArrows), nameof(PauseModeContentArrows.UpdateValue))]
            public static bool UpdateValuePrefix(PauseModeContentArrows __instance)
            {
                if ((__instance.contentData.name == PauseContentName.DefibrillatorP1 && p1Enabled.Value)
                || (__instance.contentData.name == PauseContentName.DefibrillatorP2 && p2Enabled.Value))
                {
                    __instance.valueText.text = name.Value;

                    // misc stuff
                    PauseMenuMode.CheckCJKText(__instance.valueText);
                    __instance.rightArrow.rect.gameObject.SetActive(false);
                    __instance.leftArrow.rect.gameObject.SetActive(false);
                    return false;
                }
                return true;
            }
        }
    }

}
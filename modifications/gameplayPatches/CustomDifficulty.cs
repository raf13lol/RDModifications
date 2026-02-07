using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace RDModifications;

[Modification("If the hit margins for P1/P2 should be customisable.")]
public class CustomDifficulty : Modification 
{
	[Configuration<bool>(false, "If P1 should use the custom hit margin defined.")]
    public static ConfigEntry<bool> P1Enabled;
	[Configuration<bool>(false, "If P2 should use the custom hit margin defined.")]
    public static ConfigEntry<bool> P2Enabled;

	[Configuration<float>(25f, "How many milliseconds wide the hit margin should be. (e.g. -25ms to +25ms)", [float.Epsilon, float.PositiveInfinity])]
    public static ConfigEntry<float> HitMargin;

	[Configuration<string>("Very Hard", "What the difficulty should be called in the options menu.")]
    public static ConfigEntry<string> Name;

	[Configuration<bool>(true, "If the width of the hit strip should be limited.")]
    public static ConfigEntry<bool> HitStripWidthLimiting;

	public static bool Init()
		=> P1Enabled.Value || P2Enabled.Value;

    private class DifficultyPatch
    {
        [HarmonyPrefix]
		[HarmonyPatch(typeof(scnGame), nameof(scnGame.GetHitMargin))]
        public static bool HitPrefix(ref float __result, RDPlayer player)
        {
            if ((player != RDPlayer.P2 && P1Enabled.Value)
            || (player == RDPlayer.P2 && P2Enabled.Value))
            {
                __result = marginMult(HitMargin.Value / 1000f);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
		[HarmonyPatch(typeof(scnGame), nameof(scnGame.GetReleaseMargin))]
        public static bool ReleasePrefix(ref float __result, RDPlayer player)
        {
            if ((player != RDPlayer.P2 && P1Enabled.Value)
            || (player == RDPlayer.P2 && P2Enabled.Value))
            {
                __result = marginMult(Mathf.Clamp(HitMargin.Value / 1000f, 0.08f, 0.4f));
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
            if (P1Enabled.Value)
                scnGame.p1DefibMode = defibViaHitMargins();
            if (P2Enabled.Value)
                scnGame.p2DefibMode = defibViaHitMargins();
        }

        public static RDPlayer player = RDPlayer.CPU;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(RDHitStrip), nameof(RDHitStrip.SetPlayer))]
        public static void HitstripPlayerDetect(RDPlayer player)
            => ButtonHitStripPatch.player = player;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RDHitStrip), nameof(RDHitStrip.SetWidth))]
        public static void HitstripSetup(ref float ___width)
        {
            if (!(player == RDPlayer.P1 && P1Enabled.Value)
            && !(player == RDPlayer.P2 && P2Enabled.Value))
                return;

            float hitmar = HitMargin.Value;
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
                float baseVal = HitStripWidthLimiting.Value ? 8f : 1f;
                width = baseVal + ((11f - baseVal) * (hitmar / 40f));
            }

			width = Mathf.Clamp(width, 1f, 50f);
            ___width = Mathf.Round(width);
            player = RDPlayer.CPU;
        }

        private static DefibMode defibViaHitMargins()
        {
            float hitmar = HitMargin.Value;
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
            if ((__instance.contentData.name == PauseContentName.DefibrillatorP1 && P1Enabled.Value)
            || (__instance.contentData.name == PauseContentName.DefibrillatorP2 && P2Enabled.Value))
                return false;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PauseModeContentArrows), nameof(PauseModeContentArrows.UpdateValue))]
        public static bool UpdateValuePrefix(PauseModeContentArrows __instance)
        {
            if ((__instance.contentData.name == PauseContentName.DefibrillatorP1 && P1Enabled.Value)
            || (__instance.contentData.name == PauseContentName.DefibrillatorP2 && P2Enabled.Value))
            {
                __instance.valueText.text = Name.Value;

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
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using BepInEx.Logging;
using System.Xml.Schema;

namespace RDModifications
{
    public class CustomIceChiliSpeeds
    {
        public static ConfigEntry<bool> enabled;
        public static ConfigEntry<float> iceSpeed;
        public static ConfigEntry<float> chiliSpeed;
        public static ConfigEntry<bool> enabledRankScr;

        public static ManualLogSource logger;

        public static void Init(Harmony patcher, ConfigFile config, ManualLogSource logging, ref bool anyEnabled)
        {
            logger = logging;

            enabled = config.Bind("CustomSpeeds", "Enabled", false, "Whether to use the custom defined ice/chili speeds.");
            iceSpeed = config.Bind("CustomSpeeds", "IceSpeed", 0.75f, "The speed multipler to use for ice speeds.");
            chiliSpeed = config.Bind("CustomSpeeds", "ChiliSpeed", 1.5f, "The speed multipler to use for chili speeds.");
            enabledRankScr = config.Bind("CustomSpeeds", "RankScreen", true, "Makes the rank screen colors match the speed more.");

            if (enabled.Value)
            {
                if (iceSpeed.Value == 0.75f && chiliSpeed.Value == 1.5f)
                    logger.LogMessage("CustomSpeeds: All values are default. No differences from base game.");

                if (iceSpeed.Value <= 0.00f || iceSpeed.Value >= 1.00f)
                {
                    iceSpeed.Value = 0.75f;
                    logger.LogWarning("CustomSpeeds: Invalid IceSpeed, value is reset to 0.75x");
                }
                if (chiliSpeed.Value <= 1.00f)
                {
                    chiliSpeed.Value = 1.5f;
                    logger.LogWarning("CustomSpeeds: Invalid ChiliSpeed, value is reset to 1.5x");
                }

                patcher.PatchAll(typeof(GameSpeedPatch));
                patcher.PatchAll(typeof(SaveSpeedPatch));
                patcher.PatchAll(typeof(CLSAudioSpeedPatch));
                if (enabledRankScr.Value)
                    patcher.PatchAll(typeof(RankScreenPatch));

                anyEnabled = true;
            }
        }

        [HarmonyPatch(typeof(scnGame), nameof(scnGame.StartTheGame))]
        private class GameSpeedPatch
        {
            public static void Prefix(ref float speed)
            {
                if (speed == 0.75f)
                    speed = iceSpeed.Value;
                else if (speed == 1.5f)
                    speed = chiliSpeed.Value;
            }
        }

        [HarmonyPatch(typeof(Persistence), "GetCustomLevelKey")]
        private class SaveSpeedPatch
        {
            public static void Postfix(ref string __result, ref float speed)
            {
                string text = "normal";
                if (speed >= 1.05f)
                    text = "chili";
                else if (speed <= 0.95f)
                    text = "ice";

                // this'll work as we only need to fix the _normal
                __result = __result.Replace("_normal", "_" + text);
            }
        }

        [HarmonyPatch(typeof(HUD), nameof(HUD.ShowAndSaveRank))]
        private static class RankScreenPatch
        {
            public static void Postfix(HUD __instance)
            {
                if (RDTime.speed == 1f)
                    return;

                Color iceColour = "70D8ED".HexToColor();
                Color chiliColour = "ED7070".HexToColor();

                Color normalColour = Color.white;
                Color targetColour = chiliColour;
                float intensifier = (RDTime.speed - 1f) / 0.5f;

                if (RDTime.speed < 1f)
                {
                    targetColour = iceColour;
                    intensifier = (1f - RDTime.speed) / 0.25f;
                }
                intensifier = halfValuePast1(intensifier);

                Color endColour = new(
                    Mathf.LerpUnclamped(normalColour.r, targetColour.r, intensifier),
                    Mathf.LerpUnclamped(normalColour.g, targetColour.g, intensifier),
                    Mathf.LerpUnclamped(normalColour.b, targetColour.b, intensifier)
                );

                // should be safe to do so aswell
                __instance.rank.color = endColour;
                __instance.customText.color = endColour;
                __instance.vividStasisRank.color = endColour;
            }

            private static float halfValuePast1(float value)
            {
                if (value > 1f)
                    return ((value - 1f) / 2f) + 1f;
                return value;
            }
        }

        private class CLSAudioSpeedPatch
        {
            // private methods are stupido
            [HarmonyPostfix]
            [HarmonyPatch(typeof(LevelDetail), "Start")]
            public static void StartPostfix(LevelDetail __instance)
            {
                // even stupider
                var privVar = __instance.GetType().GetField("audioSource", BindingFlags.NonPublic | BindingFlags.Instance);
                setAudioSpeed((AudioSource)privVar.GetValue(__instance));
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(LevelDetail), "ChangeLevelSpeed")]
            public static void ChangeSpeedPostfix(LevelDetail __instance)
            {
                // even stupider
                var privVar = __instance.GetType().GetField("audioSource", BindingFlags.NonPublic | BindingFlags.Instance);
                setAudioSpeed((AudioSource)privVar.GetValue(__instance));
            }

            private static void setAudioSpeed(AudioSource audioSource)
            {
                if (audioSource.pitch == 0.75f)
                    audioSource.pitch = iceSpeed.Value;
                else if (audioSource.pitch == 1.5f)
                    audioSource.pitch = chiliSpeed.Value;
            }
        }
    }
}
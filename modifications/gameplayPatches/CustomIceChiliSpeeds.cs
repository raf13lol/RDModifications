using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace RDModifications;

[Modification]
public class CustomIceChiliSpeeds
{
    public static ConfigEntry<bool> enabled;
    public static ConfigEntry<float> iceSpeed;
    public static ConfigEntry<float> chiliSpeed;
    public static ConfigEntry<bool> enabledRankScr;

    public static ManualLogSource logger;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("CustomSpeeds", "Enabled", false, 
        "Whether to use the custom defined ice/chili speeds, also allows using custom speeds on any level.");

        iceSpeed = config.Bind("CustomSpeeds", "IceSpeed", 0.75f, 
        "The speed multiplier to use for ice speeds.");

        chiliSpeed = config.Bind("CustomSpeeds", "ChiliSpeed", 1.5f, 
        "The speed multiplier to use for chili speeds.");

        enabledRankScr = config.Bind("CustomSpeeds", "RankScreen", true, 
        "Makes the rank screen colors adjust depending on the speed more accurately.");

        if (enabled.Value && iceSpeed.Value == 0.75f && chiliSpeed.Value == 1.5f)
        {
            logger.LogMessage("CustomSpeeds: All values are default. No differences from base game.");
            // return false;
        }
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
        return enabled.Value;
    }

    [HarmonyPatch(typeof(HeartMonitor), nameof(HeartMonitor.Show))]
    private class SpeedAnyPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)Level.Montage))
                .Advance(-2)
                .RemoveInstructions(6)
                .InstructionEnumeration();
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

    [HarmonyPatch(typeof(Rankscreen), nameof(Rankscreen.ShowAndSaveRank))]
    private static class RankScreenPatch
    {
        public static void Postfix(Rankscreen __instance)
        {
            if (RDTime.speed == 1f || !enabledRankScr.Value)
                return;

            Color iceColor = "70D8ED".HexToColor();
            Color chiliColor = "ED7070".HexToColor();

            Color normalColor = Color.white;
            Color targetColor = chiliColor;
            float intensifier = (RDTime.speed - 1f) / 0.5f;

            if (RDTime.speed < 1f)
            {
                targetColor = iceColor;
                intensifier = (1f - RDTime.speed) / 0.25f;
            }
            intensifier = halfValuePast1(intensifier);

            Color endColor = new(
                Mathf.LerpUnclamped(normalColor.r, targetColor.r, intensifier),
                Mathf.LerpUnclamped(normalColor.g, targetColor.g, intensifier),
                Mathf.LerpUnclamped(normalColor.b, targetColor.b, intensifier)
            );

            // should be safe to do so aswell
            __instance.rank.color = endColor;
            __instance.customText.color = endColor;
            __instance.vividStasisRank.color = endColor;
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
        public static IEnumerable<MethodInfo> TargetMethods()
        {
            List<MethodInfo> methods = [];
            methods.Add(AccessUtils.GetMethodCalled(typeof(LevelDetail), "Start"));
            methods.Add(AccessUtils.GetMethodCalled(typeof(LevelDetail), "ChangeLevelSpeed"));
            return methods.AsEnumerable();
        }

        [HarmonyPostfix]
        public static void ASPostfix(AudioSource ___audioSource) 
            => setAudioSpeed(___audioSource);

        private static void setAudioSpeed(AudioSource audioSource)
        {
            if (audioSource.pitch == 0.75f)
                audioSource.pitch = iceSpeed.Value;
            else if (audioSource.pitch == 1.5f)
                audioSource.pitch = chiliSpeed.Value;
        }
    }
}
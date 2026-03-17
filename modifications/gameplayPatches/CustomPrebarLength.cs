
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using RDLevelEditor;
using UnityEngine;

namespace RDModifications;

[Modification(
    "What the prebar length should be.\n" +
    "Do note that this will absolutely cause major issues with the game."
)]
public class CustomPrebarLength : Modification
{
    [Configuration<float>(0.6f, "The length of the prebar in seconds.", [float.Epsilon, float.PositiveInfinity])]
    public static ConfigEntry<float> PrebarLength;

    [Configuration<bool>(false, "If the auto-adjust of the prebar in-game should be fixed, for testing.")]
    public static ConfigEntry<bool> FixAutoAdjust;

    public class PrebarPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RDCalibration), nameof(RDCalibration.SetLatencyToPlatform))]
        [HarmonyPatch(typeof(RDCalibration), nameof(RDCalibration.SetToPresets))]
        [HarmonyPatch(typeof(InspectorPanel_Calibration), "Save")]
        public static void Postfix()
            => RDCalibration.latency = PrebarLength.Value;
    }    

    public class AutoAdjustFixPatch
    {
        public static MethodInfo RefreshBarLength = AccessTools.Method(typeof(scrConductor), "refreshBarLength");

        [HarmonyPrefix]
        [HarmonyPatch(typeof(scrConductor), nameof(scrConductor.SetBPM))]
        [HarmonyPatch(typeof(scrConductor), nameof(scrConductor.crotchetsPerBar), MethodType.Setter)]
        public static void SetPrefix(out float __state)
            => __state = RDCalibration.latency;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(scrConductor), nameof(scrConductor.SetBPM))]
        [HarmonyPatch(typeof(scrConductor), nameof(scrConductor.crotchetsPerBar), MethodType.Setter)]
        public static void SetPostfix(scrConductor __instance, float __state)
        {
            if (!FixAutoAdjust.Value)
                return;
            
            float currentPrebarLength = Math.Clamp(__instance.crotchet * __instance.crotchetsPerBar - RDCalibration.calibration_v - Time.unscaledDeltaTime * 2f - 0.01f, 0, float.PositiveInfinity);
            RDCalibration.latency = Mathf.Min(RDCalibration.latency, currentPrebarLength);
            __instance.startOfLastPrebar += __state - RDCalibration.latency;
            __instance.startOfLastBeatVisual += __state - RDCalibration.latency;
            RefreshBarLength.Invoke(__instance, []);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(scnGame), "Awake")]
        [HarmonyPatch(typeof(scnGame), "OnDestroy")]
        public static void RestorePostfix()
        {
            if (!FixAutoAdjust.Value)
                return;
            RDCalibration.latency = PrebarLength.Value;
        }
    }

    /*
    public class ScorchedEarthPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(scnGame), "LoadingRoutine")]
        public static void LRPrefix(scnGame __instance)
        {
            if (__instance.editor != null || !FixAutoAdjust.Value)
                return;
            SetLatency(__instance, CustomIceChiliSpeeds.GetCustomSpeed(scnGame.levelSpeed));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(scnGame), nameof(scnGame.StartTheGame))]
        public static void STGPrefix(scnGame __instance, float speed)
        {
            if (__instance.editor == null || !FixAutoAdjust.Value)
                return;
            SetLatency(__instance, speed);
        }

        public static void SetLatency(scnGame game, float speed)
        {
            RDCalibration.SetLatencyToPlatform();

            List<LevelEvent_Base> events = game.currentLevel.levelEvents;
            float maxBPM = float.NegativeInfinity;
            int minCrotchetsPerBar = int.MaxValue;
            foreach (LevelEvent_Base ev in events)
            {
                if (ev is LevelEvent_SetBeatsPerMinute bpm)
                    maxBPM = Mathf.Max(maxBPM, bpm.beatsPerMinute);
                if (ev is LevelEvent_PlaySong playSong)
                    maxBPM = Mathf.Max(maxBPM, playSong.beatsPerMinute);
                
                if (ev is LevelEvent_SetCrotchetsPerBar setCPB)
                    minCrotchetsPerBar = Mathf.Min(minCrotchetsPerBar, setCPB.crotchetsPerBar);
            }

            if (float.IsNegativeInfinity(maxBPM))
                maxBPM = 100f;
            if (minCrotchetsPerBar == int.MaxValue)
                minCrotchetsPerBar = 8;
            
            maxBPM *= speed;
            float smallestCrotchet = 60f / maxBPM;
            float shortestPrebar = smallestCrotchet * minCrotchetsPerBar - RDCalibration.calibration_v - 0.01f - Time.deltaTime * 1.5f;
            RDCalibration.latency = Math.Min(RDCalibration.latency, shortestPrebar);

            Log.LogMessage($"{speed} {shortestPrebar} {smallestCrotchet} {maxBPM} {minCrotchetsPerBar}");
        }
    }
    */
}
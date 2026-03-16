
using System;
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

            float currentPrebarLength = Math.Clamp(__instance.crotchet * __instance.crotchetsPerBar - RDCalibration.calibration_v - 0.01f, 0, float.PositiveInfinity);
            RDCalibration.latency = Mathf.Min(RDCalibration.latency, currentPrebarLength);
            __instance.startOfLastPrebar += __state - RDCalibration.latency;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(scnGame), "Awake")]
        public static void RestorePostfix()
        {
            if (!FixAutoAdjust.Value)
                return;
            RDCalibration.latency = PrebarLength.Value;
        }
    }
}

using BepInEx.Configuration;
using HarmonyLib;
using RDLevelEditor;

namespace RDModifications;

[Modification(
    "What the prebar length should be.\n" +
    "Do note that this will absolutely cause major issues with the game."
)]
public class CustomPrebarLength : Modification
{
    [Configuration<float>(0.6f, "The length of the prebar in seconds.", [float.Epsilon, float.PositiveInfinity])]
    public static ConfigEntry<float> PrebarLength;

    public class PrebarPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RDCalibration), nameof(RDCalibration.SetLatencyToPlatform))]
        [HarmonyPatch(typeof(RDCalibration), nameof(RDCalibration.SetToPresets))]
        [HarmonyPatch(typeof(InspectorPanel_Calibration), "Save")]
        public static void Postfix()
            => RDCalibration.latency = PrebarLength.Value;
    }    
}
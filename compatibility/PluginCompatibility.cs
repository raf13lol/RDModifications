using HarmonyLib;

namespace RDModifications;

public class PluginCompatibility
{
    public const string RDEditorPlusGUID = "com.9thcore.rdeditorplus";
    public static bool RDEditorPlusDetected = false;
    public static bool RDEditorPlusSubRowsWithRows = false; // waiting until 9th makes PluginConfig public...

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RDStartup), nameof(RDStartup.Setup))]
    public static void OnceAllLoadedRun()
        => Run();

    public static void Run()
    {
        if (RDEditorPlusDetected = OtherPluginUtils.DetectPlugin(RDEditorPlusGUID, new(0, 7, 0)))
            RDEditorPlusCompatibility.Run();
    }
}
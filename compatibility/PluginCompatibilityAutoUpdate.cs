using HarmonyLib;

namespace RDModifications;

public class PluginCompatibilityAutoUpdate
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SteamIntegration), nameof(SteamIntegration.Setup))]
    public static void AutoUpdateOthers()
        => RunAutoUpdates();

    public static void RunAutoUpdates()
    {
        if (PluginCompatibility.RDEditorPlusDetected)
            _ = Updater.CheckUpdate(new()
            {
                Logger = Modification.Log,
                PluginInfo = OtherPluginUtils.PluginInfos[PluginCompatibility.RDEditorPlusGUID],

                GithubRepoURL = "9thCore/RDEditorPlus",
                GithubRepoBranch = "main",

                ReleaseName = "RDEditorPlus_v{version}.zip",
                VersionName = "VERSION.txt",
                ChangelogName = "CHANGELOG.txt",

                IsZip = true,
            });
    }
}
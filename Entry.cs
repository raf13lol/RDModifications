using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
#if !BPE5
using BepInEx.Unity.Mono;
#endif
using HarmonyLib;
using UnityEngine;

namespace RDModifications;

// [BepInDependency(PluginCompatibility.RDEditorPlusGUID, BepInDependency.DependencyFlags.SoftDependency)]

[BepInProcess("Rhythm Doctor.exe")]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Entry : BaseUnityPlugin
{
    public const bool IsBPE5 =
#if !BPE5 
    false;
#else 
    true;
#endif

#if !BPE5
    public const string DLLName = "com.rhythmdr.randommodifications.dll";
#else
    public const string DLLName = "com.rhythmdr.bpe5randommodifications.dll";
#endif

    public static string UserDataFolder = Path.Combine(Application.dataPath.Replace("Rhythm Doctor_Data", ""), "User");

    public static ConfigEntry<bool> Enabled;
    public static ConfigEntry<bool> AutoUpdateEnabled;
    public static ConfigEntry<bool> AutoUpdateAssumeBeta;
    public static ConfigEntry<bool> EditorEnabled;

    public static Harmony HarmonyPatcher;
    public static ConfigFile ConfigurationFile;
    public static PluginInfo PluginInfo;

    public void Awake()
    {
        Enabled = Config.Bind("", "Enabled", true,
        "If any of the available modifications should be loaded at all.");
        AutoUpdateEnabled = Config.Bind("", "AutoUpdateEnabled", true,
        "If RDModifications should auto-update. Only disable this in specific cases.");
        AutoUpdateAssumeBeta = Config.Bind("", "AutoUpdateAssumeBeta", false,
        "If the auto-updater should assume you're on beta if it is unable to check your Steam branch.");
        EditorEnabled = Config.Bind("EditorPatches", "Enabled", true,
        "If any of the editor patches should be enabled.");

        if (!Enabled.Value)
        {
            Logger.LogMessage("All modifications have been disabled.");
            return;
        }

        HarmonyPatcher = new("RDMP");
        ConfigurationFile = Config;
        PluginInfo = Info;

        Modification.Log = Logger;
        Modification.Enabled = [];

        try
        {
            // We do everything and we give nothing to the classes
            // (i'm making it sound really fancy)
            Patcher.PatchAllWithAttribute<ModificationAttribute>(HarmonyPatcher, ConfigurationFile, out bool anyEnabled, !EditorEnabled.Value);
            HarmonyPatcher.PatchAll(typeof(PluginCompatibility));
            GlobalPatches.PatchAll(HarmonyPatcher);

            if (anyEnabled)
                Logger.LogMessage("Any modifications that have been enabled have been loaded. See individual messages for any info on issues.");
            else
                Logger.LogMessage("No modifications are enabled, edit your config file to change your settings.");
        }
        catch (Exception e)
        {
            HarmonyPatcher.UnpatchSelf();
            Logger.LogError(e);
            Logger.LogWarning($"An error occurred whilst loading a modification, so RDModifications has disabled itself (except for the auto-update, if you have that enabled).");
        }
        finally
        {
            PatchUpdater();
        }
    }

    public static void PatchUpdater()
    {
        if (!AutoUpdateEnabled.Value)
            return;

        Harmony autoUpdatePatcher = new("RDMAUP");
        autoUpdatePatcher.PatchAll(typeof(Updater.SteamUpdatePatch));
        autoUpdatePatcher.PatchAll(typeof(PluginCompatibilityAutoUpdate));

        Application.quitting += delegate ()
        {
            foreach (AutoUpdateFile file in Updater.FilesToUpdateOnClose)
            {
                Updater.HandleFile(file, true);
            }
        };
    }
}


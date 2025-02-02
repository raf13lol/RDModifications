using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;
using HarmonyLib;

namespace RDModifications
{
    [BepInProcess("Rhythm Doctor.exe")]
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class RDModificationsEntry : BaseUnityPlugin
    {
        public static new ConfigEntry<bool> enabled;
        public static ConfigEntry<bool> enabledEditor;

        public void Awake()
        {
            enabled = Config.Bind("", "Enabled", true, 
            "Whether any of the available modifications should be loaded at all.");
            enabledEditor = Config.Bind("EditorPatches", "Enabled", false,
            "If any of the editor patches should be enabled.");

            if (enabled.Value)
            {
                bool anyEnabled = false;
                Harmony patcher = new("patcher");

                // we send the patcher/config to each class so they can all handle their own logic independant of the main class
                // (i'm making it sound really fancy)
                PatchUtils.PatchAllWithAttribute<ModificationAttribute>(patcher, Config, Logger, ref anyEnabled);
                if (enabledEditor.Value)
                    PatchUtils.PatchAllWithAttribute<EditorModificationAttribute>(patcher, Config, Logger, ref anyEnabled);

                if (anyEnabled)
                    Logger.LogMessage("Any modifications that have been enabled have been loaded. See individual messages for any info on issues.");
                else
                    Logger.LogMessage("No modifications are enabled, edit your config file to change ");
            }
            else
                Logger.LogMessage("All modifications have been disabled.");
        }
    }

    
}
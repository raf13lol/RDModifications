using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;
using HarmonyLib;

namespace RDModifications;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Rhythm Doctor.exe")]
// i don't know what
#pragma warning disable BepInEx002 // Classes with BepInPlugin attribute must inherit from BaseUnityPlugin
public class RDModifications : BaseUnityPlugin
#pragma warning restore BepInEx002 // Classes with BepInPlugin attribute must inherit from BaseUnityPlugin
{
    public void Awake()
    {
        ConfigEntry<bool> enabled = Config.Bind("", "Enabled", true, "Whether any of the available modifications should be loaded at all.");

        if (enabled.Value)
        {
            bool anyEnabled = false;
            Harmony patcher = new("patcher");

            // we send the patcher/config to each class so they can all handle their own logic independant of the main class
            // (i'm making it sound really fancy)
            CustomDifficulty.Init(patcher, Config, Logger, ref anyEnabled);
            CustomDiscordRichPresence.Init(patcher, Config, Logger, ref anyEnabled);
            CustomSamuraiMode.Init(patcher, Config, Logger, ref anyEnabled);
            CustomIceChiliSpeeds.Init(patcher, Config, Logger, ref anyEnabled);
            DoctorMode.Init(patcher, Config, Logger, ref anyEnabled);
            PretendFOnMistake.Init(patcher, Config, Logger, ref anyEnabled);

            ExtraLevelEndDetails.Init(patcher, Config, Logger, ref anyEnabled);

            if (anyEnabled)
                Logger.LogMessage("Any modifications that have been enabled have been loaded. See individual messages for any info on issues.");
            else
                Logger.LogMessage("No modifications are enabled, edit your config file to change ");
        }
        else
            Logger.LogMessage("All modifications have been disabled.");
    }
}
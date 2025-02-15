using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;
using HarmonyLib;

namespace RDModifications;

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
        
        _ = CheckUpdate();

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

    public async Task CheckUpdate()
    {
        try 
        {
            HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync("https://raw.githubusercontent.com/raf13lol/RDModifications/refs/heads/main/VERSION.txt");
            if (response.StatusCode != HttpStatusCode.OK)
                return;
            string content = await response.Content.ReadAsStringAsync();

            string[] serverVersion = content.Split(".");
            int serverMajor = int.Parse(serverVersion[0]); 
            int serverMinor = int.Parse(serverVersion[1]); 
            int serverPatch = int.Parse(serverVersion[2]); 

            string[] currentVersion = MyPluginInfo.PLUGIN_VERSION.Split(".");
            int currentMajor = int.Parse(currentVersion[0]); 
            int currentMinor = int.Parse(currentVersion[1]); 
            int currentPatch = int.Parse(currentVersion[2]); 

            int serverVersionNum = serverMajor * 10000 + serverMinor * 100 + serverPatch; 
            int currentVersionNum = currentMajor * 10000 + currentMinor * 100 + currentPatch; 
            if (serverVersionNum <= currentVersionNum)
                return;

            HttpResponseMessage file = await client.GetAsync($"https://github.com/raf13lol/RDModifications/releases/download/{content}/com.rhythmdr.randommodifications.dll");
            if (file.StatusCode != HttpStatusCode.OK)
                return;
            byte[] fileData = await file.Content.ReadAsByteArrayAsync();

            char slash = Path.DirectorySeparatorChar;
            File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + $@"{slash}BepInEx{slash}plugins{slash}com.rhythmdr.randommodifications.dll", fileData);
            Logger.LogWarning($"RDModifications was outdated ({content} > {MyPluginInfo.PLUGIN_VERSION}), please restart to apply the updated version of the mod.");
        }
        catch
        {
            // doesn't matter, prob just no wifi
        }
    }
}


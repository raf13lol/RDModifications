using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
#if !BPE5
using BepInEx.Unity.Mono;
#endif
using HarmonyLib;
using UnityEngine;


namespace RDModifications;

[BepInProcess("Rhythm Doctor.exe")]
// We need this so we can detect it in our UnlockEditor so we can decide if that should run
[BepInDependency("wtf.seq.unlockeditor", BepInDependency.DependencyFlags.SoftDependency)] 
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Entry : BaseUnityPlugin
{
	#if !BPE5
		public const string DLLName = "randommodifications";
	#else
		public const string DLLName = "bpe5randommodifications";
	#endif

	public static string UserDataFolder = Path.Combine(Application.dataPath.Replace("Rhythm Doctor_Data", ""), "User");

    public static ConfigEntry<bool> Enabled;
    public static ConfigEntry<bool> AutoUpdateEnabled;
    public static ConfigEntry<bool> EditorEnabled;

	public static Harmony HarmonyPatcher;
	public static ConfigFile Configuration;
	public static PluginInfo PluginInfo;

    public void Awake()
    {
        Enabled = Config.Bind("", "Enabled", true, 
        "Whether any of the available modifications should be loaded at all.");
        AutoUpdateEnabled = Config.Bind("", "AutoUpdateEnabled", true, 
        "Whether RDModifications should auto-update. Only disable this in specific cases.");
        EditorEnabled = Config.Bind("EditorPatches", "Enabled", true,
        "If any of the editor patches should be enabled.");
        
		if (AutoUpdateEnabled.Value)
        	_ = CheckUpdate();

		if (!Enabled.Value)
		{
			Logger.LogMessage("All modifications have been disabled.");
			return;
		}

		HarmonyPatcher = new("patcher");
		Configuration = Config;
		PluginInfo = Info;

		Modification.Log = Logger;
		Modification.Enabled = [];

		// We do everything and we give nothing to the classes
		// (i'm making it sound really fancy)
		Patcher.PatchAllWithAttribute<ModificationAttribute>(out bool anyEnabled, !EditorEnabled.Value);

		if (anyEnabled)
			Logger.LogMessage("Any modifications that have been enabled have been loaded. See individual messages for any info on issues.");
		else
			Logger.LogMessage("No modifications are enabled, edit your config file to change your settings.");
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
            {
                if (serverVersionNum < currentVersionNum)
                    Logger.LogMessage("dev build 👍");
                return;
            }

            HttpResponseMessage file = await client.GetAsync($"https://github.com/raf13lol/RDModifications/releases/download/{content}/com.rhythmdr.{DLLName}.dll");
            if (file.StatusCode != HttpStatusCode.OK)
                return;

            byte[] fileData = await file.Content.ReadAsByteArrayAsync();
            File.WriteAllBytes(PluginInfo.Location, fileData);
            Logger.LogWarning($"RDModifications was outdated ({content} > {MyPluginInfo.PLUGIN_VERSION}), please restart to apply the updated version of the mod.");

			HttpResponseMessage response2 = await client.GetAsync("https://raw.githubusercontent.com/raf13lol/RDModifications/refs/heads/main/CHANGELOG.txt");
            if (response.StatusCode != HttpStatusCode.OK)
                return;
				
            string changelog = await response2.Content.ReadAsStringAsync();
			Logger.LogWarning("Changelog: \n" + changelog);
        }
        catch //(Exception e)
        {
			// Logger.LogMessage(e.Message);
			// Logger.LogMessage(e.StackTrace);
			// Logger.LogMessage(e.Source);
			// Logger.LogMessage(e.GetType());
            // doesn't matter, prob just no wifi
        }
    }
}


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
        {
            Harmony autoUpdatePatcher = new("autoupdatepatcher");
			autoUpdatePatcher.PatchAll(typeof(Updater));
        }

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

    
}


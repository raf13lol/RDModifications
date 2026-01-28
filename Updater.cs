using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace RDModifications;

public class Updater
{
	[HarmonyPatch(typeof(SteamIntegration), nameof(SteamIntegration.Setup))]
	private class SteamUpdatePatch
    {
        public static void Postfix()
			=> _ = CheckUpdate(Modification.Log, Entry.PluginInfo, Entry.DLLName);
    }

    public static async Task CheckUpdate(ManualLogSource Logger, PluginInfo PluginInfo, string DLLName)
    {
        try 
        {
            HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync("https://raw.githubusercontent.com/raf13lol/RDModifications/refs/heads/main/VERSION.txt");
            if (response.StatusCode != HttpStatusCode.OK)
                return;
            string content = await response.Content.ReadAsStringAsync();
			bool betaOnly = content.EndsWith("b");
			if (betaOnly)
				content = content[0..(content.Length - 1)];

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

			if (!betaOnly || GC.onBetaBranch)
            {
				HttpResponseMessage file = await client.GetAsync($"https://github.com/raf13lol/RDModifications/releases/download/{content}/com.rhythmdr.{DLLName}.dll");
				if (file.StatusCode != HttpStatusCode.OK)
					return;
				byte[] fileData = await file.Content.ReadAsByteArrayAsync();
				File.WriteAllBytes(PluginInfo.Location, fileData);
				Logger.LogWarning($"RDModifications was outdated ({content} > {MyPluginInfo.PLUGIN_VERSION}), please restart to apply the updated version of the mod.");
            }
			else
                Logger.LogWarning($"HEY ! Um. This update ({MyPluginInfo.PLUGIN_VERSION}->{content}) requires you to be on the beta branch. So. Um. No update.");
				
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
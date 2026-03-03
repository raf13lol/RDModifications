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
    public class SteamUpdatePatch
    {
        public static void Postfix()
            => _ = CheckUpdate(Modification.Log, Entry.PluginInfo, "raf13lol/RDModifications", Entry.DLLName);
    }

    public static async Task CheckUpdate(ManualLogSource Logger, PluginInfo PluginInfo, string githubRepoURL, string ReleaseDLLName)
    {
        try
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync($"https://raw.githubusercontent.com/{githubRepoURL}/refs/heads/main/VERSION.txt");
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
                    Logger.LogMessage($"dev build of {PluginInfo.Metadata.Name} 👍");
                return;
            }

            if (!betaOnly || GC.onBetaBranch || Entry.AutoUpdateAssumeBeta.Value)
            {
                using HttpResponseMessage file = await client.GetAsync($"https://github.com/{githubRepoURL}/releases/download/{content}/{ReleaseDLLName}.dll");
                if (file.StatusCode != HttpStatusCode.OK)
                    return;
                byte[] fileData = await file.Content.ReadAsByteArrayAsync();
                File.WriteAllBytes(PluginInfo.Location, fileData);
                Logger.LogWarning($"{PluginInfo.Metadata.Name} was outdated ({content} > {MyPluginInfo.PLUGIN_VERSION}), please restart to apply the updated version of the mod.");
            }
            else
                Logger.LogWarning($"The update of {PluginInfo.Metadata.Name} ({MyPluginInfo.PLUGIN_VERSION}->{content}) requires you to be on the beta branch (if you are, please run Steam) to update.");

            using HttpResponseMessage response2 = await client.GetAsync($"https://raw.githubusercontent.com/{githubRepoURL}/refs/heads/main/CHANGELOG.txt");
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
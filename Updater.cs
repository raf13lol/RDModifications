using System;
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

            string[] serverVersionText = content.Split(".");
            int serverMajor = int.Parse(serverVersionText[0]);
            int serverMinor = int.Parse(serverVersionText[1]);
            int serverBuild = int.Parse(serverVersionText[2]);

            string[] currentVersionText = MyPluginInfo.PLUGIN_VERSION.Split(".");
            int currentMajor = int.Parse(currentVersionText[0]);
            int currentMinor = int.Parse(currentVersionText[1]);
            int currentBuild = int.Parse(currentVersionText[2]);

            Version serverVersion = new(serverMajor, serverMinor, serverBuild);
            Version currentVersion = new(currentMajor, currentMinor, currentBuild);
            if (serverVersion <= currentVersion)
            {
                if (serverVersion < currentVersion)
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
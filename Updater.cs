using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace RDModifications;

public class Updater
{
    public static bool CanAutoUpdate = !Entry.IsBPE5 || Application.platform != RuntimePlatform.WindowsPlayer;
    public static bool LoggedClosingWarning = !Entry.IsBPE5;

    public static List<AutoUpdateFile> FilesToUpdateOnClose = [];

    [HarmonyPatch(typeof(SteamIntegration), nameof(SteamIntegration.Setup))]
    public class SteamUpdatePatch
    {
        public static void Postfix()
            => _ = CheckUpdate(new()
            {
                Logger = Modification.Log,
                PluginInfo = Entry.PluginInfo,

                GithubRepoURL = "raf13lol/RDModifications",
                GithubRepoBranch = "main",

                ReleaseName = Entry.DLLName,
                VersionName = "VERSION.txt",
                ChangelogName = "CHANGELOG.txt",

                IsZip = false,
            });
    }

    public static async Task CheckUpdate(AutoUpdateData data)
    {
        string rawGithubURL = $"https://raw.githubusercontent.com/{data.GithubRepoURL}/refs/heads/{data.GithubRepoBranch}/";
        try
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(rawGithubURL + data.VersionName);
            if (response.StatusCode != HttpStatusCode.OK)
                return;

            string versionText = (await response.Content.ReadAsStringAsync()).Trim();
            bool betaOnly = versionText.EndsWith("b");
            if (betaOnly)
                versionText = versionText[0..(versionText.Length - 1)];

            string[] serverVersionText = versionText.Split(".");
            int serverMajor = int.Parse(serverVersionText[0]);
            int serverMinor = int.Parse(serverVersionText[1]);
            int serverBuild = int.Parse(serverVersionText[2]);

            Version serverVersion = new(serverMajor, serverMinor, serverBuild);
            Version currentVersion = new(
                data.PluginInfo.Metadata.Version.Major,
                data.PluginInfo.Metadata.Version.Minor,
#if !BPE5
                data.PluginInfo.Metadata.Version.Patch
#else
                data.PluginInfo.Metadata.Version.Build
#endif
            );
            if (serverVersion <= currentVersion)
                return;
            
            if (!CanAutoUpdate)
                data.Logger.LogWarning($"{data.PluginInfo.Metadata.Name} has updated! Get it at https://github.com/{data.GithubRepoURL}/releases/latest.");
            else if (!betaOnly || GC.onBetaBranch || Entry.AutoUpdateAssumeBeta.Value)
            {
                using HttpResponseMessage file = await client.GetAsync(
                    $"https://github.com/{data.GithubRepoURL}/releases/download/{versionText}/{data.ReleaseName.Replace("{version}", versionText)}"
                );
                if (file.StatusCode != HttpStatusCode.OK)
                    return;

                if (!LoggedClosingWarning)
                {
                    data.Logger.LogWarning("Please only quit the game via 'Exit' in the main menu or Alt+F4 on the game window so the auto-updates can work.");
                    LoggedClosingWarning = true;
                }

                HandleFile(new()
                {
                    FileData = await file.Content.ReadAsByteArrayAsync(),
                    PluginLocation = data.PluginInfo.Location,
                    IsZip = data.IsZip
                });

                data.Logger.LogWarning($"{data.PluginInfo.Metadata.Name} was outdated ({versionText} > {data.PluginInfo.Metadata.Version}), please restart to apply the updated version of the mod.");
            }
            else
                data.Logger.LogWarning(
                    $"The update of {data.PluginInfo.Metadata.Name} ({data.PluginInfo.Metadata.Version}->{versionText}) requires you to be on the beta branch (if you are, please run Steam) to update."
                );

            if (data.ChangelogName == null)
                return;

            using HttpResponseMessage response2 = await client.GetAsync(
                rawGithubURL + data.ChangelogName
            );
            if (response.StatusCode != HttpStatusCode.OK)
                return;

            string changelog = await response2.Content.ReadAsStringAsync();
            data.Logger.LogWarning($"Changelog of {data.PluginInfo.Metadata.Name}: \n" + changelog);
        }
        catch// (Exception e)
        {
            // data.Logger.LogMessage(e.Message);
            // data.Logger.LogMessage(e.StackTrace);
            // data.Logger.LogMessage(e.Source);
            // data.Logger.LogMessage(e.GetType());
            // doesn't matter, prob just no wifi
        }
    }

    public static void HandleFile(AutoUpdateFile fileInfo, bool gameClosing = false)
    {
        if (Entry.IsBPE5 && !gameClosing)
        {
            FilesToUpdateOnClose.Add(fileInfo);
            return;
        }

        if (!fileInfo.IsZip)
        {
            File.WriteAllBytes(fileInfo.PluginLocation, fileInfo.FileData);
            return;
        }

        using MemoryStream file = new(fileInfo.FileData);

        string pluginFolder = Path.GetDirectoryName(fileInfo.PluginLocation) + Path.DirectorySeparatorChar;
        ZipArchive modZip = new(file);
        foreach (ZipArchiveEntry entry in modZip.Entries)
        {
            string directory = Path.GetDirectoryName(pluginFolder + entry.FullName) ?? string.Empty;
            if (directory != string.Empty)
                Directory.CreateDirectory(directory);
            entry.ExtractToFile(pluginFolder + entry.FullName, true);
        }
    }
}
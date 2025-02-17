using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BepInEx.Configuration;
using BepInEx.Logging;
using Newtonsoft.Json;
using HarmonyLib;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RDModifications;

[Modification]
public class LevelPRStatus
{
    public static ManualLogSource logger;

    public static ConfigEntry<bool> enabled;
    public static ConfigEntry<float> colorAmplifier;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("LevelPRStatus", "Enabled", false,
        "If it should display the peer-review status of a level (if it is on Rhythm Café), by changing the color of the syringe body, in the custom level select screen.");
        colorAmplifier = config.Bind("LevelPRStatus", "ColorAmplifier", 1f,
        "How much more (or less) the color of the syringe body should change depending on the PR status.");

        if (enabled.Value)
            _ = PRLevels.Init();

        return enabled.Value;
    }

    private class PRLevels
    {
        public static Dictionary<string, sbyte> levelStatuses = [];

        public static sbyte Get(string id)
        {
            if (levelStatuses.TryGetValue(id, out sbyte val))
                return val;
            return -127; // means no entry (not on rdcafe or not all data got yet)
        }

        public static async Task Init()
        {
            HttpClient client = new();
            bool gotAllSongs = false;
            int page = 1;

            logger.LogMessage("LevelPRStatus: Obtaining PR statuses...");

            while (!gotAllSongs)
            {
                HttpRequestMessage request = new(HttpMethod.Get,
                new Uri("https://orchardb.fly.dev/typesense/collections/levels/documents/search/"
                    + "?q=*&per_page=250&include_fields=id, approval&highlight_fields=none&highlight_full_fields=none"
                    + "&page=" + page++));
                request.Headers.Add("x-typesense-api-key", "nicolebestgirl");

                HttpResponseMessage response = await client.SendAsync(request);
                if (response.StatusCode != HttpStatusCode.OK)
                    return;

                string jsonText = await response.Content.ReadAsStringAsync();
                TypeSenseResponse json = JsonConvert.DeserializeObject<TypeSenseResponse>(jsonText);
                
                TypeSenseResponse.Hit[] hits = [.. json.hits];
                if (hits.Length < 250)
                    gotAllSongs = true;

                foreach (TypeSenseResponse.Hit hit in hits)
                    levelStatuses.Add(hit.document.id, (sbyte)hit.document.approval);
            }

            logger.LogMessage("LevelPRStatus: PR statuses obtained!");
        }

        // needed
        private class TypeSenseResponse
        {
            public int search_time_ms { get; set; }
            public Hit[] hits { get; set; }

            public class Hit
            {
                public Document document { get; set; }
            }

            public class Document
            {
                public string id { get; set; }
                public int approval { get; set; }
            }
        }
    }

    [HarmonyPatch(typeof(CustomLevel), nameof(CustomLevel.UpdateInfo))]
    private class CLSPatch
    {
        public static void Postfix(CustomLevel __instance, CustomLevelData data)
        {
            string path = data.path;
            if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
                path = path[..^1];
            string[] directories = path.Split(Path.DirectorySeparatorChar);
            if (directories.Length <= 1)
                directories = path.Split(Path.AltDirectorySeparatorChar);

            string folderName = directories.Last();
            folderName = folderName.Replace(".rdzip", "");
            int status = PRLevels.Get(folderName);

            if (status == -127)
            {
                __instance.syringeBodyImage.color = Color.white;
                return;
            }

            Color colToSet = status switch
            {   
                -1 => Color.red,
                0 => Color.black,
                10 => Color.green,
                _ => Color.white
            };

            __instance.syringeBodyImage.color = Color.Lerp(Color.white, colToSet, Mathf.Min((status == 10 ? 0.325f : 0.5f) * colorAmplifier.Value, 1));
        }
    }
    
}
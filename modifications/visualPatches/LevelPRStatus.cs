using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace RDModifications;

[Modification("If the colour of the syringe body should change to display the peer-review status of a level (if it is on Rhythm Café) in the custom level select screen.")]
public class LevelPRStatus : Modification
{
    [Configuration<float>(1f, "How much more (or less) the colour of the syringe body should change depending on the PR status.")]
    public static ConfigEntry<float> ColourAmplifier;

    [Configuration<bool>(false,
        "If the PR statuses should be saved and refreshed on each load instead of fully reloading each time.\n" +
        "May take up some storage (~400KB+) and slow down the start-up sequence of Rhythm Doctor."
    )]
    public static ConfigEntry<bool> ShouldCache;

    public static void Init(bool enabled)
    {
        if (enabled)
            _ = PRLevels.Init();
    }

    [HarmonyPatch(typeof(CustomLevel), nameof(CustomLevel.UpdateInfo))]
    public class CLSPatch
    {
        public static void Postfix(CustomLevel __instance, CustomLevelData data)
        {
            PRStatus status = PRLevels.Get(data);
            if (status == PRStatus.Unknown)
            {
                __instance.syringeBodyImage.color = Color.white;
                return;
            }

            Color colToSet = status switch
            {
                PRStatus.NonRefereed => Color.red,
                PRStatus.Pending => Color.black,
                PRStatus.Mixed => Color.blue,
                PRStatus.PeerReviewed => Color.green,
                _ => Color.white
            };

            __instance.syringeBodyImage.color = Color.Lerp(Color.white, colToSet, Mathf.Min((status == PRStatus.Pending ? 0.325f : 0.5f) * ColourAmplifier.Value, 1));
        }
    }

    public class PRLevels
    {
        public static Dictionary<string, sbyte> LevelStatuses = [];
        public static Dictionary<string, sbyte> LevelStatusesCached = [];

        public static string OldFilename = Path.Combine(Entry.UserDataFolder, "__rdmodifications_prstatuses_cache.rdmf");
        public static string Filename = Path.Combine(Entry.UserDataFolder, "__rdmodifications_prstatuses_cachev2.rdmf");

        public static PRStatus Get(string id)
        {
            sbyte status = (sbyte)PRStatus.Unknown;

            if (LevelStatuses.TryGetValue(id, out sbyte val1))
                status = val1;
            else if (LevelStatusesCached.TryGetValue(id, out sbyte val2))
                status = val2;

            return (PRStatus)status;
        }

        public static PRStatus Get(CustomLevelData data)
            => Get(data.Hash);

        private static void SetPRStatus(string key, sbyte value)
        {
            if (LevelStatuses.TryGetValue(key, out sbyte existingStatus) && existingStatus != value)
                value = (sbyte)PRStatus.Mixed;

            LevelStatuses[key] = value;
        }

        public static async Task Init()
        {
            try
            {
                if (File.Exists(OldFilename))
                    File.Delete(OldFilename);
            }
            catch
            { }

            if (ShouldCache.Value && File.Exists(Filename))
            {
                string[] savedCache = File.ReadAllLines(Filename);
                foreach (string str in savedCache)
                {
                    string[] parts = str.Split("=");
                    LevelStatusesCached[parts[0]] = sbyte.Parse(parts[1]);
                }
            }

            using HttpClient client = new()
            {
                Timeout = TimeSpan.FromMinutes(30)
            };
            bool gotAllSongs = false;
            int page = 1;

            Log.LogMessage("LevelPRStatus: Obtaining PR statuses...");

            while (!gotAllSongs)
            {
                using HttpRequestMessage request = new(HttpMethod.Get,
                new Uri("https://rhythm.cafe/api/levels/?peer_review=all&show_hidden=all&per_page=100"
                    + "&page=" + page++));

                HttpResponseMessage response;
                try
                {
                    response = await client.SendAsync(request);
                }
                catch (Exception exception)
                {
                    Log.LogMessage($"LevelPRStatus: Failed to get a page of levels. Error: {exception.Message}");
                    continue;
                }
                if (response.StatusCode != HttpStatusCode.OK)
                    return;

                string jsonText = await response.Content.ReadAsStringAsync();
                CafeResponse json = JsonConvert.DeserializeObject<CafeResponse>(jsonText);

                CafeResponse.Hit[] hits = json.results.hits;
                int hitsLength = Math.Min(hits.Length, 100);
                if (hitsLength < 100)
                    gotAllSongs = true;

                for (int i = 0; i < hitsLength; i++)
                {
                    CafeResponse.Hit hit = hits[i];
                    SetPRStatus(hit.rd_md5, (sbyte)hit.approval);
                }

                response.Dispose();
            }

            if (ShouldCache.Value)
            {
                Log.LogMessage("LevelPRStatus: PR statuses obtained! Generating cache...");
                string cache = "";

                int linesPerFrame = 100;
                int i = 0;
                foreach (KeyValuePair<string, sbyte> kvp in LevelStatuses)
                {
                    if (cache != "")
                        cache += "\n";
                    cache += $"{kvp.Key}={kvp.Value}";

                    if (i++ >= linesPerFrame)
                    {
                        await Task.Delay(25);
                        i = 0;
                    }
                }

                Log.LogMessage("LevelPRStatus: Cache generated!");
                _ = File.WriteAllTextAsync(Filename, cache);
            }
            else
                Log.LogMessage("LevelPRStatus: PR statuses obtained!");
        }

        public class CafeResponse
        {
            public Results results { get; set; }

            public class Results
            {
                public Hit[] hits { get; set; }
            }

            public class Hit
            {
                public string rd_md5 { get; set; }
                public int approval { get; set; }
            }
        }

        // needed
        // public class TypesenseResponse
        // {
        //     public int search_time_ms { get; set; }
        //     public Hit[] hits { get; set; }

        //     public class Hit
        //     {
        //         public Document document { get; set; }
        //     }

        //     public class Document
        //     {
        //         public string id { get; set; }
        //         public string sha1 { get; set; }
        //         public int approval { get; set; }
        //     }
        // }
    }
}

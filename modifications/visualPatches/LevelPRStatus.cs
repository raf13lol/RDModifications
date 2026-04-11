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
                PRStatus.PeerReviewed => Color.green,
                _ => Color.white
            };

            __instance.syringeBodyImage.color = Color.Lerp(Color.white, colToSet, Mathf.Min((status == PRStatus.Pending ? 0.325f : 0.5f) * ColourAmplifier.Value, 1));
        }
    }

    public class PRLevels
    {
        public static Dictionary<string, sbyte> LevelStatuses = [];
        public static Dictionary<string, sbyte> LevelV2Statuses = [];
        public static string Filename = Path.Combine(Entry.UserDataFolder, "__rdmodifications_prstatuses_cache.rdmf");

        public static PRStatus Get(string id)
        {
            sbyte status = (sbyte)PRStatus.Unknown;

            if (LevelStatuses.TryGetValue(id, out sbyte val1))
                status = val1;
            else if (LevelV2Statuses.TryGetValue(id, out sbyte val2))
                status = val2;

            return (PRStatus)status;
        }

        public static PRStatus Get(CustomLevelData data)
            => Get(LevelUtils.GetLevelFolderName(data));

        public static async Task Init()
        {
            if (ShouldCache.Value && File.Exists(Filename))
            {
                string[] savedCache = File.ReadAllLines(Filename);
                foreach (string str in savedCache)
                {
                    string[] parts = str.Split(";");
                    sbyte approval = sbyte.Parse(parts[2]);
                    LevelStatuses.Add(parts[0], approval);
                    LevelV2Statuses.Add(parts[1], approval);
                }
            }

            using HttpClient client = new()
            {
                Timeout = TimeSpan.FromMinutes(30)
            };
            bool gotAllSongs = false;
            int page = 1;
            string cache = "";

            Log.LogMessage("LevelPRStatus: Obtaining PR statuses...");

            while (!gotAllSongs)
            {
                using HttpRequestMessage request = new(HttpMethod.Get,
                new Uri("https://orchardb.fly.dev/typesense/collections/levels/documents/search/"
                    + "?q=*&per_page=250&include_fields=id, approval, sha1&highlight_fields=none&highlight_full_fields=none"
                    + "&page=" + page++));
                request.Headers.Add("x-typesense-api-key", "nicolebestgirl");

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
                TypeSenseResponse json = JsonConvert.DeserializeObject<TypeSenseResponse>(jsonText);

                TypeSenseResponse.Hit[] hits = [.. json.hits];
                int hitsLength = hits.Length;
                if (hitsLength < 250)
                    gotAllSongs = true;

                for (int i = 0; i < hitsLength; i++)
                {
                    TypeSenseResponse.Document doc = hits[i].document;

                    string sha = doc.sha1[3..];
                    sbyte approval = (sbyte)doc.approval;
                    LevelStatuses.TryAdd(doc.id, approval);
                    LevelV2Statuses.TryAdd(sha, approval);

                    if (cache != "")
                        cache += "\n";
                    cache += $"{doc.id};{sha};{approval}";
                }

                response.Dispose();
            }

            if (ShouldCache.Value)
                _ = File.WriteAllTextAsync(Filename, cache);
            Log.LogMessage("LevelPRStatus: PR statuses obtained!");
        }

        // needed
        public class TypeSenseResponse
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
                public string sha1 { get; set; }
                public int approval { get; set; }
            }
        }
    }
}

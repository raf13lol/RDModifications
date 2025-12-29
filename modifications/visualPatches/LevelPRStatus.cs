using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BepInEx.Configuration;
using Newtonsoft.Json;
using HarmonyLib;
using UnityEngine;

namespace RDModifications;

[Modification("If the colour of the syringe body should change to display the peer-review status of a level (if it is on Rhythm Café) in the custom level select screen.")]
public class LevelPRStatus : Modification
{
	[Configuration<float>(1f, "How much more (or less) the colour of the syringe body should change depending on the PR status.")]
    public static ConfigEntry<float> ColourAmplifier;

    public static void Init(bool enabled)
    {
        if (enabled)
            _ = PRLevels.Init();
    }

    [HarmonyPatch(typeof(CustomLevel), nameof(CustomLevel.UpdateInfo))]
    private class CLSPatch
    {
        public static void Postfix(CustomLevel __instance, CustomLevelData data)
        {
            int status = PRLevels.Get(LevelUtils.GetLevelFolderName(data));
            if (status == -127)
            { 
                // check if the level was downloaded from rdcafe v2
                // NOTE: i hope rdcafe v2 uses the same database, but i don't think it will due to alternate naming :(
                status = PRLevels.Get(LevelUtils.GetLevelFolderName(data), true); 
            }

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

            __instance.syringeBodyImage.color = Color.Lerp(Color.white, colToSet, Mathf.Min((status == 10 ? 0.325f : 0.5f) * ColourAmplifier.Value, 1));
        }
    }
    
    private class PRLevels
    {
        public static Dictionary<string, sbyte> levelStatuses = [];
        public static Dictionary<string, sbyte> levelV2Statuses = [];

        public static sbyte Get(string id, bool checkIfFromV2 = false)
        {
            Dictionary<string, sbyte> dictToCheck = levelStatuses;
            if (checkIfFromV2)
                dictToCheck = levelV2Statuses;
            if (dictToCheck.TryGetValue(id, out sbyte val))
                return val;
            return -127; // means no entry (not on rdcafe or not all data got yet)
        }

        public static async Task Init()
        {
			HttpClient client = new()
			{
				Timeout = TimeSpan.FromMinutes(30)
			};
			bool gotAllSongs = false;
            int page = 1;

            Log.LogMessage("LevelPRStatus: Obtaining PR statuses...");

            while (!gotAllSongs)
            {
                HttpRequestMessage request = new(HttpMethod.Get,
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
                if (hits.Length < 250)
                    gotAllSongs = true;

                foreach (TypeSenseResponse.Hit hit in hits)
                {
                    levelStatuses.Add(hit.document.id, (sbyte)hit.document.approval);
                    levelV2Statuses.Add(hit.document.sha1[3..], (sbyte)hit.document.approval);
                }
            }

            Log.LogMessage("LevelPRStatus: PR statuses obtained!");
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
                public string sha1 { get; set; }
                public int approval { get; set; }
            }
        }
    }
}
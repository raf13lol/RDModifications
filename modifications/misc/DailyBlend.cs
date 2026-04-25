using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace RDModifications;

[Modification(
    "If enabled, pressing B in the ward screen of the Custom Levels scene will automatically open the URL downloader with the link of the daily blend(s).\n" +
    "Having this enabled also makes the syringe liquid of the daily blend(s) brown, and searching 'daily-blend' will show the daily blend(s)."
)]
public class DailyBlend : Modification
{
    public static void Init(bool enabled)
    {
        if (enabled)
            _ = RecentBlends.Init();
    }

    [HarmonyPatch(typeof(scnCLS), "Update")]
    public class BlendInstallPatch
    {
        public static bool Prefix(scnCLS __instance)
        {
            if (!__instance.ShowingWard || !__instance.CanReceiveInput || __instance.levelImporter.Showing || !Input.GetKeyDown(KeyCode.B))
                return true;

            LevelImporter levelImporter = __instance.levelImporter;
            levelImporter.Showing = true;
            levelImporter.ToggleInsertUrlContainer(true, true, false);
            levelImporter.urlInput.text = RecentBlends.BlendURLTextList;
            levelImporter.ValidateUrl();
            return false;
        }
    }

    [HarmonyPatch(typeof(scnCLS), nameof(scnCLS.SetSearchData))]
    public class BlendSearchPatch
    {
        public static void Postfix(scnCLS __instance, string textToSearch)
        {
            if (textToSearch != "daily-blend")
                return;

            for (int i = 0; i < __instance.levelsData.Count; i++ )
            {
                CustomLevelData customLevelData = __instance.levelsData[i];
                
                if (RecentBlends.BlendIDs.Contains(customLevelData.Hash))
                    __instance.searchLevelsDataIndex.Add(i);
            }
        }
    }

    [HarmonyPatch(typeof(CustomLevelSyringe), nameof(CustomLevelSyringe.UpdateInfo))]
    public class BlendAppearancePatch
    {
        public static void Postfix(CustomLevelSyringe __instance, CustomLevelData data)
        {
            bool isDailyBlend = RecentBlends.BlendIDs.Contains(data.Hash);
            Image liquid = __instance.liquidRect.gameObject.GetComponent<Image>();
            if (isDailyBlend) // Like coffee. Do you get it? It's like coffee. Like daily blend. Blend of coffee beans. Come on now. 
                liquid.color = new(205f / 156f, 127f / 148f, 50f / 241f);
            else
                liquid.color = Color.white;
        }
    }

    // this code doesn't look the best but things sometimes gotta be how they gotta be
    public class RecentBlends
    {
        public static List<string> BlendIDs = [];
        public static string BlendURLTextList = "";

        public static async Task Init()
        {
            try
            {
            using HttpClient client = new();
            using HttpRequestMessage cafeV2 = new(HttpMethod.Get, new Uri("https://rhythm.cafe/?_bridge=1"));
            cafeV2.Headers.Add("X-Requested-With", "DjangoBridge");

            using HttpResponseMessage cafeV2Response = await client.SendAsync(cafeV2);
            if (cafeV2Response.StatusCode != HttpStatusCode.OK)
                return;

            string jsonPage = await cafeV2Response.Content.ReadAsStringAsync();
            CafeV2HomePage page = JsonConvert.DeserializeObject<CafeV2HomePage>(jsonPage);
            CafeV2HomePage.DailyBlendLevel level = page.props.daily_blend_level;

            BlendIDs.Add(level.rd_md5);

            string levelURL = $"https://rhythm.cafe/levels/{level.id}/download";
            BlendURLTextList += $"{levelURL}\n";

            if (BlendIDs.Count > 0)
                Log.LogMessage("DailyBlend: Obtained daily blend(s).");
            else
                Log.LogWarning("DailyBlend: No daily blend(s) obtained.");
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }
        }

        public class CafeV2HomePage
        {
            public DailyBlendHolder props { get; set; }

            public class DailyBlendHolder
            {
                public DailyBlendLevel daily_blend_level { get; set; }
            }

            public class DailyBlendLevel
            {
                public string id { get; set; }
                // public string song { get; set; }
                // public string[] authors { get; set; }
                public string rd_md5 { get; set; }
                // public string rdzip_url { get; set; }
            }
        }
    }

}
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil.Cil;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RDModifications;

[Modification]
public class DailyBlend
{
    public static ManualLogSource logger;

    public static ConfigEntry<bool> enabled;
    public static ConfigEntry<string> token;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("DailyBlend", "Enabled", false,
        "If enabled, and a valid Discord token in put in DiscordToken that has access to the Rhythm Doctor Lounge channel, #daily-blend,\n" +
        "pressing B in the ward screen of the Custom Levels scene will automatically open the URL downloader with the link of the daily blend(s).\n" +
        "Having this enabled also makes the syringe liquid of the daily blend(s) brown, and searching 'daily-blend' will show the daily blend(s).");

        token = config.Bind("DailyBlend", "DiscordToken", "",
        "Needs to be your Discord token. You can get it in many ways.\n" +
        "If you don't trust this, which is fair, you can check the source code of this patch at\n" +
        "'https://github.com/raf13lol/RDModifications/blob/main/modifications/misc/DailyBlend.cs'.");

        if (enabled.Value && token.Value != (string)token.DefaultValue)
            _ = RecentBlends.Init();

        return enabled.Value;
    }

    [HarmonyPatch(typeof(scnCLS), "Update")]
    private class BlendInstallPatch
    {
        public static bool Prefix(scnCLS __instance)
        {
            if (!__instance.ShowingWard || !__instance.CanReceiveInput || __instance.levelImporter.Showing)
                return true;

            if (Input.GetKeyDown(KeyCode.B))
            {
                LevelImporter levelImporter = __instance.levelImporter;
                levelImporter.Showing = true;
				levelImporter.ToggleInsertUrlContainer(true, true, false);
                levelImporter.urlInput.text = RecentBlends.BlendURLTextList;
                levelImporter.ValidateUrl();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(scnCLS), nameof(scnCLS.SetSearchData))]
    private class BlendSearchPatch
    {
        public static void Postfix(scnCLS __instance, string textToSearch)
        {
            if (textToSearch != "daily-blend")
                return;

            int index = 0;
            foreach (CustomLevelData customLevelData in __instance.levelsData)
            {
                logger.LogInfo(LevelUtils.GetLevelID(customLevelData));
                if (RecentBlends.BlendIDs.Contains(LevelUtils.GetLevelID(customLevelData)))
                    __instance.searchLevelsDataIndex.Add(index);
                index++;
            }
        }
    }

    [HarmonyPatch(typeof(CustomLevel), nameof(CustomLevel.UpdateInfo))]
    private class BlendAppearancePatch
    {
        public static void Postfix(CustomLevel __instance, CustomLevelData data)
        {
            bool isDailyBlend = RecentBlends.BlendIDs.Contains(LevelUtils.GetLevelID(data));
            Image liquid = __instance.liquidRect.gameObject.GetComponent<Image>();
            if (isDailyBlend)
                liquid.color = new(205f / 156f, 127f / 148f, 50f / 241f);
            else
                liquid.color = Color.white;
        }
    }

    // this code doesn't look the best but things sometimes gotta be how they gotta be
    private class RecentBlends
    {
        public const string DAILY_BLEND_CHANNEL_ID = "517144327734951936";
        public const string BARISTA_ID = "517141120837222410";
        public const long FIFTEEN_MINUTES = 10 * 60 * 1000;
        // RDSRT3 at home and maybe future events who knows
        public static List<string> BlendIDs = [];
        public static string BlendURLTextList = "";

        public static async Task Init()
        {
            HttpClient client = new();
            string lastMessageID = "";
            bool gotBlends = false;

            bool gotABlendYet = false;
            long lastBlendEpoch = 0;
            int messageCountBetweenBlends = 0;

            while (!gotBlends)
            {
                // 100ms delay to prevent being rate limited
                await Task.Delay(100);

                string url = $"https://discord.com/api/v9/channels/{DAILY_BLEND_CHANNEL_ID}/messages?limit=100";
                if (lastMessageID != "")
                    url += $"&before={lastMessageID}";

                HttpRequestMessage request = new(HttpMethod.Get, new Uri(url));
                request.Headers.Add("Authorization", token.Value);

                HttpResponseMessage response = await client.SendAsync(request);
                if (response.StatusCode != HttpStatusCode.OK)
                    break;

                string jsonText = await response.Content.ReadAsStringAsync();
                List<Message> messages = JsonConvert.DeserializeObject<List<Message>>(jsonText);
                if (messages == null || messages.Count <= 0)
                    break;

                lastMessageID = messages.Last().id;
                // actual code for understanding the messages
                foreach (Message message in messages)
                {
                    long epochMilliseconds = long.Parse(message.id) >> 22;
                    string levelURL = "";

                    gotBlends = gotABlendYet && (messageCountBetweenBlends++ >= 5 || epochMilliseconds - lastBlendEpoch >= FIFTEEN_MINUTES);
                    if (gotBlends)
                        break;

                    if (message.embeds.Length <= 0 || message.author.id != BARISTA_ID)
                        continue;

                    foreach (Message.Embed embed in message.embeds)
                    {
                        if (levelURL != "")
                            break;
                        if (embed.fields == null || embed.fields.Length <= 0)
                            continue;

                        foreach (Message.EmbedField field in embed.fields)
                        {
                            if (field.name != "Download")
                                continue;
                            levelURL = field.value.Replace("[Link](", "").Replace(".rdzip)", ".rdzip");
                            break;
                        }
                    }

                    if (levelURL == "")
                        continue;

                    gotABlendYet = true;
                    lastBlendEpoch = epochMilliseconds;
                    messageCountBetweenBlends = 0;
                    BlendIDs.Add(levelURL.Replace("https://codex.rhythm.cafe/", "").Replace(".rdzip", ""));
                    BlendURLTextList += $"{levelURL}\n";
                }
            }
            if (BlendIDs.Count > 0)
                logger.LogMessage("DailyBlend: Obtained daily blend(s).");
            else
                logger.LogWarning("DailyBlend: No daily blend(s) obtained.");
        }

        public class Blend(string url)
        {
            public string URL = url;
            public string ID = url.Replace("https://codex.rhythm.cafe/", "").Replace(".rdzip", "");
        }

        private class Message
        {
            public Author author { get; set; }
            public string id { get; set; }
            public Embed[] embeds { get; set; }

            public class Author
            {
                public string id { get; set; }
            }

            public class Embed
            {
                public EmbedField[]? fields { get; set; }
            }

            public class EmbedField
            {
                public string name { get; set; }
                public string value { get; set; }
            }
        }
    }

}
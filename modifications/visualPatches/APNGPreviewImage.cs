using System.Collections.Generic;
using System.IO;
using APNGP;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RDModifications;

[Modification]
public class APNGPreviewImage
{
    public static ManualLogSource logger;

    public static ConfigEntry<bool> enabled;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("APNGPreviewImage", "Enabled", false,
        "If enabled, custom levels that have an APNG as their preview image will have their preview image animated.\n" +
        "(WARNING: WILL CAUSE LAG WHEN FIRST VIEWING LEVEL IN THE CURRENT SESSION)");

        return enabled.Value;
    }

    private class PreviewAPNGImagePatch
    {
        public static Dictionary<string, List<OutputFrame>> apngFrames = [];
        public static int currentFrame = 0;
        public static double frameShownTime = 0;

        private static List<OutputFrame>? GetFrames(string id)
        {
            if (apngFrames.TryGetValue(id, out List<OutputFrame> list))
                return list;
            return null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LevelDetail), nameof(LevelDetail.ShowLevelData))]
        public static void ShowLevelDataPostfix(LevelDetail __instance)
        {
            currentFrame = 0;
            frameShownTime = 0;

            string levelPath = __instance.CurrentLevelData.path;
            string imageName = __instance.CurrentLevelData.settings.previewImageName;

            string imagePath = DesktopLevelLoader.GetValidImageInPath(levelPath, imageName);
            string id = LevelUtils.GetLevelID(__instance.CurrentLevelData);
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath) || apngFrames.ContainsKey(id))
                return;
            apngFrames[id] = [];

            using FileStream stream = File.Open(imagePath, FileMode.Open);
            APNG apng = new(stream);
            if (!apng.IsAnimated)
                return;

            for (int i = 0; i < apng.FrameCount; i++)
            {
                OutputFrame frame = apng.GetFrame();
                frame.Texture.filterMode = (apng.Width == 120 && apng.Height == 85) ? FilterMode.Point : FilterMode.Trilinear;
                apngFrames[id].Add(frame);
            }
            __instance.previewImage.texture = apngFrames[id][0].Texture;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LevelDetail), "Update")]
        public static void UpdatePostfix(LevelDetail __instance)
        {
            if (__instance.cls.CurrentLevel == null)
                return;

            string id = LevelUtils.GetLevelID(__instance.CurrentLevelData);
            if (!apngFrames.ContainsKey(id) || apngFrames[id].Count <= 0)
                return;

            OutputFrame frame = apngFrames[id][currentFrame];
            __instance.previewImage.texture = frame.Texture;

            frameShownTime += Time.deltaTime;
            if (frameShownTime >= frame.FrameDuration)
            {   
                currentFrame = (currentFrame + 1) % apngFrames[id].Count;
                frameShownTime = 0;
            }
        }
    }
}
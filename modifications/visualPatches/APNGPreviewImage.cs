using System;
using System.Collections.Generic;
using System.IO;
using APNG;
using HarmonyLib;
using UnityEngine;

namespace RDModifications;

[Modification("If custom levels that have an APNG as their preview image should have their preview image animated.")]
public class APNGPreviewImage : Modification
{
    private class PreviewAPNGImagePatch
    {
        public static Dictionary<string, APNGImage> apngFrames = [];
        public static string currentID = "";
        public static int currentFrame = 0;
        public static double frameShownTime = 0;

        private static APNGImage? GetFrames(string id)
        {
            if (apngFrames.TryGetValue(id, out APNGImage image))
                return image;
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
            currentID = LevelUtils.GetLevelFolderName(__instance.CurrentLevelData);

            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath) 
            || currentID == "" || apngFrames.ContainsKey(currentID))
                return;
            apngFrames[currentID] = null;

            using FileStream stream = File.Open(imagePath, FileMode.Open);
            APNGFile apng = new(stream);
			if (!apng.IsAnimated)
                return;

            apngFrames[currentID] = new(apng);
            __instance.previewImage.texture = apngFrames[currentID].GetFrame(0).Texture;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LevelDetail), "Update")]
        public static void UpdatePostfix(LevelDetail __instance)
        {
            if (currentID == "" || !apngFrames.ContainsKey(currentID) || apngFrames[currentID] == null)
                return;

            currentFrame %= apngFrames[currentID].frameCount;
            OutputFrame frame = apngFrames[currentID].GetFrame(currentFrame);
            __instance.previewImage.texture = frame.Texture;

            frameShownTime += Time.deltaTime;
            if (frameShownTime >= frame.FrameDuration)
            {   
                currentFrame = (currentFrame + 1) % apngFrames[currentID].frameCount;
                frameShownTime = 0;
            }
        }
    }

    private class APNGImage(IAnimatedImageFile apng) : IDisposable
    {
        public IAnimatedImageFile apng = apng;
        public int frameCount = apng.FrameCount;
        public List<OutputFrame> frames = [];

        public OutputFrame GetFrame(int frameIndex)
        {
            if (apng != null)
            {
                while ((frames.Count - 1) < frameIndex)
                {
                    OutputFrame frame = apng.GetFrame();
                    frame.Texture.filterMode = (apng.Width == 120 && apng.Height == 85) ? FilterMode.Point : FilterMode.Trilinear;
                    frames.Add(frame);

                    // garbage collector! more cleaning up please!
                    if (apng.FrameCount <= frames.Count)
                        break;
                }
            }

            return frames[frameIndex];
        }

        ~APNGImage() => Dispose();

        public void Dispose()
        {
            foreach (OutputFrame frame in frames)
                UnityEngine.Object.Destroy(frame.Texture);
            apng?.Dispose();
            System.GC.SuppressFinalize(this);
        }
    }
}
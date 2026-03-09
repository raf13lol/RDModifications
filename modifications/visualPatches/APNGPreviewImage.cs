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
    public class PreviewAPNGImagePatch
    {
        public static Dictionary<string, APNGImage> APNGImages = [];
        public static string CurrentID = "";
        public static int CurrentFrame = 0;
        public static double FrameShownTime = 0;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LevelDetail), nameof(LevelDetail.ShowLevelData))]
        public static void ShowLevelDataPostfix(LevelDetail __instance)
        {
            CurrentFrame = 0;
            FrameShownTime = 0;

            string levelPath = __instance.CurrentLevelData.path;
            string imageName = __instance.CurrentLevelData.settings.previewImageName;

            string imagePath = DesktopLevelLoader.GetValidImageInPath(levelPath, imageName);
            CurrentID = LevelUtils.GetLevelFolderName(__instance.CurrentLevelData);

            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)
            || CurrentID == "" || APNGImages.ContainsKey(CurrentID))
                return;
            APNGImages[CurrentID] = null;

            using FileStream stream = File.Open(imagePath, FileMode.Open);
            APNGFile apng = new(stream);
            if (!apng.IsAnimated)
            {
                apng.Dispose();
                return;
            }

            APNGImages[CurrentID] = new(apng);
            __instance.previewImage.texture = APNGImages[CurrentID].GetFrame(0).Texture;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LevelDetail), "Update")]
        public static void UpdatePostfix(LevelDetail __instance)
        {
            if (CurrentID == "" || !APNGImages.ContainsKey(CurrentID) || APNGImages[CurrentID] == null)
                return;

            CurrentFrame %= APNGImages[CurrentID].FrameCount;
            OutputFrame frame = APNGImages[CurrentID].GetFrame(CurrentFrame);
            __instance.previewImage.texture = frame.Texture;

            FrameShownTime += Time.deltaTime;
            if (FrameShownTime >= frame.FrameDuration)
            {
                CurrentFrame = (CurrentFrame + 1) % APNGImages[CurrentID].FrameCount;
                FrameShownTime = 0;
            }
        }
    }

    public class APNGImage(IAnimatedImageFile apng) : IDisposable
    {
        public IAnimatedImageFile APNG = apng;
        public int FrameCount = apng.FrameCount;
        public List<OutputFrame> Frames = [];

        public OutputFrame GetFrame(int frameIndex)
        {
            if (APNG == null)
                return Frames[frameIndex];

            while ((Frames.Count - 1) < frameIndex)
            {
                OutputFrame frame = APNG.GetFrame();
                frame.Texture.filterMode = (APNG.Width == 120 && APNG.Height == 85) ? FilterMode.Point : FilterMode.Trilinear;
                Frames.Add(frame);

                // garbage collector! more cleaning up please!
                if (APNG.FrameCount <= Frames.Count)
                    break;
            }

            if (APNG.EndOfFrames)
            {
                APNG.Dispose();
                APNG = null;
            }

            return Frames[frameIndex];
        }

        public void Dispose()
        {
            foreach (OutputFrame frame in Frames)
                UnityEngine.Object.Destroy(frame.Texture);
            APNG?.Dispose();
            System.GC.SuppressFinalize(this);
        }
    }
}
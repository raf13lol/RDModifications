using System;
using System.Collections.Generic;
using System.IO;
using APNG;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace RDModifications;

[Modification(
	"If sleeves should be able to be animated. For more info on how to do this, consult the README.md on the github.\n" + 
	"(WARNING: Do not attempt to load images that are quite big, as this may cause a crash.)"
)]
public class AnimatedSleeves : Modification
{
	[Configuration<int>(6, "The FPS of the player 1 sleeve in slot 1.")]
    public static ConfigEntry<int> P1Slot1FPS;
	[Configuration<int>(6, "The FPS of the player 2 sleeve in slot 1.")]
    public static ConfigEntry<int> P2Slot1FPS;

    [Configuration<int>(6, "The FPS of the player 1 sleeve in slot 2.")]
    public static ConfigEntry<int> P1Slot2FPS;
	[Configuration<int>(6, "The FPS of the player 2 sleeve in slot 2.")]
    public static ConfigEntry<int> P2Slot2FPS;

	[Configuration<int>(6, "The FPS of the player 1 sleeve in slot 3.")]
    public static ConfigEntry<int> P1Slot3FPS;
	[Configuration<int>(6, "The FPS of the player 2 sleeve in slot 3.")]
    public static ConfigEntry<int> P2Slot3FPS;

	[Configuration<bool>(false, "If the FPS of an animated sleeve shouldn't change with things like the Set Speed event.")]
    public static ConfigEntry<bool> ConsistentFPS;

    private class AnimateSleevePatch
    {
        // why ???
        public static List<List<int>> rdarmFrameCount = [
            [0, 0, 0],
            [0, 0, 0]
        ];

        static void animateSleeve(Material mat, int player, int slot, bool continueAnimation = true)
        {
            AnimatedSleeve animatedSleeve = SleeveData.animatedSleeves[player][slot];
            double currentFrame = animatedSleeve.currentFrame;
            if (animatedSleeve.frames.Count <= 0)
                return;

            mat.SetTexture("_Drawing", animatedSleeve.frames[(int)Math.Floor(currentFrame)]);
            if (continueAnimation)
            {
                double elapsed = Time.deltaTime;
                if (ConsistentFPS.Value && Time.timeScale != 0)
                    elapsed /= Time.timeScale;
                animatedSleeve.currentFrame = (currentFrame + elapsed * animatedSleeve.fps) % animatedSleeve.frames.Count;
                SleeveData.animatedSleeves[player][slot] = animatedSleeve;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RDArm), "Update")]
        public static void RDArmPostfix(RDArm __instance)
        {
            if ((!__instance.playerCanUse && __instance.cpuCanUse) || __instance.cpuOwner != Character.Otto || __instance.player >= RDPlayer.CPU)
                return;

            int player = (int)__instance.player;
            int slot = Persistence.currentSlotIndex;

            animateSleeve(__instance.drawing.material, player, slot, rdarmFrameCount[player][slot] != Time.frameCount);
            rdarmFrameCount[player][slot] = Time.frameCount;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SlotUI), "Update")]
        public static void SlotUIPostfix(SlotUI __instance)
        {
            animateSleeve(__instance.arm.material, (int)RDPlayer.P1, __instance.slot);
            // update material, needed so it rebuilds the ui
            __instance.arm.material = UnityEngine.Object.Instantiate(__instance.arm.material);
            // animateSleeve(__instance.arm.material, (int)RDPlayer.P1, __instance.slot);
        }
    }

    private struct AnimatedSleeve(List<Texture2D> frames, int fps = 6)
    {
        public List<Texture2D> frames = frames;
        public int fps = fps;
        public double currentFrame = 0;
    }

    [Patch]
    private class SleeveData
    {
        public static bool hasLoaded = false;
        public static AnimatedSleeve emptySleeve = new([]);
        public static List<List<AnimatedSleeve>> animatedSleeves;

        public static void LoadAnimatedSleeve(string baseFilename, int playerIndex, int slot)
        {
            AnimatedSleeve animatedSleeve = animatedSleeves[playerIndex][slot];
            if (!File.Exists(baseFilename + "_animated.png"))
            {
                int imageIndex = 1;
                bool individualImageExists = File.Exists(baseFilename + $"_frame{imageIndex}.png");
                while (individualImageExists)
                {
                    Texture2D image = new(2, 2);
                    image.LoadImage(File.ReadAllBytes(baseFilename + $"_frame{imageIndex}.png"), true);
                    image.filterMode = FilterMode.Point;

                    animatedSleeve.frames.Add(image);
                    individualImageExists = File.Exists(baseFilename + $"_frame{++imageIndex}.png");
                }
                return;
            }

            using FileStream stream = File.Open(baseFilename + "_animated.png", FileMode.Open);
            APNGFile apng = new(stream);
    
            if (apng.IsAnimated && apng.Width == 524 && apng.Height == 40) 
            {
                for (int i = 0; i < apng.FrameCount; i++)
                {
                    Texture2D tex = apng.GetFrame().Texture;
                    tex.filterMode = FilterMode.Point;
                    animatedSleeve.frames.Add(tex);
                }
                return;
            }

            Texture2D spritesheet = apng.GetFrame().Texture;
            spritesheet.filterMode = FilterMode.Point;
            int framesAcross = spritesheet.width / 524;
            int framesDown = spritesheet.height / 40;
            int framesToAdd = framesAcross * framesDown;
            
            for (int i = 0; i < framesToAdd; i++)
            {
                int frameGridX = i % framesAcross;
                int frameGridY = framesDown - 1 - i / framesAcross;

                Texture2D frame = new(524, 40, spritesheet.format, false, false, true);
                frame.CopyPixels(spritesheet, frameGridX * 524, frameGridY * 40, 524, 40, 0, 0, true);
                frame.filterMode = FilterMode.Point;

                animatedSleeve.frames.Add(frame);
            }

            UnityEngine.Object.Destroy(spritesheet);
        }

        public static void LoadAnimatedSleeves()
        {
            RDStartup.DetermineAppLocation();
            // try ask a dev about this. i shouldn't have to do this weirdness, if it even works
            for (int playerIndex = 0; playerIndex < 2; playerIndex++)
                for (int slot = 0; slot < 3; slot++)
                    LoadAnimatedSleeve(Path.Combine(Entry.UserDataFolder, $"scribbleP{playerIndex + 1}_{slot}"), playerIndex, slot);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(RDStartup), nameof(RDStartup.Setup))]
        public static void StartupPrefix() 
        {
            if (!RDStartup.hasInitialized && !hasLoaded)
            {
                hasLoaded = true;

                // create here
                animatedSleeves = [
                    [new([], P1Slot1FPS.Value), new([], P1Slot2FPS.Value), new([], P1Slot3FPS.Value)],
                    [new([], P2Slot1FPS.Value), new([], P2Slot2FPS.Value), new([], P2Slot3FPS.Value)]
                ];
                LoadAnimatedSleeves();
            }
        }
    }
}
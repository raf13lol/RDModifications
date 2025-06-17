using System;
using System.Collections.Generic;
using System.IO;
using APNGP;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RDModifications;

[Modification]
public class AnimatedSleeves
{
    public static ManualLogSource logger;

    public static ConfigEntry<bool> enabled;

    public static ConfigEntry<int> p1Slot1FPS;
    public static ConfigEntry<int> p2Slot1FPS;

    public static ConfigEntry<int> p1Slot2FPS;
    public static ConfigEntry<int> p2Slot2FPS;

    public static ConfigEntry<int> p1Slot3FPS;
    public static ConfigEntry<int> p2Slot3FPS;

    public static ConfigEntry<bool> consistentFPS;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("AnimatedSleeves", "Enabled", false,
        "If sleeves should be able to be animated. For more info on how to do this, consult the README.md on the github.");

        p1Slot1FPS = config.Bind("AnimatedSleeves", "P1Slot1FPS", 6,
        "The fps of the player 1 sleeve in slot 1.");
        p2Slot1FPS = config.Bind("AnimatedSleeves", "P2Slot1FPS", 6,
        "The fps of the player 2 sleeve in slot 1.");

        p1Slot2FPS = config.Bind("AnimatedSleeves", "P1Slot2FPS", 6,
        "The fps of the player 1 sleeve in slot 2.");
        p2Slot2FPS = config.Bind("AnimatedSleeves", "P2Slot2FPS", 6,
        "The fps of the player 2 sleeve in slot 2.");

        p1Slot3FPS = config.Bind("AnimatedSleeves", "P1Slot3FPS", 6,
        "The fps of the player 1 sleeve in slot 3.");
        p2Slot3FPS = config.Bind("AnimatedSleeves", "P2Slot3FPS", 6,
        "The fps of the player 2 sleeve in slot 3.");

        consistentFPS = config.Bind("AnimatedSleeves", "ConsistentFPS", false,
        "If the fps of an animated sleeve shouldn't change with things like the Set Speed event.");

        return enabled.Value;
    }

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
                if (consistentFPS.Value && Time.timeScale != 0)
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
            APNG apng = new(stream);
    
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

            Texture2D spritesheet = new(2, 2, TextureFormat.ARGB32, false, false, true);
            spritesheet.LoadImage(File.ReadAllBytes(baseFilename + $"_animated.png"), false);
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
            
            string baseFilePath = Persistence.GetSaveFileFolderPath() + Path.DirectorySeparatorChar + "scribble";
            for (int playerIndex = 0; playerIndex < 2; playerIndex++)
                for (int slot = 0; slot < 3; slot++)
                    LoadAnimatedSleeve(baseFilePath + $"P{playerIndex + 1}_{slot}", playerIndex, slot);
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
                    [new([], p1Slot1FPS.Value), new([], p1Slot2FPS.Value), new([], p1Slot3FPS.Value)],
                    [new([], p2Slot1FPS.Value), new([], p2Slot2FPS.Value), new([], p2Slot3FPS.Value)]
                ];
                LoadAnimatedSleeves();
            }
        }
    }
}
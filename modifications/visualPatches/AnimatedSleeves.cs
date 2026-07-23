using System;
using System.Collections.Generic;
using System.IO;
using APNG;
using BepInEx.Configuration;
using GIF;
using HarmonyLib;
using UnityEngine;

namespace RDModifications;

[Modification(
    "If sleeves should be able to be animated. For more info on how to do this, consult the README.md on the github.\n" +
    "Note that if the FPS of a sleeve is set to 0 and an animated sleeve is done via an APNG or a GIF,\n" +
    "it will use the FPS of the file, otherwise it will use 6 FPS for that sleeve.\n" +
    "(WARNING: Do not attempt to load images that are quite big, as this may cause a crash.)"
)]
public class AnimatedSleeves : Modification
{
    [Configuration<double>(6, "The FPS of the player 1 sleeve in slot 1.", [0, double.MaxValue])]
    public static ConfigEntry<double> P1Slot1FPS;
    [Configuration<double>(6, "The FPS of the player 2 sleeve in slot 1.", [0, double.MaxValue])]
    public static ConfigEntry<double> P2Slot1FPS;

    [Configuration<double>(6, "The FPS of the player 1 sleeve in slot 2.", [0, double.MaxValue])]
    public static ConfigEntry<double> P1Slot2FPS;
    [Configuration<double>(6, "The FPS of the player 2 sleeve in slot 2.", [0, double.MaxValue])]
    public static ConfigEntry<double> P2Slot2FPS;

    [Configuration<double>(6, "The FPS of the player 1 sleeve in slot 3.", [0, double.MaxValue])]
    public static ConfigEntry<double> P1Slot3FPS;
    [Configuration<double>(6, "The FPS of the player 2 sleeve in slot 3.", [0, double.MaxValue])]
    public static ConfigEntry<double> P2Slot3FPS;

    [Configuration<bool>(true, "If the FPS of an animated sleeve shouldn't change with things like the Set Speed event.")]
    public static ConfigEntry<bool> ConsistentFPS;

    [Configuration<bool>(false, "If the FPS should be linked with the current song's BPM.")]
    public static ConfigEntry<bool> BPMFPS;

    public class AnimateSleevePatch
    {
        // why ???
        public static List<List<int>> rdarmFrameCount = [
            [0, 0, 0],
            [0, 0, 0]
        ];

        static void animateSleeve(Material mat, int player, int slot, bool continueAnimation = true)
        {
            AnimatedSleeve animatedSleeve = SleeveData.animatedSleeves[player][slot];
            if (animatedSleeve.frames.Count <= 0)
                return;

            double fps = animatedSleeve.fps;

            double currentFrame = animatedSleeve.currentFrame;
            int frameIndex = animatedSleeve.frameIndex;
            int displayFrame = fps == 0 ? frameIndex : (int)Math.Floor(currentFrame);

            mat.SetTexture("_Drawing", animatedSleeve.frames[displayFrame]);
            if (!continueAnimation)
                return;

            double elapsed = Time.deltaTime;
            if (ConsistentFPS.Value && Time.timeScale != 0)
                elapsed /= Time.timeScale;
            if (BPMFPS.Value)
                elapsed /= scrConductor.instance.visualCrotchet;

            if (fps > 0d)
            {
                animatedSleeve.currentFrame = (currentFrame + elapsed * fps) % animatedSleeve.frames.Count;
                return;
            }

            currentFrame += elapsed;
            if (currentFrame >= animatedSleeve.frameLengths[frameIndex])
            {
                animatedSleeve.frameIndex = (frameIndex + 1) % animatedSleeve.frames.Count;
                animatedSleeve.currentFrame = 0d;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RDArm), "Update")]
        public static void RDArmPostfix(RDArm __instance)
        {
            // multiplayer mod
            bool isSpecialBeans = __instance.cpuOwner == Character.Beans;
            if (!__instance.playerCanUse && __instance.cpuCanUse && !isSpecialBeans)
                return;
            if (__instance.cpuOwner != Character.Otto && !isSpecialBeans)
                return;
            if (__instance.player >= RDPlayer.CPU)
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

    public class AnimatedSleeve(List<Texture2D> frames, double fps = 6)
    {
        public List<Texture2D> frames = frames;
        public double fps = fps;
        public double currentFrame = 0;

        public int frameIndex = 0;
        public List<double> frameLengths = [];
    }

    [Patch]
    public class SleeveData
    {
        public static bool hasLoaded = false;
        public static AnimatedSleeve emptySleeve = new([]);
        public static List<List<AnimatedSleeve>> animatedSleeves;

        public static void LoadAnimatedSleeve(string baseFilename, int playerIndex, int slot)
        {
            AnimatedSleeve animatedSleeve = animatedSleeves[playerIndex][slot];

            bool useGIF = File.Exists(baseFilename + "_animated.gif");
            if (!File.Exists(baseFilename + "_animated.png") && !useGIF)
            {
                if (animatedSleeve.fps == 0)
                    animatedSleeve.fps = 6;

                int imageIndex = 1;
                string filename = baseFilename + $"_frame{imageIndex}.png";

                bool individualImageExists = File.Exists(filename);
                while (individualImageExists)
                {
                    animatedSleeve.frames.Add(Tex2DUtils.LoadImage(File.ReadAllBytes(filename), true));

                    filename = baseFilename + $"_frame{++imageIndex}.png";
                    individualImageExists = File.Exists(filename);
                }
                return;
            }

            using FileStream stream = File.Open(baseFilename + $"_animated.{(useGIF ? "gif" : "png")}", FileMode.Open);
            using IAnimatedImageFile image = useGIF ? new GIFFile(stream) : new APNGFile(stream);

            if (image.IsAnimated && image.Width == 524 && image.Height == 40)
            {
                for (int i = 0; i < image.FrameCount; i++)
                {
                    if (animatedSleeve.fps == 0)
                        animatedSleeve.frameLengths.Add(image.CurrentFrameDuration);
                    Texture2D tex = image.GetFrame().Texture;
                    animatedSleeve.frames.Add(tex);
                }
                return;
            }

            if (animatedSleeve.fps == 0)
                animatedSleeve.fps = 6;

            Texture2D spritesheet = image.GetFrame().Texture;
            int framesAcross = spritesheet.width / 524;
            int framesDown = spritesheet.height / 40;
            int framesToAdd = framesAcross * framesDown;

            for (int i = 0; i < framesToAdd; i++)
            {
                int frameGridX = i % framesAcross;
                int frameGridY = framesDown - 1 - i / framesAcross;

                Texture2D frame = new(524, 40, spritesheet.format, false, false, true)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                frame.CopyPixels(spritesheet, frameGridX * 524, frameGridY * 40, 524, 40, 0, 0, true);

                animatedSleeve.frames.Add(frame);
            }

            UnityEngine.Object.Destroy(spritesheet);
        }

        public static void LoadAnimatedSleeves()
        {
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
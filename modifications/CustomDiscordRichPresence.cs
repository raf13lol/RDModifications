using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System.Reflection;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace RDModifications
{
    public class CustomDiscordRichPresence
    {
        public static ConfigEntry<long> discordClientID;
        public static ConfigEntry<string> largeImageKey;
        public static ConfigEntry<string> largeImageText;

        public static ManualLogSource logger;

        public static void Init(Harmony patcher, ConfigFile config, ManualLogSource logging, ref bool anyEnabled)
        {
            logger = logging;

            discordClientID = config.Bind("CustomDiscordRichPresence", "DiscordClientID", 477926053420072961L, 
            "The client ID that should be used for the game's rich presence.\n" +
            "(Recommended to only change this if you know what you're doing.)");

            largeImageKey = config.Bind("CustomDiscordRichPresence", "LargeImageKey", "rhythm_doctor_icon_for_fb_png", 
            "The key that should be used for the rich presence image.");

            largeImageText = config.Bind("CustomDiscordRichPresence", "LargeImageText", "Samurai.", 
            "The text that should be used for the rich presence image when you hover over it.");

            patcher.PatchAll(typeof(TextPatch));
            if (discordClientID.Value != 477926053420072961L)
            {
                patcher.PatchAll(typeof(IDPatch));
                patcher.PatchAll(typeof(ImagePatch));
            }
            anyEnabled = true;
        }

        // easy !
        [HarmonyPatch(typeof(DiscordController), "OnEnable")]
        private class IDPatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Ldc_I8, 477926053420072961L))
                    .SetOperandAndAdvance(discordClientID.Value)
                    .InstructionEnumeration();
            }
        }

        [HarmonyPatch(typeof(DiscordController), nameof(DiscordController.UpdatePresence))]
        private class ImagePatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Ldstr, "rhythm_doctor_icon_for_fb_png"))
                    .SetOperandAndAdvance(largeImageKey.Value)
                    .InstructionEnumeration();
            }
        }

        [HarmonyPatch(typeof(DiscordController), nameof(DiscordController.UpdatePresence))]
        private class TextPatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Ldstr, "Samurai."))
                    .SetOperandAndAdvance(largeImageText.Value)
                    .InstructionEnumeration();
            }
        }
    }
}
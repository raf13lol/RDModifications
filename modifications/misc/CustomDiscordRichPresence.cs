using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace RDModifications;

[Modification("If you should be able to configure information about your Discord rich presence.")]
public class CustomDiscordRichPresence : Modification
{
	[Configuration<long>(477926053420072961L, 
		"The client ID that should be used for the game's rich presence.\n" +
        "(Recommended to only change this if you know what you're doing.)"
	)]
    public static ConfigEntry<long> DiscordClientID;

	[Configuration<string>("rhythm_doctor_icon_for_fb_png", "The key that should be used for the rich presence image.")]
    public static ConfigEntry<string> LargeImageKey;

	[Configuration<string>("Samurai.", "The text that should be used for the rich presence image when you hover over it.")]
    public static ConfigEntry<string> LargeImageText;

    // easy !
    [HarmonyPatch(typeof(RDRichPresence_Discord), "TryInitDiscord")]
    private class IDPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Ldc_I8, 477926053420072961L))
                .SetOperandAndAdvance(DiscordClientID.Value)
                .InstructionEnumeration();
        }
    }

    private class LargeImagePatch
    {
		[HarmonyTranspiler]
    	[HarmonyPatch(typeof(RDRichPresence_Discord), nameof(RDRichPresence_Discord.SetPresence))]
        public static IEnumerable<CodeInstruction> KeyTranspiler(IEnumerable<CodeInstruction> instructions)
        	=> TranspilerUtils.ReplaceString(instructions, (string)LargeImageKey.DefaultValue, LargeImageKey.Value);

		[HarmonyTranspiler]
    	[HarmonyPatch(typeof(RDRichPresence_Discord), nameof(RDRichPresence_Discord.SetPresence))]
        public static IEnumerable<CodeInstruction> TextTranspiler(IEnumerable<CodeInstruction> instructions)
        	=> TranspilerUtils.ReplaceString(instructions, (string)LargeImageText.DefaultValue, LargeImageText.Value);
    }
}
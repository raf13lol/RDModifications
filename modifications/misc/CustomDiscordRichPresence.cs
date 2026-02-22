using BepInEx.Configuration;
using HarmonyLib;
using MonoMod.Cil;

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
        public static void ILManipulator(ILContext il)
        {
			ILCursor cursor = new(il);
			cursor.GotoNext(x => x.MatchLdcI8(477926053420072961L));
			cursor.Next.Operand = DiscordClientID.Value;
        }
    }

    private class LargeImagePatch
    {
		[HarmonyILManipulator]
    	[HarmonyPatch(typeof(RDRichPresence_Discord), nameof(RDRichPresence_Discord.SetPresence))]
        public static void KeyILManipulator(ILContext il)
        	=> ILManipulatorUtils.ReplaceString(il, (string)LargeImageKey.DefaultValue, LargeImageKey.Value);

		[HarmonyILManipulator]
    	[HarmonyPatch(typeof(RDRichPresence_Discord), nameof(RDRichPresence_Discord.SetPresence))]
        public static void TextILManipulator(ILContext il)
        	=> ILManipulatorUtils.ReplaceString(il, (string)LargeImageText.DefaultValue, LargeImageText.Value);
    }
}
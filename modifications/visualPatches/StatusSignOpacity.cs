using BepInEx.Configuration;
using DG.Tweening;
using HarmonyLib;
using RDLevelEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RDModifications;

[Modification("If the opacity of the status sign elements should be modified.")]
public class StatusSignOpacity : Modification
{
	[Configuration<float>(1f, 
	"How opaque the status sign background should be.\n" +
	"(Value should be between 0 and 1.)"
	)]
	public static ConfigEntry<float> BackgroundOpacity;

	[Configuration<float>(1f, 
	"How opaque the status sign text should be.\n" +
	"(Value should be between 0 and 1.)"
	)]
	public static ConfigEntry<float> TextOpacity;

	[HarmonyPatch(typeof(LEDSign), "Awake")]
    private class OpacityPatch
    {
        public static void Postfix(LEDSign __instance, RectTransform ___rect)
        {
			if (scnBase.instance is scnCLS)
				return;
			foreach (Image image in ___rect.GetComponentsInChildren<Image>())
			 	image.color = image.color.WithAlpha(BackgroundOpacity.Value);
			__instance.message.color = __instance.message.color.WithAlpha(TextOpacity.Value);
        }
    }
}
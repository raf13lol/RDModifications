using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace RDModifications;

[Modification("If the custom-defined ice/chili speeds should be used, also allows using custom speeds on any level.")]
public class CustomIceChiliSpeeds : Modification
{
	[Configuration<float>(0.75f, "The speed multiplier to use for ice speeds.")]
	public static ConfigEntry<float> IceSpeed;

	[Configuration<float>(1.5f, "The speed multiplier to use for chili speeds.")]
	public static ConfigEntry<float> ChiliSpeed;

	[Configuration<bool>(true, "If the rankscreen colours should adjust depending on the speed more accurately.")]
	public static ConfigEntry<bool> ChangeRankScreen;

	public static void Init(bool enabled)
	{
		if (enabled && IceSpeed.Value == 0.75f && ChiliSpeed.Value == 1.5f)
			Log.LogMessage("CustomIceChiliSpeeds: All values are default. No differences from base game.");
		if (IceSpeed.Value <= 0.00f || IceSpeed.Value >= 1.00f)
		{
			IceSpeed.Value = 0.75f;
			Log.LogWarning("CustomIceChiliSpeeds: Invalid IceSpeed, value is reset to 0.75x");
		}
		if (ChiliSpeed.Value <= 1.00f)
		{
			ChiliSpeed.Value = 1.5f;
			Log.LogWarning("CustomIceChiliSpeeds: Invalid ChiliSpeed, value is reset to 1.5x");
		}
	}

	[HarmonyPatch(typeof(HeartMonitor), nameof(HeartMonitor.Show))]
	private class SpeedAnyPatch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			=> new CodeMatcher(instructions)
				.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)Level.Montage))
				.Advance(-2)
				.RemoveInstructions(6)
				.InstructionEnumeration();
	}

	[HarmonyPatch(typeof(scnGame), nameof(scnGame.StartTheGame))]
	private class GameSpeedPatch
	{
		public static void Prefix(ref float speed)
		{
			if (speed == 0.75f)
				speed = IceSpeed.Value;
			else if (speed == 1.5f)
				speed = ChiliSpeed.Value;
		}
	}

	[HarmonyPatch(typeof(Persistence), "GetCustomLevelKey")]
	private class SaveSpeedPatch
	{
		public static void Postfix(ref string __result, ref float speed)
		{
			string text = "normal";
			if (speed >= 1.05f)
				text = "chili";
			else if (speed <= 0.95f)
				text = "ice";

			// this'll work as we only need to fix the _normal
			__result = __result.Replace("_normal", "_" + text);
		}
	}

	[HarmonyPatch(typeof(Rankscreen), nameof(Rankscreen.ShowAndSaveRank))]
	private static class RankScreenPatch
	{
		public static void Postfix(Rankscreen __instance)
		{
			if (RDTime.speed == 1f || !ChangeRankScreen.Value)
				return;

			Color iceColor = "70D8ED".HexToColor();
			Color chiliColor = "ED7070".HexToColor();

			Color normalColor = Color.white;
			Color targetColor = chiliColor;
			float intensifier = (RDTime.speed - 1f) / 0.5f;

			if (RDTime.speed < 1f)
			{
				targetColor = iceColor;
				intensifier = (1f - RDTime.speed) / 0.25f;
			}
			intensifier = halfValuePast1(intensifier);

			Color endColor = new(
				Mathf.LerpUnclamped(normalColor.r, targetColor.r, intensifier),
				Mathf.LerpUnclamped(normalColor.g, targetColor.g, intensifier),
				Mathf.LerpUnclamped(normalColor.b, targetColor.b, intensifier)
			);

			// should be safe to do so aswell
			__instance.rank.color = endColor;
			__instance.customText.color = endColor;
			__instance.vividStasisRank.color = endColor;
		}

		private static float halfValuePast1(float value)
		{
			if (value > 1f)
				return ((value - 1f) / 2f) + 1f;
			return value;
		}
	}

	private class CLSAudioSpeedPatch
	{
		public static IEnumerable<MethodInfo> TargetMethods()
		{
			yield return AccessTools.Method(typeof(LevelDetail), "Start");
			yield return AccessTools.Method(typeof(LevelDetail), "ChangeLevelSpeed");
		}

		[HarmonyPostfix]
		public static void ASPostfix(AudioSource ___audioSource)
		{
			if (___audioSource.pitch == 0.75f)
				___audioSource.pitch = IceSpeed.Value;
			else if (___audioSource.pitch == 1.5f)
				___audioSource.pitch = ChiliSpeed.Value;
		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RDLevelEditor;
using UnityEngine;

namespace RDModifications;

[Modification("If to fix bugs within the game.")]
public class GameplayBugs : Modification
{
    private class SetGameSoundCompatibilityPatch
    {
		[HarmonyPostfix]
		[HarmonyPatch(typeof(LevelEvent_SetGameSound), nameof(LevelEvent_SetGameSound.Decode))]
		public static void DecodePostfix(LevelEvent_SetGameSound __instance, Dictionary<string, object> dict)
        {
            if (dict.ContainsKey("soundSubtypes"))
				return;
			if (!RDEditorConstants.gameSoundGroups.TryGetValue(__instance.soundType, out GameSoundType[] array))
				return;
			for (int i = 0; i <__instance.sounds.Length; i++)
				__instance.sounds[i].groupSubtype = __instance.sounds[i].used ? array[i] : (GameSoundType)int.MaxValue;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(LevelEvent_SetGameSound), nameof(LevelEvent_SetGameSound.Run))]
		public static void RunPrefix(LevelEvent_SetGameSound __instance)
        {
			for (int i = 0; i <__instance.sounds.Length; i++)
			{
				if (!__instance.sounds[i].used || __instance.sounds[i].groupSubtype != (GameSoundType)int.MaxValue)
					continue;
				__instance.sounds[i].used = false;
			}
		}
    }

	[HarmonyPatch(typeof(CustomAnimation), nameof(CustomAnimation.UpdateMesh))]
    private class PivotEaseBugPatch
    {
		public static void Prefix(CustomAnimation __instance, out int __state)
        {
			__state = __instance.clipFrame;
            __instance.clipFrame = Mathf.Clamp(__state, 0, __instance.currentClip.frames.Length - 1);
        }

		public static void Postfix(CustomAnimation __instance, int __state)
			=> __instance.clipFrame = __state;
    }

	[HarmonyPatch(typeof(scnGame), "Start")]
    private class HardcodedLevelsPatch
    {
    	public static void ILManipulator(ILContext il)
        {
            ILCursor cursor = new(il);
			cursor.GotoNext(
				x => x.MatchCall(AccessTools.Method(typeof(Conditionals), nameof(Conditionals.GetGlobalConditionals))),
				x => x.MatchLdloc(13),
				x => x.MatchLdfld(AccessTools.Field(typeof(RDLevelData), nameof(RDLevelData.conditionals)))
			);
			cursor.RemoveRange(5);
			cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(HardcodedLevelsPatch), nameof(GetConditionals)));
        }

		public static List<Conditional> GetConditionals()
        {
            List<Conditional> conds = Conditionals.GetGlobalConditionals();
			if (RDLevelData.current != null)
				conds = [.. conds, .. RDLevelData.current.conditionals];
			return conds;
        }
    }

    private class SetCountingSoundPatch
    {
		// I've done some bullshit because otherwise it just doesn't work?
		public static MethodInfo TargetMethod()
			=> AccessUtils.GetFirstInnerMethodContains(typeof(LevelEvent_SetCountingSound), "<Prepare>", "MoveNext");

		[HarmonyPostfix]
    	public static void Postfix(IEnumerator __instance, bool __result)
        {
			if (__result)
				return;

			LevelEvent_SetCountingSound levelEvent = (LevelEvent_SetCountingSound)AccessUtils.GetFirstFieldContains(__instance.GetType(), "this").GetValue(__instance);
			if (levelEvent.voiceSource != CountingVoiceSource.Custom)
				return;

			SoundData[] soundsData = (SoundData[])AccessTools.Field(typeof(LevelEvent_SetCountingSound), "soundsData").GetValue(levelEvent);
			foreach (SoundData soundData in soundsData)
			{
				if (soundData == null || soundData.filename.HasAudioFileExtension())
					continue;

				if (soundData.itsASong || !soundData.filename.StartsWith("sndsnd"))
					continue;
					
				soundData.itsASong = true;
				soundData.filename = soundData.filename.Replace("sndsnd", "snd");
			}
		}
    }
}
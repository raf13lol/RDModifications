using System;
using System.Collections.Generic;
using HarmonyLib;
using RDLevelEditor;
using UnityEngine;

namespace RDModifications;

[Modification("If to fix bugs within the game.")]
public class GameplayBugs : Modification
{
	[HarmonyPatch(typeof(LevelEvent_SetGameSound), nameof(LevelEvent_SetGameSound.Decode))]
    private class SetGameSoundCompatibilityPatch
    {
		public static void Postfix(LevelEvent_SetGameSound __instance, Dictionary<string, object> dict)
        {
            if (dict.ContainsKey("soundSubtypes"))
				return;
			if (!RDEditorConstants.gameSoundGroups.TryGetValue(__instance.soundType, out GameSoundType[] array))
				return;

			for (int i = 0; i <__instance.sounds.Length; i++)
			{
				if (__instance.sounds[i].used)
				{
					__instance.soundType = array[i];
					return;
				}
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
}
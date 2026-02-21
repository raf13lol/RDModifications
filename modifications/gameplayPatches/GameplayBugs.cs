using System.Collections.Generic;
using HarmonyLib;
using RDLevelEditor;

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
}
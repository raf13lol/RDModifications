using HarmonyLib;
using RDLevelEditor;

namespace RDModifications;

[Modification("If to fix the bugs in the editor.", true)]
public class EditorBugs : Modification
{
	[HarmonyPatch(typeof(LevelEvent_Base), nameof(LevelEvent_Base.isSpriteTabEvent), MethodType.Getter)]
	private class IsSpriteTabCommentPatch
    {
        public static void Postfix(LevelEvent_Base __instance, ref bool __result)
        {
            if (!__result || __instance is not LevelEvent_Comment comment)
				return;
			__result = comment.tab == Tab.Sprites;
        }
    }

	private class Separate2PLevelSetCountingSoundPatch
    {
		public static RDLevelData levelDataToUse = null;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.ForceSave))]
        public static void ExportPrefix(RDLevelData rdLevelData)
        	=> levelDataToUse = rdLevelData;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.ForceSave))]
        public static void ExportPostfix()
        	=> levelDataToUse = null;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(LevelEvent_SetCountingSound), nameof(LevelEvent_SetCountingSound.EnableSubdivOffsetIf))]
		public static bool EnablePrefix(LevelEvent_SetCountingSound __instance, ref bool __result)
        {
			__result = false;
            if (levelDataToUse == null)
				return __instance.row < __instance.editor.rowsData.Count;

			if (!__instance.IfEnabled() || __instance.row >= levelDataToUse.rows.Count)
				return false;
				
			__result = levelDataToUse.rows[__instance.row].rowType == RowType.Oneshot;
			return false;
        }
	}	

	
}
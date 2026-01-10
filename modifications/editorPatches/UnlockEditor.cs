using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;

#if !BPE5
using BepInEx.Unity.Mono.Bootstrap;
#else
using BepInEx.Bootstrap;
#endif
using HarmonyLib;
using RDLevelEditor;

namespace RDModifications;

[Modification("If most developer-exclusive functions of the editor should be unlocked.", true)]
public class UnlockEditor : Modification
{
	public static bool UnlockEditorExists = false;

	public static bool Init(bool enabled)
    {
		#if !BPE5
		UnlockEditorExists = UnityChainloader.Instance.Plugins.ContainsKey("wtf.seq.unlockeditor");
		#else
		UnlockEditorExists = Chainloader.PluginInfos.ContainsKey("wtf.seq.unlockeditor");
		#endif
		if (!enabled)
			return false;
		// check for seq's mod and if it does exist don't do the EnableDevEventStuffPatch because it already does what we do
		// and if they have the direct mod then let it do its thing
		return !UnlockEditorExists;
    }

	// THANK YOU SEQ FOR ALLOWING ME TO PORT UNLOCKEDITOR ! https://gist.github.com/lithiumjs/847ce77f3888585ad2d7c0fcd5041b83

	[HarmonyPatch(typeof(RDBase), nameof(RDBase.isDev), MethodType.Getter)]
    private class DevPatch
    {
        public static int OverrideDev = 0;

        public static bool Prefix(ref bool __result)
        {
            if (OverrideDev > 0)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    private class FunctionsPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Constructor(typeof(RDLevelData), [typeof(Dictionary<string, object>), typeof(bool), typeof(bool)]);
            yield return AccessTools.Method(typeof(LevelEvent_NarrateRowInfo), nameof(LevelEvent_NarrateRowInfo.EnableLengthIf));
            yield return AccessTools.Method(typeof(LevelEvent_ReorderRow), nameof(LevelEvent_ReorderRow.EnableSortingOrderIf));
            yield return AccessTools.Method(typeof(LevelEvent_AddClassicBeat), nameof(LevelEvent_AddClassicBeat.EnableLengthIf));
            yield return AccessTools.Method(typeof(LevelEvent_SetTheme), nameof(LevelEvent_SetTheme.EnableFirstRowOnFloorIf));
            yield return AccessTools.Method(typeof(LevelEvent_HideWindow), nameof(LevelEvent_HideWindow.EnableIfDev));
            yield return AccessTools.Method(typeof(RDStartup), "LoadLevelEditorProperties");
            yield return AccessTools.Method(typeof(InspectorPanel_MakeRow), nameof(InspectorPanel_MakeRow.Awake));
            yield return AccessTools.Method(typeof(InspectorPanel_AddOneshotBeat), nameof(InspectorPanel_AddOneshotBeat.Awake));
            yield return AccessTools.Method(typeof(InspectorPanel_ShowDialogue), "Update");
            yield return AccessTools.Method(typeof(InspectorPanel_MoveCamera), "Update");
            yield return AccessTools.Method(typeof(LevelBase), "GoToLevelWithWarning");
            yield return AccessTools.Method(typeof(LevelEventControlEventTrigger), nameof(LevelEventControlEventTrigger.OnDrag));
        }

		[HarmonyPrefix]
        public static void Prefix()
        	=> DevPatch.OverrideDev++;

		[HarmonyFinalizer]
        public static void Finalizer()
            => DevPatch.OverrideDev--;
    }
}
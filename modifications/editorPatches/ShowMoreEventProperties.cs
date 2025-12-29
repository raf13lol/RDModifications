using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RDLevelEditor;

namespace RDModifications;

[Modification("If some properties of events that are hidden, e.g. some properties that are a spoiler for 1.0, should be shown.", true)]
public class ShowMoreEventProperties : Modification
{
	public static bool UnlockEditorExists = false;

	public static bool Init(bool enabled)
    {
		UnlockEditorExists = File.Exists(Path.Combine(Path.GetDirectoryName(RDModificationsEntry.PluginInfo.Location), "UnlockEditor.dll"));;
        if (!enabled)
			return false;
		// check for seq's mod and if it does exist don't do the EnableDevEventStuffPatch because it already does what we do
		// i would like to implement UE in this but like that's kinda like copying and i have not the will to
		// ask him because... I can't really start a conversation with people that i don't speak to regularly...
		return !UnlockEditorExists;
    }

    private class EnableDevEventStuffPatch
    {
		[HarmonyPatch(typeof(LevelEvent_HideWindow), nameof(LevelEvent_HideWindow.EnableIfDev))]
		[HarmonyPatch(typeof(LevelEvent_AddClassicBeat), nameof(LevelEvent_AddClassicBeat.EnableLengthIf))]
		[HarmonyPatch(typeof(LevelEvent_NarrateRowInfo), nameof(LevelEvent_NarrateRowInfo.EnableLengthIf))]
		[HarmonyPatch(typeof(LevelEvent_MakeRow), nameof(LevelEvent_MakeRow.EnableLengthIf))]
        public static void Postfix(ref bool __result)
            => __result = true;

		[HarmonyTranspiler]
        [HarmonyPatch(typeof(InspectorPanel_MakeRow), nameof(InspectorPanel_MakeRow.Awake))]
        public static IEnumerable<CodeInstruction> InspectorPanelTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo isDevFunc = AccessTools.PropertyGetter(typeof(RDBase), "isDev");

            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Call, isDevFunc))
                // .Advance(-1)
                .RemoveInstruction()
                .InsertAndAdvance([new(OpCodes.Ldc_I4_1)])
                .InstructionEnumeration();
        }
    }
}
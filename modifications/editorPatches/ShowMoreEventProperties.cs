using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using RDLevelEditor;

namespace RDModifications;

[EditorModification]
public class ShowMoreEventProperties
{
    public static ManualLogSource logger;
    
    public static ConfigEntry<bool> enabled;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("EditorPatches", "ShowMoreEventProperties", false,
        "Shows some properties of events that were hidden before, e.g. some properties that are a spoiler for 1.0.");
        
        return enabled.Value;
    }

    [HarmonyPatch(typeof(LevelEvent_HideWindow), nameof(LevelEvent_HideWindow.EnableIfDev))]
    private class HideWindowPatch
    {
        public static void Postfix(ref bool __result)
            => __result = true;
    }

    [HarmonyPatch(typeof(LevelEvent_AddClassicBeat), nameof(LevelEvent_AddClassicBeat.EnableLengthIf))]
    private class AddClassicBeatPatch
    {
        public static void Postfix(ref bool __result)
            => __result = true;
    }

    private class MakeRowPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LevelEvent_MakeRow), nameof(LevelEvent_MakeRow.EnableLengthIf))]
        public static void LengthPostfix(ref bool __result)
            => __result = true;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InspectorPanel_MakeRow), "SaveInternal")]
        public static void SavePrefix(InspectorPanel_MakeRow __instance, ref string __state)
            => __state = __instance.length.text;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InspectorPanel_MakeRow), "SaveInternal")]
        public static void SavePostfix(InspectorPanel_MakeRow __instance, ref string __state, LevelEvent_Base levelEvent)
        {
            if (!DisableInputFieldLimits.enabled.Value)
                return;
            LevelEvent_MakeRow data = (LevelEvent_MakeRow)levelEvent;
            data.length = int.TryParse(__state, out int result) ? result : 7;
            __instance.length.text = __state;
        }

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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InspectorPanel_MakeRow), nameof(InspectorPanel_MakeRow.Awake))]
        public static void InspectorPanelPostfix(InspectorPanel_MakeRow __instance)
            => __instance.length.characterLimit = 0;
    }

    [HarmonyPatch(typeof(LevelEvent_NarrateRowInfo), nameof(LevelEvent_NarrateRowInfo.EnableLengthIf))]
    private class NarrateRowInfoPatch
    {
        public static void Postfix(ref bool __result)
            => __result = true;
    }
}
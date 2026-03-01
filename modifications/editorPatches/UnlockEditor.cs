using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RDLevelEditor;

namespace RDModifications;

[Modification("If most developer-exclusive functions of the editor should be unlocked.", true)]
public class UnlockEditor : Modification
{
    // THANK YOU SEQ FOR ALLOWING ME TO PORT UNLOCKEDITOR ! https://gist.github.com/lithiumjs/847ce77f3888585ad2d7c0fcd5041b83
    [HarmonyPatch(typeof(RDBase), nameof(RDBase.isDev), MethodType.Getter)]
    public class DevPatch
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

    public class FunctionsPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Constructor(typeof(RDLevelData), [typeof(Dictionary<string, object>), typeof(bool), typeof(bool)]);
            yield return AccessTools.Method(typeof(LevelEvent_ReorderRow), nameof(LevelEvent_ReorderRow.EnableSortingOrderIf));
            yield return AccessTools.Method(typeof(LevelEvent_SetTheme), nameof(LevelEvent_SetTheme.EnableFirstRowOnFloorIf));
            yield return AccessTools.Method(typeof(RDStartup), "LoadLevelEditorProperties");
            //yield return AccessTools.Method(typeof(InspectorPanel_MakeRow), nameof(InspectorPanel_MakeRow.Awake));
            yield return AccessTools.Method(typeof(InspectorPanel_AddOneshotBeat), nameof(InspectorPanel_AddOneshotBeat.Awake));
            yield return AccessTools.Method(typeof(LevelBase), "GoToLevelWithWarning");
        }

        [HarmonyPrefix]
        public static void Prefix()
            => DevPatch.OverrideDev++;

        [HarmonyFinalizer]
        public static void Finalizer()
            => DevPatch.OverrideDev--;
    }
}
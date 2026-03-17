using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RDLevelEditor;

namespace RDModifications;

[Modification("If the bugs within the editor should be fixed.", true)]
public class EditorBugs : Modification
{
    [HarmonyPatch(typeof(LevelEvent_Base), nameof(LevelEvent_Base.isSpriteTabEvent), MethodType.Getter)]
    public class IsSpriteTabCommentPatch
    {
        public static void Postfix(LevelEvent_Base __instance, ref bool __result)
        {
            if (!__result || __instance is not LevelEvent_Comment comment)
                return;
            __result = comment.tab == Tab.Sprites;
        }
    }

    public class Separate2PLevelSetCountingSoundPatch
    {
        public static RDLevelData LevelDataToUse = null;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.ForceSave))]
        public static void ExportPrefix(RDLevelData rdLevelData)
            => LevelDataToUse = rdLevelData;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.ForceSave))]
        public static void ExportPostfix()
            => LevelDataToUse = null;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LevelEvent_SetCountingSound), nameof(LevelEvent_SetCountingSound.EnableSubdivOffsetIf))]
        public static bool EnablePrefix(LevelEvent_SetCountingSound __instance, ref bool __result)
        {
            __result = false;
            if (LevelDataToUse == null)
                return __instance.row < __instance.editor.rowsData.Count;

            if (!__instance.IfEnabled() || __instance.row >= LevelDataToUse.rows.Count)
                return false;

            __result = LevelDataToUse.rows[__instance.row].rowType == RowType.Oneshot;
            return false;
        }
    }

    public class ClonedEventsOffsetPatch
    {
        public static bool BlockRecalcCell = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LevelEventControlEventTrigger), nameof(LevelEventControlEventTrigger.OnPointerEnter))]
        public static void Prefix(LevelEventControlEventTrigger __instance)
            => BlockRecalcCell = __instance.currentTransform != null && __instance.currentTransform != __instance.transform;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LevelEventControlEventTrigger), nameof(LevelEventControlEventTrigger.OnPointerEnter))]
        public static void Postfix()
            => BlockRecalcCell = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LevelEventControlEventTrigger), nameof(LevelEventControlEventTrigger.RecalculateCurrentCell))]
        public static bool BlockPrefix()
            => !BlockRecalcCell;
    }

    [HarmonyPatch(typeof(RDUtils), nameof(RDUtils.OpenInLinuxFileBrowser))]
    public class OpenInFolderPatch
    {
        public static void ILManipulator(ILContext il)
        {
            ILCursor cursor = new(il);

            cursor.GotoNext(x => x.OpCode == OpCodes.Brfalse_S);
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldc_I4_0);
        }
    }
}
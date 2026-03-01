using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RDLevelEditor;

namespace RDModifications;

[Modification("If to fix the bugs in the editor.", true)]
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

    [HarmonyPatch(typeof(RDPublishPopup), nameof(RDPublishPopup.GetFilesForLevel))]
    public class IncludeFilesSetCountingSoundPatch
    {
        public static void Postfix(ref string[] __result, RDLevelData levelData)
        {
            MethodInfo checkFile = AccessTools.Method(typeof(RDPublishPopup), "CheckFile");
            scnEditor editor = scnEditor.instance;
            List<LevelEvent_Base> list = levelData?.levelEvents ?? [];

            if (levelData == null)
                foreach (LevelEventControl_Base eventControl in editor.eventControls)
                    list.Add(eventControl.levelEvent);

            List<string> filesForLevel = [];
            foreach (LevelEvent_Base levelEvent in list)
            {
                if (levelEvent is not LevelEvent_SetCountingSound setCountingSound)
                    continue;

                SoundDataStruct[] sounds = setCountingSound.sounds;
                foreach (SoundDataStruct sound in sounds)
                    checkFile.Invoke(null, [filesForLevel, sound.filename]);
            }

            __result = [.. __result, .. filesForLevel];
        }
    }
}
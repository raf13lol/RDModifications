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

    [HarmonyPatch(typeof(RDPublishPopup), nameof(RDPublishPopup.GetFilesForLevel))]
    public class IncludeFilesPatch
    {
        public static void Postfix(ref string[] __result, RDLevelData levelData)
        {
            List<string> missingFiles = [];
            MethodInfo checkFile = AccessTools.Method(typeof(RDPublishPopup), "CheckFile");

            scnEditor editor = scnEditor.instance;
            List<LevelEvent_Base> events = levelData?.levelEvents ?? [];
            List<LevelEvent_MakeRow> rows = levelData?.rows ?? editor.rowsData;
            List<LevelEvent_MakeSprite> sprites = levelData?.sprites ?? editor.spritesData;

            if (levelData == null)
                foreach (LevelEventControl_Base eventControl in editor.eventControls)
                    events.Add(eventControl.levelEvent);

            void AddFreezeshotSprite(string character)
                => checkFile.Invoke(null, [missingFiles, Path.Combine(RDEditorUtils.GetCurrentLevelFolderPath(), character + "_freeze.png")]);

            foreach (LevelEvent_Base levelEvent in events)
            {
                if (levelEvent is LevelEvent_ChangeCharacter changeCharacter)
                {
                    if (string.IsNullOrEmpty(changeCharacter.customCharacter))
                        continue;
                    AddFreezeshotSprite(changeCharacter.customCharacter);
                }
            }

            foreach (LevelEvent_MakeRow row in rows)
            {
                if (row.character != Character.Custom || string.IsNullOrEmpty(row.customCharacterName))
                    continue;
                AddFreezeshotSprite(row.customCharacterName);
            }

            foreach (LevelEvent_MakeSprite sprite in sprites)
            {
                if (string.IsNullOrEmpty(sprite.filename) || sprite.filename.HasImageFileExtension())
                    continue;
                AddFreezeshotSprite(Path.GetFileNameWithoutExtension(sprite.filename));
            }

            __result = [.. __result, .. missingFiles];
        }
    }

    [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.Clone))]
    public class ClonedEventsSentToStratospherePatch
    {
        public static Type LevelState = AccessTools.Inner(typeof(scnEditor), "LevelState");
        public static List<LevelEventControl_Base> savedSelectedControls = [];

        public static void Prefix(scnEditor __instance, ref LevelEventControlEventTrigger eventTrigger)
        {
            savedSelectedControls = [.. __instance.selectedControls.Where(lec => !lec.isBase)];
            eventTrigger = null;
        }

        public static void Postfix(scnEditor __instance)
            => __instance.SelectEventControls(savedSelectedControls);
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
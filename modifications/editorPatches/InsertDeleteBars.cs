using HarmonyLib;
using RDLevelEditor;
using UnityEngine.EventSystems;
using UnityEngine;

namespace RDModifications;

[Modification("If you can insert a bar (Alt+Left Click) or delete a bar (Alt+Right Click) when clicking on the timeline to normally scrub to a position.", true)]
public class InsertDeleteBars : Modification
{
    [HarmonyPatch(typeof(TimelineEventTrigger), nameof(TimelineEventTrigger.OnPointerClick))]
    private class TimelineClickPatch
    {
        public static bool Prefix(TimelineEventTrigger __instance, PointerEventData data, bool ___isDragging)
        {
            if (data.button == PointerEventData.InputButton.Middle || ___isDragging)
                return false;
                
            if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt))
                return true;
            
            scnEditor editor = scnEditor.instance;
            Int2 cellPointedByMouse = editor.timeline.cellPointedByMouse;
            if (cellPointedByMouse.y > -1 && !editor.timeline.mouseIsOnNumbers)
                return true;

            int bar = editor.timeline.GetBarAndBeatWithPosX(editor.timeline.cellWidth * cellPointedByMouse.x, null, 0f).bar;

            using (new SaveStateScope(true, false, false))
            {
                if (data.button == PointerEventData.InputButton.Left)
                    InsertBar(editor, bar);
                else
                    DeleteBar(editor, bar);
            }
            editor.PlaySound("sndButtonRadio");

            return false;
        }

        public static void InsertBar(scnEditor editor, int bar)
        {
            foreach (LevelEventControl_Base levelEventControl in editor.eventControls)
            {
                if (levelEventControl.bar >= bar)
                    levelEventControl.bar++;
            }
            editor.timeline.UpdateUI(true);
        }

        public static void DeleteBar(scnEditor editor, int bar)
        {
            for (int i = 0; i < editor.eventControls.Count; i++)
            {
                LevelEventControl_Base levelEventControl = editor.eventControls[i];

                if (levelEventControl.bar == bar)
                {
                    editor.DeleteEventControl(levelEventControl, false, false);
                    i--;
                }
                else if (levelEventControl.bar >= bar)
                    levelEventControl.bar--;
            }
            editor.timeline.UpdateUI(true);
            // LevelEvent_PlaySong
        }
    }
}
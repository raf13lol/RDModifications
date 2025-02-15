using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using System.Reflection;
using RDLevelEditor;
using UnityEngine.EventSystems;
using UnityEngine;

namespace RDModifications;

[EditorModification]
public class InsertDeleteBars
{
    public static ManualLogSource logger;

    public static ConfigEntry<bool> enabled;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("EditorPatches", "InsertDeleteBars", false,
        "Allows you to insert a bar (Alt+Left Click) or delete a bar (Alt+Right Click) when clicking on the timeline to normally scrub to a position.");

        return enabled.Value;
    }

    [HarmonyPatch(typeof(TimelineEventTrigger), nameof(TimelineEventTrigger.OnPointerClick))]
    private class TimelineClickPatch
    {
        public static bool Prefix(TimelineEventTrigger __instance, PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Middle)
                return false;

            FieldInfo isDragging = typeof(RDEventTrigger).GetField("isDragging", BindingFlags.NonPublic | BindingFlags.Instance);
            if ((bool)isDragging.GetValue(__instance))
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
        }
    }
}
using System.Collections.Generic;
using HarmonyLib;
using RDLevelEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RDModifications;

[Modification("If there should be a button to duplicate decoration in the decoration settings panel.", true)]
public class DuplicateDecorationButton : Modification
{
    [HarmonyPatch(typeof(MakeDecorationInspectorPanel), nameof(MakeDecorationInspectorPanel.Awake))]
    public class CreateDuplicateButtonPatch
    {
        public static void Postfix(MakeDecorationInspectorPanel __instance)
        {
            GameObject deleteButton = __instance.container.Find("delete").gameObject;
            GameObject duplicateButton = Object.Instantiate(deleteButton, deleteButton.transform.parent);
            duplicateButton.name = "duplicate";

            Text buttonText = duplicateButton.GetComponentInChildren<Text>();
            buttonText.text = "Duplicate";
            foreach (Outline textOutline in duplicateButton.GetComponentsInChildren<Outline>())
                textOutline.effectColor = Color.black;

            RectTransform rectTransform = duplicateButton.GetComponent<RectTransform>();
            RectTransform deleteButtonRectTransform = deleteButton.GetComponent<RectTransform>();
            deleteButtonRectTransform.AnchorPosY(deleteButtonRectTransform.anchoredPosition.y - 17.55f);

            // horrible code to remove the delete decoration callback
            Button button = duplicateButton.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            AccessTools.Method(typeof(UnityEventBase), "DirtyPersistentCalls").Invoke(button.onClick, []);
            AccessTools.Field(typeof(UnityEventBase), "m_CallsDirty").SetValue(button.onClick, false);

            static Color createColor(float intensity) => new(intensity, intensity, intensity);

            ColorBlock buttonColors = button.colors;
            buttonColors.normalColor = createColor(201f / 255f);
            buttonColors.highlightedColor = buttonColors.selectedColor = createColor(245f / 255f);
            buttonColors.pressedColor = createColor(200f / 255f);
            buttonColors.disabledColor = buttonColors.pressedColor.WithAlpha(128f / 255f);
            button.colors = buttonColors;

            // actual code now
            button.onClick.AddListener(delegate { ButtonClick(__instance.editor); });
        }

        public static void ButtonClick(scnEditor editor)
        {
            // seems to be kinda important so let's use this
            using (new SaveStateScope(true, false, false))
            {
                editor.LevelEditorPlaySound("sndEditorPanelCreate", "LevelEditorActive", 1f, 1f, 0f);

                // get decoration
                LevelEvent_MakeDecorationBase decorationData = DecorationHeader.GetDecorationData(editor.selectedDecoration);
                int decorationDataIndex = DecorationHeader.GetDecorationDataIndex(editor.selectedDecoration);

                // make new decoration data
                LevelEvent_MakeDecorationBase newDecorationData = (LevelEvent_MakeDecorationBase)decorationData.Clone();
                int newDecorationDataIndex = decorationDataIndex + 1;
                newDecorationData.decorationId = LevelEvent_MakeDecorationBase.RandomString(7);

                // err the events themselves i think
                List<LevelEventControl_Base> decorationEventControls = editor.eventControls_decorations[decorationDataIndex];
                List<LevelEventControl_Base> newDecorationEventControls = [];

                editor.decorationsData.Insert(newDecorationDataIndex, newDecorationData);
                editor.eventControls_decorations.Insert(newDecorationDataIndex, newDecorationEventControls);

                // Move already existing events to the next index,
                // since LevelEventController_Base.controller indexes based on the event's row
                foreach (LevelEventControl_Base decorationEventControl in editor.eventControls)
                {
                    if (!decorationEventControl.levelEvent.isDecorationTabEvent)
                        continue;
                    if (decorationEventControl.levelEvent.row >= newDecorationDataIndex)
                    {
                        decorationEventControl.levelEvent.row++;
                    }
                }

                foreach (LevelEventControl_Base decorationEventControl in decorationEventControls)
                {
                    if (decorationEventControl.levelEvent.target == decorationData.decorationId)
                    {
                        LevelEvent_Base newDecorationEvent = decorationEventControl.levelEvent.Clone();
                        newDecorationEvent.target = newDecorationData.decorationId;
                        newDecorationEvent.y++;

                        newDecorationEvent.row = newDecorationDataIndex;
                        editor.CreateEventControl(newDecorationEvent, Tab.Decorations, true);
                    }
                }

                int[] indexRooms = [0, 0, 0, 0];
                foreach (LevelEventControl_Base decorationEventControl in editor.eventControls)
                {
                    if (decorationEventControl.levelEvent.isDecorationTabEvent)
                    {
                        decorationEventControl.levelEvent.y = indexRooms[DecorationHeader.GetDecorationData(decorationEventControl.levelEvent.target).room]++;
                        decorationEventControl.UpdateUI();
                    }
                }

                editor.selectedDecoration = newDecorationData.decorationId;
                editor.tabSection_decorations.UpdateUI();
            }
        }
    }
}
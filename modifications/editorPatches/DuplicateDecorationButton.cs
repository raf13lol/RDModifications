using HarmonyLib;
using RDLevelEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

namespace RDModifications;

[Modification("If there should be a button to duplicate decoration in the sprite settings panel.", true)]
public class DuplicateDecorationButton : Modification
{
    [HarmonyPatch(typeof(InspectorPanel_MakeSprite), nameof(InspectorPanel_MakeSprite.Awake))]
    private class CreateDuplicateButtonPatch
    {
        public static void Postfix(InspectorPanel_MakeSprite __instance)
        {
            GameObject deleteButton = __instance.container.Find("delete").gameObject;
            GameObject duplicateButton = UnityEngine.Object.Instantiate(deleteButton, deleteButton.transform.parent);
            duplicateButton.name = "duplicate";

            Text buttonText = duplicateButton.GetComponentInChildren<Text>();
            buttonText.text = "Duplicate";
            foreach (Outline textOutline in duplicateButton.GetComponentsInChildren<Outline>())
                textOutline.effectColor = Color.black;

            RectTransform rectTransform = duplicateButton.GetComponent<RectTransform>();
            RectTransform deleteButtonRectTransform = deleteButton.GetComponent<RectTransform>();
            deleteButtonRectTransform.AnchorPosY(deleteButtonRectTransform.anchoredPosition.y - 17.55f);

            // horrible code to remove the delete sprite callback
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

                // get sprite
                LevelEvent_MakeSprite spriteData = SpriteHeader.GetSpriteData(editor.selectedSprite);
                int spriteDataIndex = SpriteHeader.GetSpriteDataIndex(editor.selectedSprite);

                // make new sprite data
                LevelEvent_MakeSprite newSpriteData = (LevelEvent_MakeSprite)spriteData.Clone();
                int newSpriteDataIndex = spriteDataIndex + 1;
                newSpriteData.spriteId = LevelEvent_MakeSprite.RandomString(7);

                // err the events themselves i think
                List<LevelEventControl_Base> spriteEventControls = editor.eventControls_sprites[spriteDataIndex];
                List<LevelEventControl_Base> newSpriteEventControls = [];
                
                editor.spritesData.Insert(newSpriteDataIndex, newSpriteData);
                editor.eventControls_sprites.Insert(newSpriteDataIndex, newSpriteEventControls);
                
                foreach (LevelEventControl_Base spriteEventControl in spriteEventControls)
                {
                    if (spriteEventControl.levelEvent.target == spriteData.spriteId)
                    {
                        LevelEvent_Base newSpriteEvent = spriteEventControl.levelEvent.Clone();
                        newSpriteEvent.target = newSpriteData.spriteId;
                        newSpriteEvent.y++;
                        newSpriteEvent.row = newSpriteDataIndex;
                        editor.CreateEventControl(newSpriteEvent, Tab.Sprites, true);
                        // LevelEventControl_Sprite newSpriteEventControl = (LevelEventControl_Sprite)editor.CreateEventControl(newSpriteEvent, Tab.Sprites, true);
                    }
                }
                
                int index = 0;
                int[] indexRooms = [0, 0, 0, 0];
                foreach (LevelEventControl_Base spriteEventControl in editor.eventControls)
                {
                    if (spriteEventControl.levelEvent.isSpriteTabEvent) 
                    // && (spriteEventControl.levelEvent.type != LevelEventType.Comment || (spriteEventControl.levelEvent as LevelEvent_Comment).tab == Tab.Sprites))
                    {
                        spriteEventControl.levelEvent.row = index++;
                        spriteEventControl.levelEvent.y = indexRooms[SpriteHeader.GetSpriteData(spriteEventControl.levelEvent.target).room]++;
                        spriteEventControl.UpdateUI();
                    }
                }
                
                editor.selectedSprite = newSpriteData.spriteId;
                editor.UpdateUI_AllTabSections();
            }
        }
    }
}
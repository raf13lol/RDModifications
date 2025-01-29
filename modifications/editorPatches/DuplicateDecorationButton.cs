using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using System;
using System.Reflection;
using RDLevelEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace RDModifications
{
    public class DuplicateDecorationButton
    {
        public static ConfigEntry<bool> enabled;

        public static ManualLogSource logger;

        public static void Init(Harmony patcher, ConfigFile config, ManualLogSource logging, ref bool anyEnabled)
        {
            logger = logging;

            enabled = config.Bind("EditorPatches", "DuplicateDecorationButton", false,
            "If there should be a button to duplicate decoration in the Sprite Settings panel.");

            if (!EditorPatches.enabled.Value)
                return;
            if (enabled.Value)
            {
                patcher.PatchAll(typeof(CreateDuplicateButton));
                patcher.PatchAll(typeof(FixCommentBug));
                anyEnabled = true;
            }
        }

        [HarmonyPatch(typeof(InspectorPanel_MakeSprite), nameof(InspectorPanel_MakeSprite.Awake))]
        private class CreateDuplicateButton
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
                typeof(UnityEventBase).GetMethod("DirtyPersistentCalls", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(button.onClick, []);
                typeof(UnityEventBase).GetField("m_CallsDirty", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(button.onClick, false);

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
        
        [HarmonyPatch(typeof(LevelEvent_Base), nameof(LevelEvent_Base.isSpriteTabEvent), MethodType.Getter)]
        private class FixCommentBug
        {
            public static void Postfix(LevelEvent_Base __instance, ref bool __result)
            {
                if (__instance.type == LevelEventType.Comment && __instance.tab == Tab.Sprites)
                    __result = true;
            }
        }  
    }
}
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using DG.Tweening;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RDLevelEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

namespace RDModifications;

[Modification("If the current page of the Rows tab should be shown at all times.", true)]
public class GhostRowBeats : Modification
{
    [Configuration<float>(0.45f, "What the opacity of the ghost rows should be multiplied by.", [float.Epsilon, float.MaxValue])]
    public static ConfigEntry<float> GhostRowBeatsOpacityMultiplier;

    [Configuration<bool>(false, "If the ghost row events should be selectable.")]
    public static ConfigEntry<bool> GhostRowBeatsSelectable;

    [HarmonyPatch(typeof(TabSection_Rows), nameof(TabSection_Rows.Setup))]
    public class PreventSetupShowPatch
    {
        public static void Prefix()
            => scnEditor.instance.tabSection_rows.roomPagesLabel.text = "SETUP";
    }

    [HarmonyPatch(typeof(TabSection), "SetVisible")]
    public class SetVisiblePatch
    {
        public static bool CalledManually = false;

        public static void Postfix(TabSection __instance, bool visible, bool animated)
        {
            TabSection_Rows rows = __instance as TabSection_Rows;
            if (rows && visible)
            {
                if (CalledManually)
                {
                    CalledManually = false;
                    return;
                }

                rows.container[rows.pageIndex].transform.SetSiblingIndex(1);
                LevelEventControl_Base[] restoreAlphaControls = rows.container[rows.pageIndex].GetComponentsInChildren<LevelEventControl_Base>(true);
                foreach (LevelEventControl_Base control in restoreAlphaControls)
                {
                    // ev.UpdateUIInternal();
                    if (rows.editor.selectedControls.Contains(control))
                        control.ShowAsSelected();
                    else
                        control.ShowAsDeselected();
                    control.trigger.enabled = true;
                }
                return;
            }
            else if (!rows)
                return;
            if (rows.roomPagesLabel.text == "SETUP")
                return;

            CalledManually = true;
            rows.Show(animated);
            if (PluginCompatibility.RDEditorPlusDetected)
                RDEditorPlusCompatibility.ForceRowSubRowUpdate();

            rows.container[rows.pageIndex].transform.SetSiblingIndex(1);
            LevelEventControl_Base[] events = rows.container[rows.pageIndex].GetComponentsInChildren<LevelEventControl_Base>(true);
            foreach (LevelEventControl_Base control in events)
            {
                control.UpdateUIInternal();
                if (rows.editor.selectedControls.Contains(control))
                    control.ShowAsSelected();
                else
                {
                    control.ShowAsDeselected();
                    MultiplyGraphicAlpha(control);
                }

                if (GhostRowBeatsSelectable.Value)
                    continue;

                control.trigger.enabled = false;
                control.trigger.OnEndDrag(new(EventSystem.current));
                control.trigger.OnPointerExit(new(EventSystem.current));
            }

            rows.tabPanel.gameObject.SetActive(false);
            RectTransform rectTransform = rows.tabRT;
            Vector2 vector = rectTransform.sizeDelta.WithX(visible ? 20f : 15f);
            rectTransform.DOKill();
            if (animated)
                rectTransform.DOSizeDelta(vector, 0.1f).SetUpdate(UpdateType.Normal, true).SetId("noKillOnReload");
            else
                rectTransform.sizeDelta = vector;
        }
    }

    [HarmonyPatch(typeof(EventSystem), nameof(EventSystem.RaycastAll))]
    public class DisableStackSelectPatch
    {
        public static void Postfix(List<RaycastResult> raycastResults)
        {
            if (GhostRowBeatsSelectable.Value)
                return;
            if (scnEditor.instance == null || scnEditor.instance.currentTab == Tab.Rows)
                return;

            for (int i = 0; i < raycastResults.Count; i++)
            {
                RaycastResult hit = raycastResults[i];
                LevelEventControl_Base control = hit.gameObject.GetComponent<LevelEventControl_Base>();
                if (control == null || control.tab != Tab.Rows)
                    continue;

                raycastResults.RemoveAt(i--);

            }
        }
    }

    public class EnsureUIPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(scnEditor), "ShowUIAsNewFile")]
        public static void LoadFilePostfix(scnEditor __instance)
            => __instance.tabSection_rows.Hide(false);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Timeline), nameof(Timeline.ZoomIn))]
        [HarmonyPatch(typeof(Timeline), nameof(Timeline.ZoomOut))]
        [HarmonyPatch(typeof(Timeline), nameof(Timeline.ZoomVert))]
        public static void ZoomUpdatePostfix(bool ___hasZoomed, bool ___hasZoomedVert)
        {
            if ((!___hasZoomed && !___hasZoomedVert) || scnEditor.instance.currentTab == Tab.Rows)
                return;
            UpdateRowPageUI();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.UpdateTimelineAccordingToLevelEventType))]
        public static void SetCPBDragPostfix(scnEditor __instance, LevelEventType type)
        {
            if (__instance.currentTab == Tab.Rows || type != LevelEventType.SetCrotchetsPerBar)
                return;
            UpdateRowPageUI();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InspectorPanel_SetCrotchetsPerBar), nameof(InspectorPanel_SetCrotchetsPerBar.SaveProperties))]
        public static void SetCPBEventPostfix()
        {
            if (scnEditor.instance.currentTab == Tab.Rows)
                return;
            UpdateRowPageUI();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.OffsetSelectedEventsByBar))]
        [HarmonyPatch(typeof(BulkSelectPanel), nameof(BulkSelectPanel.UpdateTag))]
        [HarmonyPatch(typeof(BulkSelectPanel), nameof(BulkSelectPanel.UpdateTagRunNormally))]
        [HarmonyPatch(typeof(LevelEventControlEventTrigger), nameof(LevelEventControlEventTrigger.OnDrag))]
        public static void OffsetPostfix()
        {
            scnEditor instance = scnEditor.instance;
            if (instance.currentTab == Tab.Rows)
                return;

            foreach (LevelEventControl_Base control in instance.selectedControls)
            {
                if (control.tab != Tab.Rows)
                    continue;
                UpdateControlUI(control);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.DeselectAllEventControls))]
        public static void DeselectAllPrefix(scnEditor __instance)
        {
            if (__instance.currentTab == Tab.Rows)
                return;

            List<LevelEventControl_Base> deselectedControls = [];
            foreach (LevelEventControl_Base control in __instance.selectedControls)
            {
                if (control.isBase || control.tab != Tab.Rows)
                    continue;

                control.SaveData();
                deselectedControls.Add(control);
            }

            foreach (LevelEventControl_Base control in deselectedControls)
            {
                __instance.selectedControls.Remove(control);
                control.ShowAsDeselected();
                MultiplyGraphicAlpha(control);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Property), nameof(Property.Save))]
        public static void SavePostfix(LevelEvent_Base levelEvent)
        {
            if (scnEditor.instance.currentTab == Tab.Rows)
                return;
            LevelEventControl_Base control = scnEditor.instance.eventControls.Find(x => x.levelEvent == levelEvent);
            if (control.tab == Tab.Rows)
                UpdateControlUI(control);
        }

        public static void UpdateRowPageUI()
        {
            scnEditor editor = scnEditor.instance;
            int pageIndex = editor.tabSection_rows.pageIndex;

            int firstOfPage = RowHeader.GetRowDataIndex(0, pageIndex);
            if (firstOfPage == editor.eventControls.Count)
                return;

            int startOfNextPage = RowHeader.GetRowDataIndex(0, pageIndex + 1);

            for (int i = firstOfPage; i < startOfNextPage; i++)
            {
                List<LevelEventControl_Base> controls = editor.eventControls_rows[i];
                foreach (LevelEventControl_Base control in controls)
                    UpdateControlUI(control);
            }
        }

        public static void UpdateControlUI(LevelEventControl_Base control)
        {
            bool selected = scnEditor.instance.selectedControls.Contains(control);
            control.UpdateUIInternal();
            if (selected)
                control.ShowAsSelected();
            else
            {
                control.ShowAsDeselected();
                MultiplyGraphicAlpha(control);
            }
        }
    }

    [HarmonyPatch(typeof(Timeline), "CullMaskedObjects")]
    public class CullingPatch
    {
        public static MethodInfo CullMaskedObjects = AccessTools.Method(typeof(Timeline), "CullMaskedObjects");

        public static void Postfix(Timeline __instance)
        {
            Tab tab = __instance.editor.currentTab;
            if (tab == Tab.Rows)
                return;

            __instance.editor.currentTab = Tab.Rows;
            CullMaskedObjects.Invoke(__instance, null);
            __instance.editor.currentTab = tab;
        }
    }

    // public class DisableInteractionsPatch
    // {
    //     [HarmonyPrefix]
    //     [HarmonyPatch(typeof(LevelEventControlEventTrigger), nameof(LevelEventControlEventTrigger.OnBeginDrag))]
    //     [HarmonyPatch(typeof(LevelEventControlEventTrigger), nameof(LevelEventControlEventTrigger.OnDrag))]
    //     // [HarmonyPatch(typeof(LevelEventControlEventTrigger), nameof(LevelEventControlEventTrigger.OnEndDrag))]
    //     [HarmonyPatch(typeof(LevelEventControlEventTrigger), nameof(LevelEventControlEventTrigger.OnPointerClick))]
    //     [HarmonyPatch(typeof(LevelEventControlEventTrigger), nameof(LevelEventControlEventTrigger.OnPointerDown))]
    //     [HarmonyPatch(typeof(LevelEventControlEventTrigger), nameof(LevelEventControlEventTrigger.OnPointerEnter))]
    //     // [HarmonyPatch(typeof(LevelEventControlEventTrigger), nameof(LevelEventControlEventTrigger.OnPointerExit))]
    //     public static bool Prefix(LevelEventControlEventTrigger __instance)
    //         => __instance.control.tab == scnEditor.instance.currentTab;
    // }

    public static void MultiplyGraphicAlpha(Graphic graphic)
    {
        if (!graphic)
            return;
        graphic.color = graphic.color.WithAlpha(graphic.color.a * GhostRowBeatsOpacityMultiplier.Value);
    }

    public static void MultiplyGraphicAlpha(LevelEventControl_Base control)
    {
        MultiplyGraphicAlpha(control.image);
        MultiplyGraphicAlpha(control.border);
        MultiplyGraphicAlpha(control.conditional);
        MultiplyGraphicAlpha(control.tagIndicator);
        MultiplyGraphicAlpha(control.durationBorder);
        MultiplyGraphicAlpha(control.durationFill);
    }
}
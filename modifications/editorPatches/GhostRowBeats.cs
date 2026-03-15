using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using DG.Tweening;
using HarmonyLib;
using RDLevelEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RDModifications;

[Modification("If the current page of the Rows tab should be shown at all times.", true)]
public class GhostRowBeats : Modification
{
    [Configuration<float>(0.45f, "What the opacity of the ghost rows should be multiplied by.", [float.Epsilon, float.MaxValue])]
    public static ConfigEntry<float> GhostRowBeatsOpacityMultiplier;

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
            scnEditor editor = scnEditor.instance;
            if ((!___hasZoomed && !___hasZoomedVert) || editor.currentTab == Tab.Rows)
                return;
            foreach (LevelEventControl_Base control in editor.eventControls_rows[editor.tabSection_rows.pageIndex])
                control.UpdateUIInternal();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.OffsetSelectedEventsByBar))]
        [HarmonyPatch(typeof(BulkSelectPanel), nameof(BulkSelectPanel.UpdateTag))]
        [HarmonyPatch(typeof(BulkSelectPanel), nameof(BulkSelectPanel.UpdateTagRunNormally))]
        public static void OffsetPostfix()
        {
            scnEditor instance = scnEditor.instance;
            if (instance.currentTab == Tab.Rows)
                return;

            foreach (LevelEventControl_Base control in instance.selectedControls)
            {
                if (control.tab != Tab.Rows)
                    continue;
                control.UpdateUIInternal();
                MultiplyGraphicAlpha(control);
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
                control.UpdateUIInternal();
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
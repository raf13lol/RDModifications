using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using System;
using System.Reflection;
using RDLevelEditor;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;
using System.Reflection.Emit;

namespace RDModifications;

[EditorModification]
public class EditorBorderTintOpacity
{
    public static ManualLogSource logger;

    public static ConfigEntry<bool> enabled;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("EditorPatches", "EditorBorderTintOpacity", false,
        "If there should be a slider for the border/tint opacity in Paint Rows.\n" +
        "Meant to simplify under/overtinting.\n" +
        "(Will only show if DisableSliderLimits is enabled as otherwise it would be useless.)");

        return DisableSliderLimits.enabled.Value && enabled.Value;
    }

    private class VariablesNeeded
    {
        // heh heh, important shit for my horribleness later on
        [JsonProperty("___borderOpacity", "", null, "", false, true, nameof(EnableBorderColorIf))]
        [SliderAlpha(false, null)]
        [IntInfo(0, 100)]
        public static int borderOpacity
        {
            get => (int)(trueBorderOpacity * 100d);
            set => trueBorderOpacity = value / 100d;
        }

        [JsonProperty("___tintOpacity", "", null, "", false, true, nameof(EnableTintColorIf))]
        [SliderAlpha(false, null)]
        [IntInfo(0, 100)]
        public static int tintOpacity
        {
            get => (int)(trueTintOpacity * 100);
            set => trueTintOpacity = value / 100d;
        }

        public static double trueBorderOpacity = 1d;
        public static double trueTintOpacity = 1d;

        public static object eventToUse;

        private static BorderType GetBorder()
        {
            if (eventToUse is LevelEvent_Tint tint)
                return tint.border;
            return ((LevelEvent_TintRows)eventToUse).border;
        }

        private static bool GetTint()
        {
            if (eventToUse is LevelEvent_Tint tint)
                return tint.tint;
            return ((LevelEvent_TintRows)eventToUse).tint;
        }

        public static bool stuffBeingEncoded = false;

        // Token: 0x0600231D RID: 8989 RVA: 0x000E9182 File Offset: 0x000E7382
        public static bool EnableBorderColorIf()
        {
            if (eventToUse == null || stuffBeingEncoded)
                return false;
            return GetBorder() > BorderType.None;
        }

        // Token: 0x0600231E RID: 8990 RVA: 0x000E918D File Offset: 0x000E738D
        public static bool EnableTintColorIf()
        {
            if (eventToUse == null || stuffBeingEncoded)
                return false;
            return GetTint();
        }
    }

    [HarmonyPatch(typeof(InspectorPanel), nameof(InspectorPanel.GetLocalizedString))]
    private class AddStringsLocPatch
    {
        public static void Postfix(string ___panelName, ref string __result, string key)
        {
            if (RDString.samuraiMode)
                return;
            string fullKey = "editor." + key.Replace("___", "");
            if (!___panelName.StartsWith("Tint"))
                return;

            if (fullKey == "editor.borderOpacity")
                __result = "Border Opacity";
            if (fullKey == "editor.tintOpacity")
                __result = "Tint Opacity";
        }
    }

    [HarmonyPatch(typeof(InspectorPanel), nameof(InspectorPanel.Show))]
    private class SetVariablesNeededPatch
    {
        public static void Prefix(LevelEventControl_Base levelEventControl)
        {
            if (levelEventControl.levelEvent is LevelEvent_Tint __tintEvent
            || levelEventControl.levelEvent is LevelEvent_TintRows __tintRowsEvent)
            {
                var levelEvent = levelEventControl.levelEvent;
                VariablesNeeded.eventToUse = levelEvent;

                ColorOrPalette borderCol = Color.white;
                ColorOrPalette tintCol = Color.white;
                if (levelEvent is LevelEvent_TintRows tintRowsEvent)
                {
                    borderCol = tintRowsEvent.borderColor;
                    tintCol = tintRowsEvent.tintColor;
                }
                else if (levelEvent is LevelEvent_Tint tintEvent)
                {
                    borderCol = tintEvent.borderColor;
                    tintCol = tintEvent.tintColor;
                }

                VariablesNeeded.trueBorderOpacity = Math.Round((double)(borderCol.alpha ?? 1f), 2);
                VariablesNeeded.trueTintOpacity = Math.Round((double)(tintCol.alpha ?? 1f), 2);

                PropertyControl_Color colorControlBorder = (PropertyControl_Color)levelEvent.inspectorPanel.properties
                    .Find((p) => p.name.StartsWith("borderColor")).control;
                PropertyControl_Color colorControlTint = (PropertyControl_Color)levelEvent.inspectorPanel.properties
                    .Find((p) => p.name.StartsWith("tintColor")).control;
                colorControlBorder.colorPicker.storesAlpha = false;
                colorControlTint.colorPicker.storesAlpha = false;
            }
            else
                VariablesNeeded.eventToUse = null;
        }
    }

    [HarmonyPatch(typeof(LevelEvent_Base), nameof(LevelEvent_Base.Encode))]
    private class PreventDuplicatedKeysPatch
    {
        public static void Prefix(LevelEvent_Base __instance)
            => VariablesNeeded.stuffBeingEncoded = __instance is LevelEvent_Tint || __instance is LevelEvent_TintRows;
            
        public static void Postfix()
            => VariablesNeeded.stuffBeingEncoded = false;
    }

    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(PropertyControl_SliderAlpha), nameof(PropertyControl_SliderAlpha.Save))]
    private class SetTintEventDataPatch
    {
        public static void Postfix(PropertyControl_SliderAlpha __instance, LevelEvent_Base levelEvent)
        {
            if (levelEvent != VariablesNeeded.eventToUse)
                return;
            BasePropertyInfo propertyInfo = (BasePropertyInfo)typeof(PropertyControl)
                .GetMethod("get_propertyInfo", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(__instance, []);

            if (levelEvent is LevelEvent_TintRows __tintRowsEvent
            || levelEvent is LevelEvent_Tint __tintEvent)
            {   
                void setColorAlpha(LevelEvent_Base levelEvent, string name, ColorOrPalette baseColor, double newAlpha)
                {
                    // at the very end, do everything 
                    string propName = name.Replace("Opacity", "Color");
                    PropertyControl_Color colorControl = (PropertyControl_Color)levelEvent.inspectorPanel.properties
                        .Find((p) => p.name.StartsWith(propName)).control;

                    // colorControl.colorPicker.color = baseColor.Encode(false);
                    if (newAlpha == 1d) // this should keep palette colours entact as best as possible so we'll do it
                        baseColor = baseColor.WithAlpha((float)newAlpha);
                    else
                        baseColor = baseColor.ToColor().WithAlpha((float)newAlpha);

                    Type type = typeof(LevelEvent_TintRows);
                    if (levelEvent is LevelEvent_Tint)
                        type = typeof(LevelEvent_Tint);
                    type.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public)
                        .SetValue(levelEvent, baseColor);
                }

                if (propertyInfo.name == "opacity")
                {
                    ColorOrPalette borderCol = Color.white;
                    ColorOrPalette tintCol = Color.white;
                    if (levelEvent is LevelEvent_TintRows tintRowsEvent)
                    {
                        borderCol = tintRowsEvent.borderColor;
                        tintCol = tintRowsEvent.tintColor;
                    }
                    else if (levelEvent is LevelEvent_Tint tintEvent)
                    {
                        borderCol = tintEvent.borderColor;
                        tintCol = tintEvent.tintColor;
                    }

                    setColorAlpha(levelEvent, "borderOpacity", borderCol, VariablesNeeded.trueBorderOpacity);
                    setColorAlpha(levelEvent, "tintOpacity", tintCol, VariablesNeeded.trueTintOpacity);
                }
            }
        }
    }

    [HarmonyPatch(typeof(LevelEventInfo), MethodType.Constructor, [typeof(Type)])]
    private class LevelEventInfoConstructorPatch
    {
        public static void Postfix(LevelEventInfo __instance, Type eventType)
        {
            if (!eventType.Name.Contains("LevelEvent_Tint"))
                return;

            BindingFlags flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public;
            // this is from the game because idk this query syntax shit atm
            List<BasePropertyInfo> propsList = [.. (from fieldInfo in eventType.GetProperties(flags)
                where fieldInfo.IsDefined(typeof(JsonPropertyAttribute))
                orderby fieldInfo.MetadataToken
                select BasePropertyInfo.FromProperty(fieldInfo))];

            for (int i = 0; i < propsList.Count; i++)
            {
                if (propsList[i].name == "borderColor")
                {
                    propsList.Insert(i, BasePropertyInfo.FromProperty(typeof(VariablesNeeded).GetProperty("borderOpacity")));
                    break;
                }
            }
            for (int i = 0; i < propsList.Count; i++)
            {
                if (propsList[i].name == "tintColor")
                {
                    propsList.Insert(i, BasePropertyInfo.FromProperty(typeof(VariablesNeeded).GetProperty("tintOpacity")));
                    break;
                }
            }

            __instance.GetType()
                .GetField(nameof(__instance.propertiesInfo), BindingFlags.Instance | BindingFlags.Public)
                .SetValue(__instance, propsList.ToImmutableList());
        }
    }

    
}
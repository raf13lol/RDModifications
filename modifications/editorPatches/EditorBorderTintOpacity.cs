using HarmonyLib;
using System;
using System.Reflection;
using RDLevelEditor;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine.UI;

namespace RDModifications;

[Modification(
	"If there should be the built-in input field for the border/tint opacity in Paint Rows/Sprite, no matter what.\n" +
	"Meant to simplify under/overtinting."
, true)]
public class EditorBorderTintOpacity : Modification
{
    private class TintRowsPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LevelEvent_TintRows), nameof(LevelEvent_TintRows.EnableBorderOpacityIf))]
        public static bool BorderPrefix(LevelEvent_TintRows __instance, ref bool __result)
        {
            __result = __instance.EnableBorderColorIf();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LevelEvent_TintRows), nameof(LevelEvent_TintRows.EnableTintOpacityIf))]
        public static bool TintPrefix(LevelEvent_TintRows __instance, ref bool __result)
        {
            __result = __instance.EnableTintColorIf();
            return false;
        }
    }

    private class VariablesNeeded
    {
        // heh heh, important shit for my horribleness later on
        [JsonProperty("___borderOpacity", "", null, "", false, true, nameof(EnableBorderColorIf))]
        [InputField("%", InputField.LineType.SingleLine, 40, 14, false, false, null)]
        public static int borderOpacity
        {
            get => (int)(trueBorderOpacity * 100d);
            set => trueBorderOpacity = value / 100d;
        }

        [JsonProperty("___tintOpacity", "", null, "", false, true, nameof(EnableTintColorIf))]
        [InputField("%", InputField.LineType.SingleLine, 40, 14, false, false, null)]
        public static int tintOpacity
        {
            get => (int)(trueTintOpacity * 100);
            set => trueTintOpacity = value / 100d;
        }

        public static double trueBorderOpacity = 1d;
        public static double trueTintOpacity = 1d;

        public static LevelEvent_Tint eventToUse;

        private static BorderType GetBorder()
        {
            return eventToUse.border;
        }

        private static bool GetTint()
        {
            return eventToUse.tint;
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
        public static void Postfix(ref string __result, string key, string ___panelName)
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
            if (levelEventControl.levelEvent is LevelEvent_Tint tintEvent)
            {
                VariablesNeeded.eventToUse = tintEvent;

                ColorOrPalette borderCol = tintEvent.borderColor;
                ColorOrPalette tintCol = tintEvent.tintColor;

                VariablesNeeded.trueBorderOpacity = Math.Round((double)(borderCol.alpha ?? 1f), 2);
                VariablesNeeded.trueTintOpacity = Math.Round((double)(tintCol.alpha ?? 1f), 2);

                PropertyControl_Color colorControlBorder = (PropertyControl_Color)tintEvent.inspectorPanel.properties
                    .Find((p) => p.name.StartsWith("borderColor")).control;
                PropertyControl_Color colorControlTint = (PropertyControl_Color)tintEvent.inspectorPanel.properties
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
            => VariablesNeeded.stuffBeingEncoded = __instance is LevelEvent_Tint;
            
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
            BasePropertyInfo propertyInfo = (BasePropertyInfo)AccessTools.PropertyGetter(typeof(PropertyControl), "propertyInfo").Invoke(__instance, []);
    
            if (levelEvent is LevelEvent_Tint tintEvent)
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
                    AccessTools.Property(type, propName).SetValue(levelEvent, baseColor);
                }

                if (propertyInfo.name == "opacity")
                {
                    ColorOrPalette borderCol = tintEvent.borderColor;
                    ColorOrPalette tintCol = tintEvent.tintColor;

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
            if (eventType.Name != "LevelEvent_Tint")
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
                    propsList.Insert(i+1, BasePropertyInfo.FromProperty(AccessTools.Property(typeof(VariablesNeeded), "borderOpacity")));
                    break;
                }
            }
            for (int i = 0; i < propsList.Count; i++)
            {
                if (propsList[i].name == "tintColor")
                {
                    propsList.Insert(i+1, BasePropertyInfo.FromProperty(AccessTools.Property(typeof(VariablesNeeded), "tintOpacity")));
                    break;
                }
            }

            AccessTools.Field(__instance.GetType(), nameof(__instance.propertiesInfo)).SetValue(__instance, propsList.ToImmutableList());
        }
    }

    
}
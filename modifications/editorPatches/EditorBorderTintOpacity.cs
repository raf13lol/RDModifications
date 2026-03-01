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
    public class TintRowsPatch
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

    public class VariablesNeeded
    {
        // heh heh, important shit for my horribleness later on
        [JsonProperty("___borderOpacity", "", null, "", false, true, nameof(EnableBorderColorIf))]
        [InputField("%", InputField.LineType.SingleLine, 40, 14, false, false)]
        public static int BorderOpacity
        {
            get => (int)(TrueBorderOpacity * 100d);
            set => TrueBorderOpacity = value / 100d;
        }

        [JsonProperty("___tintOpacity", "", null, "", false, true, nameof(EnableTintColorIf))]
        [InputField("%", InputField.LineType.SingleLine, 40, 14, false, false)]
        public static int TintOpacity
        {
            get => (int)(TrueTintOpacity * 100);
            set => TrueTintOpacity = value / 100d;
        }

        public static double TrueBorderOpacity = 1d;
        public static double TrueTintOpacity = 1d;

        public static LevelEvent_Tint EventToUse;

        public static BorderType GetBorder()
        {
            return EventToUse.border.HasValue ? (BorderType)EventToUse.border : BorderType.None;
        }

        public static bool GetTint()
        {
            return EventToUse.tint.HasValue && (bool)EventToUse.tint;
        }

        public static bool DataBeingEncoded = false;

        // Token: 0x0600231D RID: 8989 RVA: 0x000E9182 File Offset: 0x000E7382
        public static bool EnableBorderColorIf()
        {
            if (EventToUse == null || DataBeingEncoded)
                return false;
            return GetBorder() > BorderType.None;
        }

        // Token: 0x0600231E RID: 8990 RVA: 0x000E918D File Offset: 0x000E738D
        public static bool EnableTintColorIf()
        {
            if (EventToUse == null || DataBeingEncoded)
                return false;
            return GetTint();
        }
    }

    [HarmonyPatch(typeof(InspectorPanel), nameof(InspectorPanel.GetLocalizedString))]
    public class AddStringsLocPatch
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
    public class SetVariablesNeededPatch
    {
        public static void Prefix(LevelEventControl_Base levelEventControl)
        {
            if (levelEventControl.levelEvent is LevelEvent_Tint tintEvent)
            {
                VariablesNeeded.EventToUse = tintEvent;

                ColorOrPalette borderCol = tintEvent.borderColor;
                ColorOrPalette tintCol = tintEvent.tintColor;

                VariablesNeeded.TrueBorderOpacity = Math.Round((double)(borderCol.alpha ?? 1f), 2);
                VariablesNeeded.TrueTintOpacity = Math.Round((double)(tintCol.alpha ?? 1f), 2);

                PropertyControl_Color colorControlBorder = (PropertyControl_Color)tintEvent.inspectorPanel.properties
                    .Find((p) => p.name.StartsWith("borderColor")).control;
                PropertyControl_Color colorControlTint = (PropertyControl_Color)tintEvent.inspectorPanel.properties
                    .Find((p) => p.name.StartsWith("tintColor")).control;
                colorControlBorder.colorPicker.storesAlpha = false;
                colorControlTint.colorPicker.storesAlpha = false;
            }
            else
                VariablesNeeded.EventToUse = null;
        }
    }

    [HarmonyPatch(typeof(LevelEvent_Base), nameof(LevelEvent_Base.Encode))]
    public class PreventDuplicatedKeysPatch
    {
        public static void Prefix(LevelEvent_Base __instance)
            => VariablesNeeded.DataBeingEncoded = __instance is LevelEvent_Tint;

        public static void Postfix()
            => VariablesNeeded.DataBeingEncoded = false;
    }

    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(PropertyControl_SliderAlpha), nameof(PropertyControl_SliderAlpha.Save))]
    public class SetTintEventDataPatch
    {
        public static void Postfix(PropertyControl_SliderAlpha __instance, LevelEvent_Base levelEvent)
        {
            if (levelEvent != VariablesNeeded.EventToUse)
                return;
            BasePropertyInfo propertyInfo = (BasePropertyInfo)AccessTools.PropertyGetter(typeof(PropertyControl), "propertyInfo").Invoke(__instance, []);

            if (levelEvent is LevelEvent_Tint tintEvent)
            {
                void setColorAlpha(LevelEvent_Base levelEvent, string propName, ColorOrPalette baseColor, double newAlpha)
                {
                    // at the very end, do everything 

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

                    setColorAlpha(levelEvent, nameof(LevelEvent_Tint.borderColor), borderCol, VariablesNeeded.TrueBorderOpacity);
                    setColorAlpha(levelEvent, nameof(LevelEvent_Tint.tintColor), tintCol, VariablesNeeded.TrueTintOpacity);
                }
            }
        }
    }

    [HarmonyPatch(typeof(LevelEventInfo), MethodType.Constructor, [typeof(Type)])]
    public class LevelEventInfoConstructorPatch
    {
        public static void Postfix(LevelEventInfo __instance, Type eventType)
        {
            if (eventType.Name != nameof(LevelEvent_Tint))
                return;

            // this is from the game because idk this query syntax shit atm
            List<BasePropertyInfo> propsList = [.. (from fieldInfo in AccessTools.GetDeclaredProperties(eventType)
                where fieldInfo.IsDefined(typeof(JsonPropertyAttribute))
                orderby fieldInfo.MetadataToken
                select BasePropertyInfo.FromProperty(fieldInfo))];

            for (int i = 0; i < propsList.Count; i++)
            {
                if (propsList[i].name == nameof(LevelEvent_Tint.borderColor))
                {
                    propsList.Insert(i + 1, BasePropertyInfo.FromProperty(AccessTools.Property(typeof(VariablesNeeded), nameof(VariablesNeeded.BorderOpacity))));
                    break;
                }
            }
            for (int i = 0; i < propsList.Count; i++)
            {
                if (propsList[i].name == nameof(LevelEvent_Tint.tintColor))
                {
                    propsList.Insert(i + 1, BasePropertyInfo.FromProperty(AccessTools.Property(typeof(VariablesNeeded), nameof(VariablesNeeded.TintOpacity))));
                    break;
                }
            }

            AccessTools.Field(__instance.GetType(), nameof(__instance.propertiesInfo)).SetValue(__instance, propsList.ToImmutableList());
        }
    }


}
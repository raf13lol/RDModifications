using HarmonyLib;
using System;
using System.Reflection;
using RDLevelEditor;
using UnityEngine.UI;

namespace RDModifications;

[Modification(
	"If input fields (next to sliders or not) in the editor shouldn't be limited.\n" + 
	"Do note that it may break some events horribly by not abiding to their limits." 
, true)]
public class DisableInputFieldLimits : Modification
{
    [HarmonyPatch(typeof(PropertyControl_InputField), nameof(PropertyControl_InputField.Save))]
    private class RegularInputFieldsPatch
    {
        public static bool Prefix(PropertyControl_InputField __instance, LevelEvent_Base levelEvent, ref bool ___alreadyUpdating)
        {
            ___alreadyUpdating = true;
            Property parentProperty = (Property)AccessTools.Property(typeof(PropertyControl), "parentProperty").GetValue(__instance);
            BasePropertyInfo nullableUnderlying = parentProperty.propertyInfo.NullableUnderlying;
            object obj;

            if (nullableUnderlying is StringPropertyInfo)
                obj = __instance.inputField.text;
            else if (nullableUnderlying is FloatExpressionPropertyInfo)
                obj = FloatExpression.FromString(__instance.inputField.text);
            else if (nullableUnderlying is FloatPropertyInfo)
                obj = float.Parse(__instance.inputField.text);
            else if (nullableUnderlying is IntPropertyInfo)
                obj = int.Parse(__instance.inputField.text);
            else
            {
                ___alreadyUpdating = false;
                return false;
            }

            __instance.inputField.text = obj?.ToString();
            parentProperty.propertyInfo.propertyInfo.SetValue(levelEvent, obj);
            ___alreadyUpdating = false;
            return false;
        }
    }

    // we need to use this because of the delegation
    private class SliderOnEndEditPatch
    {
        public static MethodInfo TargetMethod()
        	=> AccessUtils.GetInnerMethodContainsWithArgs(typeof(PropertyControl_Slider), "<>c__", "<AddListeners>", [typeof(string)]);

        [HarmonyPostfix]
        public static void ALPost(object __instance, string value)
        {
            PropertyControl_Slider inst = (PropertyControl_Slider)AccessUtils.GetFirstFieldContains(__instance.GetType(), "_this").GetValue(__instance);
            if (!inst.inputField.gameObject.activeInHierarchy)
                return;

            inst.inputField.text = value;
            inst.Save(inst.editor.selectedControl.levelEvent);
        }
    }

    private class SliderPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PropertyControl_Slider), nameof(PropertyControl_Slider.UpdateUI))]
        public static void UpdateUIPostfix(PropertyControl_Slider __instance, LevelEvent_Base levelEvent)
        {
            if (!__instance.inputField.gameObject.activeInHierarchy || __instance.inputField == null)
                return;
            __instance.inputField.text = AccessTools.Method(typeof(PropertyControl), "GetEventValue").Invoke(__instance, [levelEvent]).ToString();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PropertyControl_Slider), nameof(PropertyControl_Slider.Save))]
        public static void SavePropertyPostfix(PropertyControl_Slider __instance, LevelEvent_Base levelEvent)
        {
            if (!__instance.inputField.gameObject.activeInHierarchy || __instance.inputField == null)
                return;

            object num;
            if (!__instance.slider.wholeNumbers)
                num = float.Parse(__instance.inputField.text);
            else
                num = Convert.ToInt32(__instance.inputField.text);

            AccessTools.Method(typeof(PropertyControl), "SetEventValue").Invoke(__instance, [levelEvent, num]);
        }
    }

    private class OtherSlidersPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PropertyControl_SliderAlpha), nameof(PropertyControl_SliderAlpha.UpdateSlider))]
		[HarmonyPatch(typeof(PropertyControl_SliderPercent), nameof(PropertyControl_SliderPercent.UpdateSlider))]
        public static void UpdateSliderPrefix(out string __state, InputField ___inputField)
            => __state = ___inputField.text;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PropertyControl_SliderAlpha), nameof(PropertyControl_SliderAlpha.UpdateSlider))]
        [HarmonyPatch(typeof(PropertyControl_SliderPercent), nameof(PropertyControl_SliderPercent.UpdateSlider))]
        public static void UpdateSliderPostfix(PropertyControl __instance, string __state, InputField ___inputField)
        {
            ___inputField.text = __state;
            __instance.Save(__instance.editor.selectedControl.levelEvent);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(PropertyControl_SliderAlpha), nameof(PropertyControl_SliderAlpha.UpdateUI))]
        [HarmonyPatch(typeof(PropertyControl_SliderPercent), nameof(PropertyControl_SliderPercent.UpdateUI))]
        public static void UpdateUIPostfix(PropertyControl __instance, LevelEvent_Base levelEvent, InputField ___inputField)
        {
            float value = (float)AccessTools.Method(typeof(PropertyControl), "GetEventValue").Invoke(__instance, [levelEvent]);
			if (__instance is PropertyControl_SliderPercent)
				value *= 100;
			___inputField.text = value.ToString();
        } 

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PropertyControl_SliderAlpha), nameof(PropertyControl_SliderAlpha.Save))]
        public static void SavePropertyAlphaPostfix(PropertyControl_SliderAlpha __instance, LevelEvent_Base levelEvent, InputField ___inputField)
            => AccessTools.Method(typeof(PropertyControl), "SetEventValue").Invoke(__instance, [levelEvent, int.Parse(___inputField.text)]);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PropertyControl_SliderPercent), nameof(PropertyControl_SliderPercent.Save))]
        public static void SavePropertyPercentPostfix(PropertyControl_SliderPercent __instance, LevelEvent_Base levelEvent)
        {
            float num = float.Parse(__instance.inputField.text);
            AccessTools.Method(typeof(PropertyControl), "SetEventValue").Invoke(__instance, [levelEvent, num / 100]);
        }
    }

	private class MakeRowPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InspectorPanel_MakeRow), "SaveInternal")]
        public static void SavePrefix(InspectorPanel_MakeRow __instance, ref string __state)
            => __state = __instance.length.text;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InspectorPanel_MakeRow), "SaveInternal")]
        public static void SavePostfix(InspectorPanel_MakeRow __instance, ref string __state, LevelEvent_Base levelEvent)
        {
            LevelEvent_MakeRow data = (LevelEvent_MakeRow)levelEvent;
            data.length = int.TryParse(__state, out int result) ? result : 7;
            __instance.length.text = __state;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InspectorPanel_MakeRow), nameof(InspectorPanel_MakeRow.Awake))]
        public static void InspectorPanelPostfix(InspectorPanel_MakeRow __instance)
            => __instance.length.characterLimit = 0;
    }
}
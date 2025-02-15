using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using System;
using System.Reflection;
using RDLevelEditor;

namespace RDModifications;

[EditorModification]
public class DisableSliderLimits
{
    public static ManualLogSource logger;

    public static ConfigEntry<bool> enabled;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("EditorPatches", "DisableSliderLimits", false, 
        "If input boxes next to sliders in the editor shouldn't be limited.");

        return enabled.Value;
    }

    // we need to use this because of the delegation
    private class SliderOnEndEditPatch
    {
        public static MethodInfo TargetMethod()
        {
            return AccessUtils.GetInnerMethodContainsWithArgs(typeof(PropertyControl_Slider), "<>c__", "<AddListeners>", [typeof(string)]);
        }

        [HarmonyPostfix]
        public static void ALPost(object __instance, string value)
        {
            PropertyControl_Slider inst = (PropertyControl_Slider)AccessUtils.GetFieldContains(__instance.GetType(), "_this").GetValue(__instance);
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
            
            object value = typeof(PropertyControl)
                .GetMethod("GetEventValue", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(__instance, [levelEvent]);

            __instance.inputField.text = value.ToString();
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

            typeof(PropertyControl)
                .GetMethod("SetEventValue", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(__instance, [levelEvent, num]);
        }
    }

    // i hate how i have to copy this horrible stuff
    private class SliderAlphaPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PropertyControl_SliderAlpha), nameof(PropertyControl_SliderAlpha.UpdateSlider))]
        public static void UpdateSliderPrefix(PropertyControl_SliderAlpha __instance, ref string __state)
            => __state = __instance.inputField.text;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PropertyControl_SliderAlpha), nameof(PropertyControl_SliderAlpha.UpdateSlider))]
        public static void UpdateSliderPostfix(PropertyControl_SliderAlpha __instance, ref string __state)
        {
            __instance.inputField.text = __state;
            __instance.Save(__instance.editor.selectedControl.levelEvent);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(PropertyControl_SliderAlpha), nameof(PropertyControl_SliderAlpha.UpdateUI))]
        public static void UpdateUIPostfix(PropertyControl_SliderAlpha __instance, LevelEvent_Base levelEvent)
        {
            int value = (int)typeof(PropertyControl)
                .GetMethod("GetEventValue", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(__instance, [levelEvent]);
            __instance.inputField.text = value.ToString();
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(PropertyControl_SliderAlpha), nameof(PropertyControl_SliderAlpha.Save))]
        public static void SavePropertyPostfix(PropertyControl_SliderAlpha __instance, LevelEvent_Base levelEvent)
        {
            int num = int.Parse(__instance.inputField.text);

            typeof(PropertyControl)
                .GetMethod("SetEventValue", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(__instance, [levelEvent, num]);
        }
    }

    private class SliderPercentPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PropertyControl_SliderPercent), nameof(PropertyControl_SliderPercent.UpdateSlider))]
        public static void UpdateSliderPrefix(PropertyControl_SliderPercent __instance, ref string __state)
            => __state = __instance.inputField.text;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PropertyControl_SliderPercent), nameof(PropertyControl_SliderPercent.UpdateSlider))]
        public static void UpdateSliderPostfix(PropertyControl_SliderPercent __instance, ref string __state)
        {
            __instance.inputField.text = __state;
            __instance.Save(__instance.editor.selectedControl.levelEvent);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(PropertyControl_SliderPercent), nameof(PropertyControl_SliderPercent.UpdateUI))]
        public static void UpdateUIPostfix(PropertyControl_SliderPercent __instance, LevelEvent_Base levelEvent)
        {
            float value = (float)typeof(PropertyControl)
                .GetMethod("GetEventValue", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(__instance, [levelEvent]);

            __instance.inputField.text = (value * 100).ToString();
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(PropertyControl_SliderPercent), nameof(PropertyControl_SliderPercent.Save))]
        public static void SavePropertyPostfix(PropertyControl_SliderPercent __instance, LevelEvent_Base levelEvent)
        {
            float num = float.Parse(__instance.inputField.text);

            typeof(PropertyControl)
                .GetMethod("SetEventValue", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(__instance, [levelEvent, num / 100]);
        }
    }

}
using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using System;
using System.Reflection;
using RDLevelEditor;

namespace RDModifications
{
    public class EditorPatches
    {
        public static ConfigEntry<bool> enabled;

        public static ManualLogSource logger;

        public static void Init(Harmony patcher, ConfigFile config, ManualLogSource logging, ref bool anyEnabled)
        {
            logger = logging;

            enabled = config.Bind("EditorPatches", "Enabled", false,
            "If any of these patches should be enabled.");

            DisableSliderLimits.Init(patcher, config, logging, ref anyEnabled);
            EditorBorderTintOpacity.Init(patcher, config, logging, ref anyEnabled);
        }
    }
}
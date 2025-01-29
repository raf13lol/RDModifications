using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using System;
using System.Reflection;
using RDLevelEditor;

namespace RDModifications
{
    public class TemplateEditor
    {
        public static ConfigEntry<bool> enabled;

        public static ManualLogSource logger;

        public static void Init(Harmony patcher, ConfigFile config, ManualLogSource logging, ref bool anyEnabled)
        {
            logger = logging;

            enabled = config.Bind("EditorPatches", "Template", false,
            "Template");

            if (!EditorPatches.enabled.Value)
                return;
            if (enabled.Value)
            {
                anyEnabled = true;
            }
        }
    }
}
using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using System;
using System.Reflection;
using RDLevelEditor;

namespace RDModifications
{
    public class Template
    {
        public static ConfigEntry<bool> enabled;

        public static ManualLogSource logger;

        public static void Init(Harmony patcher, ConfigFile config, ManualLogSource logging, ref bool anyEnabled)
        {
            logger = logging;

            enabled = config.Bind("Template", "Template", false,
            "Template");

            if (enabled.Value)
            {
                anyEnabled = true;
            }
        }
    }
}
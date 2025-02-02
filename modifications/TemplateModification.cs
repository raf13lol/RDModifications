using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using System;
using System.Reflection;
using RDLevelEditor;

namespace RDModifications
{
    [Modification]
    public class TemplateModification
    {
        public static ManualLogSource logger;

        public static ConfigEntry<bool> enabled;

        public static bool Init(ConfigFile config, ManualLogSource logging)
        {
            logger = logging;
            enabled = config.Bind("Template", "Template", false,
            "Template");

            return enabled.Value;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace RDModifications;

public class Patcher
{
    public static void PatchAllWithAttribute<T>(Harmony patcher, ConfigFile config, out bool anyEnabled, bool forceEditorOff) where T : ModificationAttribute
    {
        Type[] potentialPatches = typeof(Entry).Assembly.GetTypes();

        Dictionary<Type, MethodInfo> GetConfigCache = [];
        Dictionary<Type, FieldInfo> SetAsConfigValueCache = [];

        anyEnabled = false;
        foreach (Type type in potentialPatches)
        {
            if (type.Name.StartsWith("TemplateModification") || type.Name == "Modification" || type.Name.EndsWith("Attribute"))
                continue;

            ModificationAttribute modAttribute = type.GetCustomAttribute<T>();
            if (modAttribute == null || (modAttribute.PlatformSpecific != null && modAttribute.PlatformSpecific != Application.platform))
                continue;

            // config stuff
            string sectionName = modAttribute.IsEditor ? "EditorPatches" : type.Name;
            ConfigEntry<bool> enabledConfigEntry = config.Bind(sectionName, modAttribute.IsEditor ? type.Name : "Enabled", false, modAttribute.EnabledDescription);
            Modification.Enabled[type] = enabledConfigEntry;

            List<FieldInfo> fields = AccessTools.GetDeclaredFields(type);
            foreach (FieldInfo field in fields)
            {
                Attribute[] attributes = [.. field.GetCustomAttributes()];
                Attribute configAttribute = null;

                foreach (Attribute attribute in attributes)
                {
                    if (!attribute.GetType().Name.StartsWith(nameof(ConfigurationAttribute<>)))
                        continue;

                    configAttribute = attribute;
                    break;
                }

                if (configAttribute == null)
                    continue;

                Type specificConfigAttributeType = configAttribute.GetType();
                Type configType = specificConfigAttributeType.GetGenericArguments()[0];

                if (!GetConfigCache.TryGetValue(configType, out MethodInfo GetConfig))
                {
                    GetConfig = AccessTools.Method(specificConfigAttributeType, nameof(ConfigurationAttribute<>.GetConfig));
                    GetConfigCache[configType] = GetConfig;
                    SetAsConfigValueCache[configType] = AccessTools.Field(specificConfigAttributeType, nameof(ConfigurationAttribute<>.SetAsConfigValue));
                }

                object configEntryOrValue = GetConfig.Invoke(configAttribute, [config, sectionName, field.Name]);
                if ((bool)SetAsConfigValueCache[configType].GetValue(configAttribute))
                {
                    MethodInfo valueGetter = AccessTools.PropertyGetter(configEntryOrValue.GetType(), nameof(ConfigEntry<>.Value));
                    configEntryOrValue = valueGetter.Invoke(configEntryOrValue, []);
                }

                field.SetValue(null, configEntryOrValue);
            }

            // should we actually patch + init
            bool shouldPatch = enabledConfigEntry.Value;
            bool initShouldPatch = true;
            // access tool throws logs... 
            MethodInfo method = type.GetMethod("Init", AccessTools.all);
            if (method != null)
            {
                object[] funcParams = [];
                if (method.GetParameters().Length == 1)
                    funcParams = [enabledConfigEntry.Value];

                if (method.ReturnType == typeof(bool))
                    initShouldPatch = (bool)method.Invoke(null, funcParams);
                else
                    method.Invoke(null, funcParams);
            }

            if (!shouldPatch || (modAttribute.IsEditor && forceEditorOff))
                continue;

            // patch
            Type[] innerTypes = [.. (from t in type.GetNestedTypes(AccessTools.all)
                                        where t.Name.EndsWith("Patch") || t.GetCustomAttribute(typeof(PatchAttribute)) != null
                                        select t)];

            foreach (Type innerPatch in innerTypes)
            {
                PatchAttribute patch = (PatchAttribute)innerPatch.GetCustomAttribute(typeof(PatchAttribute));
                if (!initShouldPatch && (patch == null || !patch.IgnoreInitReturn))
                    continue;

                anyEnabled = true;
                patcher.PatchAll(innerPatch);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RDModifications;

public class Patcher
{
	public static void PatchAllWithAttribute<T>(out bool anyEnabled, bool forceEditorOff) where T : ModificationAttribute
	{
		Harmony patcher = Entry.HarmonyPatcher;
		ConfigFile config = Entry.Configuration;
		ManualLogSource logger = Modification.Log;

		Type[] potentialTypes = typeof(Entry).Assembly.GetTypes();
		Dictionary<Type, MethodInfo> getConfigs = [];

		anyEnabled = false;
		foreach (Type type in potentialTypes)
		{
			if (type.Name.StartsWith("TemplateModification") || type.Name == "Modification" || type.Name.EndsWith("Attribute"))
				continue;

			ModificationAttribute modAttrib = type.GetCustomAttribute<T>();
			if (modAttrib == null || (modAttrib.PlatformSpecific != null && modAttrib.PlatformSpecific != Application.platform))
				continue;
				
			// config stuff
			string sectionName = modAttrib.IsEditor ? "EditorPatches" : type.Name;
			List<FieldInfo> fields = AccessTools.GetDeclaredFields(type);

			ConfigEntry<bool> enabled =  config.Bind(sectionName, modAttrib.IsEditor ? type.Name : "Enabled", false, modAttrib.EnabledDescription);
			Modification.Enabled[type] = enabled;
			foreach (FieldInfo field in fields)
            {
				Attribute[] attribs = [.. field.GetCustomAttributes()];
                Attribute configAttrib = attribs.Length > 0 ? attribs[0] : null;
				if (configAttrib == null || !configAttrib.GetType().IsGenericType) // may not work forever.... beware!
					continue;

				Type configType = configAttrib.GetType().GetGenericArguments()[0];
				if (!getConfigs.TryGetValue(configType, out MethodInfo getConfig))
				{
					getConfig = AccessTools.Method(configAttrib.GetType(), "GetConfig");
					getConfigs[configType] = getConfig;	
				}
				field.SetValue(null, getConfig.Invoke(configAttrib, [config, sectionName, field.Name]));
            }

			// should we actually patch + init
			bool shouldPatch = enabled.Value;
			bool initShouldPatch = true;
			// access tool throws logs... 
			MethodInfo method = type.GetMethod("Init", BindingFlags.Public | BindingFlags.Static);
			if (method != null)
			{
				object[] funcParams = [];
				if (method.GetParameters().Length > 0)
					funcParams = [enabled.Value];

				if (method.ReturnType == typeof(bool))
					initShouldPatch = (bool)method.Invoke(null, funcParams);
				else
					method.Invoke(null, funcParams);
			}

			if (!shouldPatch || (modAttrib.IsEditor && forceEditorOff))
				continue;

			// patch
			Type[] innerTypes = [..(from t in type.GetNestedTypes(BindingFlags.NonPublic)
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
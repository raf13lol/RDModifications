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
		Harmony patcher = RDModificationsEntry.HarmonyPatcher;
		ConfigFile config = RDModificationsEntry.Configuration;
		ManualLogSource logger = Modification.Log;

		Type[] potentialTypes = typeof(RDModificationsEntry).Assembly.GetTypes();

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
                Attribute configAttrib = field.GetCustomAttributes().ToList().Find(attrib => attrib.GetType().Name.StartsWith(nameof(ConfigurationAttribute<>)));
				if (configAttrib == null)
					continue;
				
				field.SetValue(null, AccessTools.Method(configAttrib.GetType(), "GetConfig").Invoke(configAttrib, [config, sectionName, field.Name]));
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
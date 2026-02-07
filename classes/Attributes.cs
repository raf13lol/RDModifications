using System;
using BepInEx.Configuration;
using UnityEngine;

namespace RDModifications;

// ! NOTE: -2 will never be used lmao ! so we use it as our "null" value (attributes don't like nullable Params ig)
public class ModificationAttribute(string enabledDescription, bool isEditor = false, int platformSpecific = -2) : Attribute
{
	public string EnabledDescription = enabledDescription;
	public RuntimePlatform? PlatformSpecific = platformSpecific == -2 ? null : (RuntimePlatform)platformSpecific;
	public bool IsEditor = isEditor;
}

public class ConfigurationAttribute<T>(T defaultValue, string description, T[] acceptableRange = null) : Attribute where T : IComparable
{
	public T DefaultValue = defaultValue;
	public string Description = description;
	public AcceptableValueRange<T> AcceptableRange = acceptableRange == null ? null : new(acceptableRange[0], acceptableRange[1]);

	public ConfigEntry<T> GetConfig(ConfigFile config, string sectionName, string fieldName)
		=> config.Bind(sectionName, fieldName, DefaultValue, new ConfigDescription(Description, AcceptableRange));
}

public class PatchAttribute(bool ignoreInitReturn = false) : Attribute
{
	public bool IgnoreInitReturn = ignoreInitReturn;
}
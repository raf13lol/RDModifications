using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;

#if !BPE5
using BepInEx.Unity.Mono.Bootstrap;
using HarmonyLib;
#else
using BepInEx.Bootstrap;
#endif

namespace RDModifications;

public class OtherPluginUtils
{
    public static Dictionary<string, PluginInfo> PluginInfos
    {
        get
        {
#if !BPE5
            return UnityChainloader.Instance.Plugins;
#else
            return Chainloader.PluginInfos;
#endif
        }
    }

    public static bool DetectPlugin(string pluginGUID)
        => PluginInfos.ContainsKey(pluginGUID);

#if !BPE5
    public static bool DetectPlugin(string pluginGUID, SemanticVersioning.Version version)
        => PluginInfos.ContainsKey(pluginGUID) && PluginInfos[pluginGUID].Metadata.Version <= version;
#else
    public static bool DetectPlugin(string pluginGUID, Version version)
        => PluginInfos.ContainsKey(pluginGUID) && PluginInfos[pluginGUID].Metadata.Version <= version;
#endif

    public static Assembly GetOtherPluginAssembly(string pluginGUID)
    {
        if (!DetectPlugin(pluginGUID))
            return null;
        return Assembly.LoadFrom(PluginInfos[pluginGUID].Location);
    }

    public static Type GetOtherPluginType(Assembly assembly, string typeNamespace, string name)
        => assembly.GetType($"{typeNamespace}.{name}");

    public static Type GetOtherPluginType(Assembly assembly, string name)
        => assembly.GetTypes().First((t) => t.Name == name);

    public static Type GetOtherPluginType(string pluginGUID, string typeNamespace, string name)
        => GetOtherPluginAssembly(pluginGUID).GetType($"{typeNamespace}.{name}");

    public static Type GetOtherPluginType(string pluginGUID, string name)
        => GetOtherPluginAssembly(pluginGUID).GetTypes().First((t) => t.Name == name);
}
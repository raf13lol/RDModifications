using System;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace RDModifications
{
    public class PatchUtils
    {
        public static void PatchAllWithAttribute<T>(Harmony patcher, ConfigFile config, ManualLogSource logging, ref bool anyEnabled)
        {
            Type[] potentialTypes = typeof(RDModificationsEntry).Assembly.GetTypes();

            foreach (Type type in potentialTypes)
            {
                if (type.Name.StartsWith("TemplateModification") || type.Name.EndsWith("Attribute"))
                    continue;

                BaseModificationAttribute attrib = (BaseModificationAttribute)type.GetCustomAttribute(typeof(T));
                if (attrib != null)
                {
                    BindingFlags publicStatic =  BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                    object[] funcParams = [config, logging];
                    if (!attrib.autoPatch)
                        funcParams = [patcher, ..funcParams];

                    bool shouldPatch = (bool)type.GetMethod("Init", publicStatic).Invoke(null, funcParams);
                    if (!anyEnabled)
                        anyEnabled = anyEnabled || shouldPatch;
                    
                    if (attrib.autoPatch && shouldPatch)
                    {
                        Type[] innerTypes = [..(from t in type.GetNestedTypes(BindingFlags.NonPublic)
                                            where t.Name.EndsWith("Patch") || t.GetCustomAttribute(typeof(PatchAttribute)) != null
                                            select t)];
                        foreach (Type innerPatch in innerTypes)
                            patcher.PatchAll(innerPatch);
                    }
                }
            }
        }
    }
}
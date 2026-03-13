using HarmonyLib;
using RDLevelEditor;

namespace RDModifications;

[Modification("If events that have a narrate option should have said option off by default when created.", true)]
public class DefaultNarrateOffOnCreate : Modification
{
    [HarmonyPatch(typeof(LevelEvent_FloatingText), nameof(LevelEvent_FloatingText.Init))]
    public class FloatingTextPatch
    {
        public static void Postfix(LevelEvent_FloatingText __instance)
            => __instance.narrate = false;
    }

    [HarmonyPatch(typeof(LevelEvent_Base), nameof(LevelEvent_Base.Init))]
    public class ShowStatusSignPatch
    {
        public static void Postfix(LevelEvent_Base __instance)
        {
            if (__instance is LevelEvent_ShowStatusSign showStatusSign)
                showStatusSign.narrate = false;
        }
    }
}
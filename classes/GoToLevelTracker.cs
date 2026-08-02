using HarmonyLib;

namespace RDModifications;

public class GoToLevelTracker
{
    public static bool OnFirstLevel = true;

    public static void Patch(Harmony patcher)
    {
        patcher.PatchAll(typeof(SetFalsePatch));
        patcher.PatchAll(typeof(SetTruePatch));
    }

    [HarmonyPatch(typeof(LevelBase), "GoToLevelWithWarning")]
    public class SetFalsePatch
    {
        // base game uses this as well so we have to do this
        public static void Postfix()
            => OnFirstLevel = scnGame.levelToLoadSource != LevelSource.ExternalPath;
    }

    [HarmonyPatch(typeof(scnGame), nameof(scnGame.ClearLevelPersistence))]
    public class SetTruePatch
    {
        public static void Postfix()
            => OnFirstLevel = true;
    }
}
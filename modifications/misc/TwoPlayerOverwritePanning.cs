using HarmonyLib;

namespace RDModifications;

[Modification(
    "If 2-player panning separation should overwrite all pulse sounds instead of those with no panning set.\n" +
    "Do note that this does not affect sounds that are changed via SetGameSound."
)]
public class TwoPlayerOverwritePanning : Modification
{
    [HarmonyPatch(typeof(RDUtils), nameof(RDUtils.OverridePanFor2P), [typeof(RDPlayer), typeof(float)])]
    public class OverwritePatch
    {
        public static void Prefix(ref float pan)
            => pan = 0f;
    }
}
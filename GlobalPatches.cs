using HarmonyLib;

namespace RDModifications;

public class GlobalPatches
{
    public static void PatchAll(Harmony patcher)
    {
        patcher.PatchAll(typeof(Fix2PSwappedInputPatch));
    }

    // Fuck this fucking fucking bitch cunt bullshit. 
    // How ??? How the fuck does this shit happen. I am fucking pissed.
    // Apparently this only happens if this patch is enabled.
    [HarmonyPatch(typeof(scnMenu), "Start")]
    public class Fix2PSwappedInputPatch
    {
        public static bool Bootup = true;

        public static void Postfix()
        {
            if (!Bootup)
                return;
            if (RDInput.p1Default.schemeIndex != RDInput.p1.schemeIndex)
            {
                RDInput.p1Default.SwapSchemeIndex();
                RDInput.p2Default.SwapSchemeIndex();
            }
            Bootup = false;
        }
    }
}


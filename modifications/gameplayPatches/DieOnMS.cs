using System;
using BepInEx.Configuration;
using HarmonyLib;

namespace RDModifications;

[Modification("If upon getting a certain millisecond timing on a hit, the player should instantly die.")]
public class DieOnMS : Modification
{
	[Configuration<int>(73, "The certain millisecond timing that would kill the player.")]
    public static ConfigEntry<int> Millisecond;

	[Configuration<bool>(false, "If it should uses the absolute value of the timing.")]
    public static ConfigEntry<bool> AbsoluteTiming;

    [HarmonyPatch(typeof(scrPlayerbox), nameof(scrPlayerbox.Pulse))]
    private class HitMSPatch
    {
        public static void Postfix(scrPlayerbox __instance, float timeOffset, bool CPUTriggered)
        {
            if (CPUTriggered)
                return;
            int ms = (int)(timeOffset * 1000f);
            if (AbsoluteTiming.Value)
                ms = Math.Abs(ms) * Math.Sign(Millisecond.Value);
            if (ms == Millisecond.Value)
                __instance.game.FailLevel(__instance.ent);
        }
    }

    [HarmonyPatch(typeof(scrPlayerbox), nameof(scrPlayerbox.SpaceBarReleased))]
    private class ReleaseHoldMSPatch
    {
        public static void Postfix(scrPlayerbox __instance, RDPlayer player, bool cpuTriggered)
        {
            if (player != __instance.ent.row.GetCurrentPlayer() || cpuTriggered || !__instance.currentHoldBeat)
                return;
            int ms = (int)((__instance.conductor.audioPos - __instance.currentHoldBeat.releaseTime) * 1000.0);
            if (AbsoluteTiming.Value)
                ms = Math.Abs(ms) * Math.Sign(Millisecond.Value);
            if (ms == Millisecond.Value)
                __instance.game.FailLevel(__instance.ent);
        }
    }
}
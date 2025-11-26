using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RDModifications;

[Modification]
public class DieOnMS
{
    public static ManualLogSource logger;

    public static ConfigEntry<bool> enabled;
    public static ConfigEntry<int> millisecond;
    public static ConfigEntry<bool> absolute;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("DieOnMS", "Enabled", false,
        "If upon getting a certain millisecond timing on a hit, whether the player should instantly die.");

        millisecond = config.Bind("DieOnMS", "Millisecond", 79, "The certain millisecond timing that would kill the player.");
        
        absolute = config.Bind("DieOnMS", "AbsoluteTiming", false, "Whether to use the absolute value of the timing.");

        return enabled.Value;
    }

    [HarmonyPatch(typeof(scrPlayerbox), nameof(scrPlayerbox.Pulse))]
    private class HitMSPatch
    {
        public static void Postfix(scrPlayerbox __instance, float timeOffset, bool CPUTriggered)
        {
            if (CPUTriggered)
                return;
            int ms = (int)(timeOffset * 1000f);
            if (absolute.Value)
                ms = Math.Abs(ms) * Math.Sign(millisecond.Value);
            if (ms == millisecond.Value)
                __instance.game.FailLevel(__instance.ent);
        }
    }

    [HarmonyPatch(typeof(scrPlayerbox), nameof(scrPlayerbox.SpaceBarReleased))]
    private class ReleaseHoldMSPatch
    {
        public static void Postfix(scrPlayerbox __instance, RDPlayer player, bool cpuTriggered)
        {
            if (player != __instance.ent.row.playerProp.GetCurrentPlayer() || cpuTriggered || !__instance.currentHoldBeat)
                return;
            int ms = (int)((__instance.conductor.audioPos - __instance.currentHoldBeat.releaseTime) * 1000.0);
            if (absolute.Value)
                ms = Math.Abs(ms) * Math.Sign(millisecond.Value);
            if (ms == millisecond.Value)
                __instance.game.FailLevel(__instance.ent);
        }
    }
}
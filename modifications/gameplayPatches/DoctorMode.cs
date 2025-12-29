using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace RDModifications;

[Modification(
	"If this is enabled, the mod will completely destroy the rhythm engine.\n" +
	"It destroys the rhythm engine by multipling the 'crotchet' by a random value between 2 bounds.\n" +
	"With these settings, you can configure how much to destroy it by modifying these 2 bounds."
)]
public class DoctorMode : Modification
{
	[Configuration<float>(0.75f, "The lowest multiplier to use in the random multiplier.")]
    public static ConfigEntry<float> LowMultiplier;
	[Configuration<float>(1.25f, "The highest multiplier to use in the random multiplier.")]
    public static ConfigEntry<float> HighMultiplier;

	[Configuration<bool>(false, "If the songs should be played automatically. Only applies to Doctor mode. (NO RANKS NOR ANY ACHIEVEMENTS WILL BE SAVED)")]
    public static ConfigEntry<bool> Auto;

    public static bool Init(bool enabled)
    {
        if (enabled && LowMultiplier.Value == 1.00f && HighMultiplier.Value == 1.00f)
        {
            Log.LogWarning("DoctorMode: LowMultiplier and HighMultiplier are both 1.00. No Doctor mode patches will be applied.");
            return false;
        }

        if (LowMultiplier.Value > HighMultiplier.Value)
        {
            LowMultiplier.Value = HighMultiplier.Value;
            Log.LogWarning("DoctorMode: LowMultiplier was greater than HighMultiplier. LowMultiplier has been set to HighMultiplier.");
        }
        return true;
    }

    private class ConductorPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(scrConductor), nameof(scrConductor.crotchet), MethodType.Getter)]
        public static void GetCrotchetPostfix(ref float __result)
            => __result *= UnityEngine.Random.Range(LowMultiplier.Value, HighMultiplier.Value);

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(scrConductor), nameof(scrConductor.BeatToTime))]
        public static IEnumerable<CodeInstruction> BeatToTimeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] randMult = [
                new(OpCodes.Ldc_R4, LowMultiplier.Value),
                new(OpCodes.Ldc_R4, HighMultiplier.Value),
                new(OpCodes.Call, AccessTools.Method("UnityEngine.Random:Range", [typeof(float), typeof(float)])),
                new(OpCodes.Mul)
            ];

            // twice?! ... okay then :pensive:
            return new CodeMatcher(instructions)
                // need to Ignore the first two `mul`'s so
                // sets it to the max via the Idiotproofing (oob)
                .Advance(500000)
                // goes backwards to the last one before ret
                .MatchBack(false, new CodeMatch(OpCodes.Mul))
                .InsertAndAdvance(randMult)
                // back to where we just were
                .MatchBack(false, new CodeMatch(OpCodes.Mul))
                // go past that (idk if we can match on the same thing sooo)
                .Advance(-5)
                // To the next one after
                .MatchBack(false, new CodeMatch(OpCodes.Mul))
                .InsertAndAdvance(randMult)
                // done
                .InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(scnMenu), "Start")]
    private class TitlescreenPatch
    {
        public static void Postfix(scnMenu __instance)
        {
            __instance.logo.rhythm.image.color = new(0, 0, 0, 0);
            __instance.logo.rhythmChinese.image.color = new(0, 0, 0, 0);
        }
    }

    private class AutoPatch
    {
        // This is how Otto in the editor works so Do not strike me down mods
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DebugSettings), nameof(DebugSettings.Auto), MethodType.Getter)]
        public static void AutoHitPostfix(ref bool __result)
            // this code is only ran if auto.Value 
            => __result = Auto.Value;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Rankscreen), nameof(Rankscreen.ShowAndSaveRank))]
        public static void RankscreenPostfix(Rankscreen __instance)
        {
            if (!Auto.Value)
                return;

            // should be safe to do so
            if (__instance.rank.text.Length > 0)
                __instance.rank.text += "?";
            if (__instance.customText.text.Length > 0)
                __instance.customText.text += "?";
        }
    }

    private class RanksAchievementsPatch
    {
        [HarmonyPrefix]
		[HarmonyPatch(typeof(SteamIntegration), nameof(SteamIntegration.UnlockAchievement), [typeof(string), typeof(bool)])]
		[HarmonyPatch(typeof(Persistence), nameof(Persistence.SetLevelRank), [typeof(string), typeof(Rank), typeof(bool), typeof(bool)])]
		[HarmonyPatch(typeof(Persistence), nameof(Persistence.SetLevelScore))]
        public static bool NormalPrefix()
            => !Auto.Value;

        [HarmonyPrefix]
		[HarmonyPatch(typeof(Persistence), nameof(Persistence.SetCustomLevelRank), [typeof(string), typeof(Rank), typeof(float)])]
        public static bool CustomRankPrefix(ref Rank rank)
            => !Auto.Value || rank == Rank.NotFinished;
    }
}
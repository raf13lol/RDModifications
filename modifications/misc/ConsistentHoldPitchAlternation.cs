using System;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RDModifications;

[Modification("If holds should have their pitch alternation consistent to how regular classics work.")]
public class ConsistentHoldPitchAlternation : Modification
{
    public class ConsistentPatch
    {
        public static BeatClassic CurrentInstance = null;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BeatClassic), nameof(BeatClassic.GetAlternatingBeatPitchMultiplier))]
        public static void FinalHoldPrefix(ref int i)
        {
            if (CurrentInstance == null || !CurrentInstance.hasHeldPulses)
                return;
            i = CurrentInstance.beats[i].beatboxNumber - 1;
        }

        [HarmonyILManipulator]
        [HarmonyPatch(typeof(BeatClassic), nameof(BeatClassic.Add7BeatsFreetimeFlexible))]
        public static void MostHoldsILManipulator(ILContext il)
        {
            ILCursor cursor = new(il);

            cursor.GotoNext(MoveType.After, x => x.MatchStfld(AccessTools.Field(typeof(BeatClassic), nameof(BeatClassic.shouldHeldbeatPulseBeAlt))));
            cursor.Index++;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, 6);
            cursor.EmitDelegate(delegate (BeatClassic beat, int k)
            {
                int kShouldBe = scrConductor.BeatsoundPitchMode ? 0 : 1;
                beat.shouldHeldbeatPulseBeAlt = k % 2 == kShouldBe;
            });
        }
    }

    [HarmonyPatch(typeof(BeatClassic), nameof(BeatClassic.Add7BeatsFreetimeFlexible))]
    public class InstancePatch
    {
        public static void Prefix(BeatClassic __instance)
            => ConsistentPatch.CurrentInstance = __instance;

        public static void Postfix()
            => ConsistentPatch.CurrentInstance = null;
    }
}
using System;
using System.Reflection;
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
        public static FieldInfo shouldHeldbeatPulseBeAlt = AccessTools.Field(typeof(BeatClassic), nameof(BeatClassic.shouldHeldbeatPulseBeAlt));

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

            cursor.GotoNext(MoveType.After, x => x.MatchStfld(shouldHeldbeatPulseBeAlt));
            cursor.Index++;
     
            cursor.Emit(OpCodes.Ldarg_0);

            // (k % 2)
            cursor.Emit(OpCodes.Ldloc, 6);
            cursor.Emit(OpCodes.Ldc_I4_2);
            cursor.Emit(OpCodes.Rem);

            // flipped scrConductor.BeatsoundPitchMode - (scrConductor.BeatsoundPitchMode != 0)
            cursor.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(scrConductor), nameof(scrConductor.BeatsoundPitchMode)));
            cursor.Emit(OpCodes.Ldc_I4_0);
            cursor.Emit(OpCodes.Ceq);

            // (k % 2) == (!scrConductor.BeatsoundPitchMode)
            cursor.Emit(OpCodes.Ceq);
            // beat.shouldHeldbeatPulseBeAlt = (k % 2) != scrConductor.BeatsoundPitchMode
            cursor.Emit(OpCodes.Stfld, shouldHeldbeatPulseBeAlt);
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
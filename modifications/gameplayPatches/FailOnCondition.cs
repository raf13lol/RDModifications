using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;

namespace RDModifications;

[Modification("If upon meeting a certain condition, the player should instantly fail the level.")]
public class FailOnCondition : Modification
{
	[Configuration<FailOn>(FailOn.Heartbreak,
	"What the fail condition should be.\n" + 
	"If it is a rank, the condition will be met once you meet the condition for the rank at any point during the level."
	)]	
	public static ConfigEntry<FailOn> FailCondition;

	[Configuration<float>(10f, 
	"How many 'mistakes' (misses with mistake-weighting) can be made.\n" + 
	"This does include mistakes added via Call Custom Method."
	, [float.Epsilon, float.PositiveInfinity])]
	public static ConfigEntry<float> AmountOfMistakesToFailOn; 

	[Configuration<int>(10, "How many misses can be made.", [1, int.MaxValue])]
	public static ConfigEntry<int> AmountOfMissesToFailOn; 

	[Configuration<bool>(false, "If upon meeting the fail condition, the status sign should appear with the specified text below, instead of the player failing the level.")]
	public static ConfigEntry<bool> AlertInsteadOfFail;

	[Configuration<string>("Fail condition met!", "The text to appear upon meeting the fail condition and with AlertInsteadOfFail enabled.")]
	public static ConfigEntry<string> AlertText;

	private static bool FailedYet = false;

	private static void FailLevel(RowEntity entity = null)
    {
		if (FailedYet || scnGame.instance.currentLevel.failedLevel)
			return;
		FailedYet = true;
        if (AlertInsteadOfFail.Value || entity == null)
            scnGame.instance.statusText.SetStatusText(AlertText.Value);
		else
            scnGame.instance.FailLevel(entity);
    }

	[HarmonyPatch(typeof(scnGame), nameof(scnGame.StartTheGame))]
	private class ResetFailedYetPatch
    {
		public static void Postfix()
			=> FailedYet = false;
    }

	[HarmonyPatch(typeof(scnGame), nameof(scnGame.OnMistakeOrHeal))]
	private class RankMistakesFailConditionsPatch
    {
		public static void Postfix(scnGame __instance, Row prop)
		{
			bool shouldFail = FailCondition.Value == FailOn.AmountOfMistakes && __instance.mistakesManager.mistakes >= AmountOfMistakesToFailOn.Value;
			if (!shouldFail && FailCondition.Value <= FailOn.A)
			{
				Rank rank = __instance.currentLevel.GetRankFromMistakes().ToNormal();
				shouldFail = rank <= (int)FailCondition.Value;
			}
			if (shouldFail)
				FailLevel(prop.ent);
		}
	}

	[HarmonyPatch(typeof(scnGame), nameof(scnGame.AddHitOffset))]
	private class MissesFailConditionPatch
    {
		public static void Postfix(scnGame __instance, int rowID)
		{
			if (FailCondition.Value != FailOn.AmountOfMisses)
				return;
			int misses = __instance.allHitOffsets.Count(hit => hit.offsetType != OffsetType.Perfect);
			if (misses >= AmountOfMissesToFailOn.Value)
				FailLevel(__instance.rows[rowID].ent);
		}
	}

	// don't be a heartbreaker
	[HarmonyPatch(typeof(scrHeart), nameof(scrHeart.DoFinalCrack))]
	private class HeartbreakerFailConditionPatch
    {
		public static void Postfix(scrHeart __instance)
		{
			if (FailCondition.Value != FailOn.Heartbreak)
				return;
			FailLevel(__instance.ent);
		}
	}

	public enum FailOn
    {
		F = Rank.F,
		D = Rank.D,
		C = Rank.C,
		B = Rank.B,
		A = Rank.A,
		Heartbreak,
		AmountOfMistakes,
		AmountOfMisses
    }
}
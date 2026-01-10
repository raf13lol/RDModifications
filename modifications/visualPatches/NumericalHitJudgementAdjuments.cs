using System.Text.RegularExpressions;
using BepInEx.Configuration;
using HarmonyLib;

namespace RDModifications;

[Modification("If the displayed sign should be adjusted when a beat is hit and Numerical Hit Judgements are enabled.")]
public class NumericalHitJudgementAdjuments : Modification
{
	[Configuration<ColourStyleType>(ColourStyleType.Regular, 
	"How the sign text should be coloured (zero-offset means if the hit is within ±25ms).\n" +
	"Regular - Base game.\n" +
	"GreenZeroOffset - Makes the sign text green if the hit has zero-offset.\n" +
	"GreenZeroOffsetElseRed - Makes the sign text green if the hit has zero-offset, otherwise makes the sign text red.\n" +
	"Legacy - Makes the sign text green if the hit has zero-offset, yellow if it was earlier than zero-offset, and orange if it was later than zero-offset.\n" +
	"None - Disables any colouring, including the default darkened colour on releasing a hold."
	)]
    public static ConfigEntry<ColourStyleType> ColourStyle;

	[Configuration<JudgementDisplayType>(JudgementDisplayType.Millisecond, 
	"How the judgement should be displayed.\n" +
	"Millisecond - Base game.\n" +
	"FrameOffset - Displays the frame offset.\n" +
	"Legacy - Displays the raw time offset in seconds."
	)]
    public static ConfigEntry<JudgementDisplayType> JudgementDisplay;

	private class JudgementInfoPatch
    {
		public static double TimeOffset = 0;
		public static int FrameOffset = 0;		

		[HarmonyPostfix]
		[HarmonyPatch(typeof(scrPlayerbox), nameof(scrPlayerbox.Pulse))]
		public static void PressPostfix(float timeOffset, bool CPUTriggered)
		{
			if (!CPUTriggered)
				TimeOffset = timeOffset;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(scrPlayerbox), nameof(scrPlayerbox.SpaceBarReleased))]
		public static void ReleasePrefix(scrPlayerbox __instance, out double __state)
			=> __state = __instance.currentHoldBeat  ? __instance.currentHoldBeat.releaseTime : double.NaN;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(scrPlayerbox), nameof(scrPlayerbox.SpaceBarReleased))]
		public static void ReleasePostfix(scrPlayerbox __instance, double __state, bool cpuTriggered)
		{
			if (!double.IsNaN(__state) && !__instance.currentHoldBeat && !cpuTriggered)
				TimeOffset = __instance.conductor.audioPos - __state;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MistakesManager), nameof(MistakesManager.AddAbsoluteMistake))]
		public static void FrameOffsetPostfix(int frameOffset)
			=> FrameOffset = frameOffset;
    }

    [HarmonyPatch(typeof(LEDSign), "SetText")]
    private class FullSignPatch
    {
        public static void Prefix(ref string value)
        {
			static string addColorToSign(string value, string colour, string darkenedColour)
            {
				if (ColourStyle.Value == ColourStyleType.Regular)
					return value;

				darkenedColour = darkenedColour.IsNullOrEmpty() ? colour.Replace("F", "A") : darkenedColour;

                string newValue = value;
				newValue = newValue.Replace("</color>", $"</color><color={colour}>");
                newValue = newValue.Replace("<color=#AAA>", $"</color><color={darkenedColour}>");
                newValue = $"<color={colour}>{newValue}</color>";
            
				return newValue;
			}

            string val = value.Replace("<color=#AAA>", "").Replace("</color>", "");
            if (!val.StartsWith("[ ") || !val.EndsWith(RDString.Get("editor.unit.ms") + " ]"))
                return;

            Match match = new Regex(@"(-)?[0-9]{1,}").Match(val);
            if (!match.Success || match.Captures.Count > 1)
                return;

			bool parsed = int.TryParse(match.Value, out int ms);
			if (parsed && JudgementDisplay.Value != JudgementDisplayType.Millisecond)
            {
				string judgementReplacement;
				if (JudgementDisplay.Value != JudgementDisplayType.FrameOffset)
				{
					judgementReplacement = ((float)JudgementInfoPatch.TimeOffset).ToString(); // too much precision otherwise
					goto SetValue;
				}

				judgementReplacement = JudgementInfoPatch.FrameOffset.ToString();
				if (JudgementInfoPatch.FrameOffset > 0)
					judgementReplacement = $"+{judgementReplacement}";

			SetValue:
				value = value.Replace("+", "").Replace($"{ms} {RDString.Get("editor.unit.ms")}", judgementReplacement).Replace("[ ", "").Replace(" ]", "");
            }

			string colour = "#0D3";
            string darkenedColour = "#00901F";
			if (JudgementInfoPatch.FrameOffset == 0)
				goto End;

			if (ColourStyle.Value == ColourStyleType.GreenZeroOffsetElseRed)
			{
				colour = "#D03";
				darkenedColour = "#90001f";
				goto End;
			}

			if (ColourStyle.Value != ColourStyleType.Legacy)
				goto End;

			if (JudgementInfoPatch.FrameOffset < 0)
            {
                colour = "#FFC002"; 
				darkenedColour = "#AA8001";
            }
			else if (JudgementInfoPatch.FrameOffset > 0)
            {
                colour = "#E8651D";
				darkenedColour = "#9B4313";
            } 
			
		End:
			if (ColourStyle.Value == ColourStyleType.None)
            {
                colour = "#FFF";
                darkenedColour = "#FFF";
            }
            value = addColorToSign(value, colour, darkenedColour);
        }
    }
	
	public enum ColourStyleType
    {
        Regular,
		GreenZeroOffset,
		GreenZeroOffsetElseRed,
		Legacy,
		None
    }

	public enum JudgementDisplayType
    {
        Millisecond,
		FrameOffset,
		Legacy
    }
}
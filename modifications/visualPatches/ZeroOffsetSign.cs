using System.Text.RegularExpressions;
using BepInEx.Configuration;
using HarmonyLib;

namespace RDModifications;

[Modification("If the sign text should be green when a hit is within ±25ms and Numerical Hit Judgements are enabled.")]
public class ZeroOffsetSign : Modification
{
	[Configuration<bool>(true, "If the sign text should be red when a hit is not within ±25ms.")]
    public static ConfigEntry<bool> RedMissOffset;

    [HarmonyPatch(typeof(LEDSign), "SetText")]
    private class GreenRedSignPatch
    {
        public static void Prefix(ref string value)
        {
			static void addColorToSign(ref string value, string color = "#0F0", string darkenedColor = "#0A0")
            {
                value = value.Replace("</color>", $"</color><color={color}>");
                value = value.Replace("<color=#AAA>", $"</color><color={darkenedColor}>");
                value = $"<color={color}>" + value + "</color>";
            }

            string val = value.Replace("<color=#AAA>", "").Replace("</color>", "");
            if (!val.StartsWith("[ ") || !val.EndsWith(RDString.Get("editor.unit.ms") + " ]"))
                return;
				
            Match match = new Regex(@"(-)?[0-9]{1,}").Match(val);
            if (!match.Success || match.Captures.Count > 1)
                return;

            bool isNumber = int.TryParse(match.Value, out int number);
            if (!isNumber || number >= 25 || number <= -25)
            {
                if (RedMissOffset.Value)
                    addColorToSign(ref value, "#F00", "#A00");
                return;
            }
            addColorToSign(ref value);
        }
    }
}
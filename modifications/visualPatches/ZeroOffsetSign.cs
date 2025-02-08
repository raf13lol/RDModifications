using System.Text.RegularExpressions;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace RDModifications
{
    [Modification]
    public class ZeroOffsetSign
    {
        public static ManualLogSource logger;

        public static ConfigEntry<bool> enabled;
        public static ConfigEntry<bool> redOffset;

        public static bool Init(ConfigFile config, ManualLogSource logging)
        {
            logger = logging;
            enabled = config.Bind("ZeroOffsetSign", "Enabled", false,
            "If Numerical Hit Judgements are enabled, and if a hit is within the range that it would count as \"zero-offset\", the sign text will be green.");

            redOffset = config.Bind("ZeroOffsetSign", "RedOffset", false,
            "If enabled and the hit is out of the range that it would count as \"zero-offset\", the sign text will be red.");

            return enabled.Value;
        }

        [HarmonyPatch(typeof(HUD), nameof(HUD.status), MethodType.Setter)]
        private class GreenRedSignPatch
        {
            public static void Prefix(ref string value)
            {
                void addColorToSign(ref string value, string color = "#0F0", string darkenedColor = "#0A0")
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
                    if (redOffset.Value)
                        addColorToSign(ref value, "#F00", "#A00");
                    return;
                }
                addColorToSign(ref value);
            }
        }
    }
}
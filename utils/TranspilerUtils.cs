using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace RDModifications
{
    public class TranspilerUtils
    {
        public static IEnumerable<CodeInstruction> ReplaceString(IEnumerable<CodeInstruction> instructions, string oldValue, string newValue, int times = 1)
        {
            var matcher = new CodeMatcher(instructions);
            for (int i = 0; i < times; i++)
                matcher = matcher.MatchForward(false, new CodeMatch(OpCodes.Ldstr, oldValue))
                                 .SetOperandAndAdvance(newValue);

            return matcher.InstructionEnumeration();
        }
    }
}
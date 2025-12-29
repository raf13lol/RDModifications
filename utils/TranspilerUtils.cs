using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace RDModifications;

public class TranspilerUtils
{
    public static IEnumerable<CodeInstruction> ReplaceString(IEnumerable<CodeInstruction> instructions, string oldValue, string newValue)
    {
		foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldstr && (string)instruction.operand == oldValue)
				instruction.operand = newValue;
			yield return instruction;
        }
    }
}
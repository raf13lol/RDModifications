using MonoMod.Cil;

namespace RDModifications;

public class ILManipulatorUtils
{

    public static void ReplaceString(ILContext il, string oldValue, string newValue)
    {
		ILCursor cursor = new(il);
		if (!cursor.TryFindNext(out ILCursor[] cursors, x => x.MatchLdstr(oldValue)))
			return;
		foreach (ILCursor c in cursors)
			c.Next.Operand = newValue;
    }
}
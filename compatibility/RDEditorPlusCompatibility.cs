using RDEditorPlus;

namespace RDModifications;

public class RDEditorPlusCompatibility
{
    public static void Run()
    {
        Plugin.RDModificationsRowPatchEnabled = Modification.Enabled[typeof(RemoveFourRowLimit)].Value; 
    }
}
using BepInEx.Logging;
using RDEditorPlus;
using RDEditorPlus.Functionality.SubRow;
#if !BPE5
#endif
namespace RDModifications;

public class RDEditorPlusCompatibility
{
    public static ManualLogSource Log;

    public static void Run()
    {
        #if BPE5
        Log = Plugin.Logger;
        #endif
        Plugin.RDModificationsRowPatchEnabled = Modification.Enabled[typeof(RemoveFourRowLimit)].Value;
    }

    public static void ForceRowSubRowUpdate()
    {
        RowManager.Instance.UpdateTab(true);
        GeneralManager.Instance.ResetAlternatingTimelineStrips();
    }
}
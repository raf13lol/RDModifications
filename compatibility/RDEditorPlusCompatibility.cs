using System;
using System.Reflection;
using HarmonyLib;
using RDEditorPlus;
using RDEditorPlus.Functionality.SubRow;
using RDLevelEditor;

namespace RDModifications;

public class RDEditorPlusCompatibility
{
    public static void Run()
    {
        Plugin.RDModificationsRowPatchEnabled = Modification.Enabled[typeof(RemoveFourRowLimit)].Value;
    }

    public static void ForceRowSubRowUpdate()
    {
        RowManager.Instance.UpdateTab(true);
        GeneralManager.Instance.ResetAlternatingTimelineStrips();
    }
}
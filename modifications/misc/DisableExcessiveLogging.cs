using DG.Tweening;
using HarmonyLib;
using UnityEngine;

namespace RDModifications;

[Modification("If the game should be prevented from logging useless information. Cleans up logs.")]
public class DisableExcessiveLogging : Modification
{
	public static bool DebugLogEnabled = true;

    public static void Init(bool enabled)
    {
        if (!enabled)
			return;
		
		DOTween.onWillLog = (_, _) => false;
		DOTween.logBehaviour = LogBehaviour.ErrorsOnly;
		DOTween.safeModeLogBehaviour = DG.Tweening.Core.Enums.SafeModeLogBehaviour.None;
		Log.LogMessage("DisableExcessiveLogging is enabled.");
    }

	[HarmonyPatch(typeof(Debug), nameof(Debug.Log), [typeof(object)])]
	private class DebugLogPatch
    {
        public static bool Prefix()
			=> DebugLogEnabled;
    }

	[HarmonyPatch(typeof(Window), nameof(Window.CheckIfCurrentViewSizeIsCorrect))]
	[HarmonyPatch(typeof(WindowChoreographer), nameof(WindowChoreographer.Check))]
	private class ToggleLogPatch
    {
        public static void Prefix()
			=> DebugLogEnabled = false;
		public static void Postfix()
			=> DebugLogEnabled = true;
    }
}


using DG.Tweening;

namespace RDModifications;

[Modification("If the tweening engine of the game should be prevented from logging useless information. Cleans up logs.")]
public class DisableDOTweenLogging : Modification
{
    public static void Init(bool enabled)
    {
        if (!enabled)
			return;
		
		DOTween.onWillLog = (_, _) => false;
		DOTween.logBehaviour = LogBehaviour.ErrorsOnly;
		DOTween.safeModeLogBehaviour = DG.Tweening.Core.Enums.SafeModeLogBehaviour.None;
		Log.LogMessage("DisableDOTweenLogging is enabled.");
    }
}


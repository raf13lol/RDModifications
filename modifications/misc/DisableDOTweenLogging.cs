using BepInEx.Configuration;
using BepInEx.Logging;
using DG.Tweening;

namespace RDModifications;

[Modification]
public class DisableDOTweenLogging
{
    public static ManualLogSource logger;

    public static ConfigEntry<bool> enabled;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("DisableDOTweenLogging", "Enabled", false,
        "Prevents the tweening engine of the game from logging useless information. Cleans up logs.");

        if (enabled.Value)
        {
            DOTween.onWillLog = (_, _) => false;
            DOTween.logBehaviour = LogBehaviour.ErrorsOnly;
            DOTween.safeModeLogBehaviour = DG.Tweening.Core.Enums.SafeModeLogBehaviour.None;
            logger.LogMessage("DisableDOTweenLogging: Enabled! Logged so if the log is sent to developers, they know why there's no logs from DOTween.");
        }

        return enabled.Value;
    }
}


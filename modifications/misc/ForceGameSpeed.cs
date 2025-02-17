using BepInEx.Configuration;
using BepInEx.Logging;
using DG.Tweening;
using HarmonyLib;
using UnityEngine;

namespace RDModifications;

[Modification]
public class ForceGameSpeed
{
    public static ManualLogSource logger;

    public static ConfigEntry<bool> enabled;
    public static ConfigEntry<float> gameSpeed;
    public static ConfigEntry<bool> always;

    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("ForceGameSpeed", "Enabled", false,
        "If the game speed should be forced to a specific value in levels or all the time.\n" + 
        "(May cause some oddities due the modification being very forceful with setting the speed.)");
 
        gameSpeed = config.Bind("ForceGameSpeed", "GameSpeed", 1f,
        "What speed the game should be forced to be at.");

        always = config.Bind("ForceGameSpeed", "Always", true,
        "If enabled, the game speed will always be set to the desired value instead of only in levels.");

        return enabled.Value;
    }

    [HarmonyPatch(typeof(scnBase), "Update")]
    private class ForceSpeedPatch
    {
        public static void Postfix(scnBase __instance)
        {
            if (!always.Value && __instance is not scnGame)
                return;
            RDTime.speed = gameSpeed.Value;
            Time.timeScale = gameSpeed.Value;
            DOTween.timeScale = gameSpeed.Value;
        }
    }
}
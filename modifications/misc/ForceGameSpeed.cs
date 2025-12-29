using BepInEx.Configuration;
using DG.Tweening;
using HarmonyLib;
using UnityEngine;

namespace RDModifications;

[Modification(
	"If the game speed should be forced to a specific value in levels or all the time.\n" + 
	"(May cause some oddities due the modification being very forceful with setting the speed.)"
)]
public class ForceGameSpeed : Modification
{
	[Configuration<float>(1f, "What speed the game should be forced to be at.")]
    public static ConfigEntry<float> GameSpeed;

	[Configuration<bool>(true, "If enabled, the game speed will always be set to the desired value instead of only in levels.")]
    public static ConfigEntry<bool> Always;

    [HarmonyPatch(typeof(scnBase), "Update")]
    private class ForceSpeedPatch
    {
        public static void Postfix(scnBase __instance)
        {
            if (!Always.Value && __instance is not scnGame)
                return;
            RDTime.speed = GameSpeed.Value;
            Time.timeScale = GameSpeed.Value;
            DOTween.timeScale = GameSpeed.Value;
            if (__instance is scnGame game)
                game.visualSpeed = GameSpeed.Value;
        }
    }
}
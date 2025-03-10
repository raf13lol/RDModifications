using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using DG.Tweening;
using HarmonyLib;
using RDLevelEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using static scnCLS;

namespace RDModifications;

[Modification]
public class MassUnwrapLevels
{
    public static ManualLogSource logger;

    public static ConfigEntry<bool> enabled;
    public static ConfigEntry<string> holdKey;
    public static ConfigEntry<string> pressKey;

    public static KeyCode holdKeyCode = KeyCode.None;
    public static KeyCode pressKeyCode = KeyCode.None;

    // err this code isn't the best 
    public static bool Init(ConfigFile config, ManualLogSource logging)
    {
        logger = logging;
        enabled = config.Bind("MassUnwrapLevels", "Enabled", false,
        "If enabled, pressing a certain key combination in the custom level select screen will unwrap all wrapped levels.\n" + 
        "Depending on the amount of wrapped levels, this could cause severe lag.");

        holdKey = config.Bind("MassUnwrapLevels", "HoldKey", "LeftControl",
        "The key that should be held in combination with PressKey being pressed to unwrap all wrapped levels.\n" +
        "Set this to 'None' to disable needing to hold an extra key.\n" +
        "(list of acceptable values: https://docs.unity3d.com/ScriptReference/KeyCode.html)");

        pressKey = config.Bind("MassUnwrapLevels", "PressKey", "Return",
        "The key that should be pressed in combination with HoldKey being held to unwrap all wrapped levels.\n" +
        "(list of acceptable values: https://docs.unity3d.com/ScriptReference/KeyCode.html)");

        // specifically this part
        if (Enum.TryParse(typeof(KeyCode), holdKey.Value, out object keyCodeHold))
            holdKeyCode = (KeyCode)keyCodeHold;
        else
        {
            logger.LogWarning("MassUnwrapLevels: The value of HoldKey is not a valid key. Resetting to LeftControl...");
            holdKey.Value = (string)holdKey.DefaultValue;
            holdKeyCode = KeyCode.LeftControl;
        }

        if (Enum.TryParse(typeof(KeyCode), pressKey.Value, out object keyCodePress))
            pressKeyCode = (KeyCode)keyCodePress;
        else
        {
            logger.LogWarning("MassUnwrapLevels: The value of PressKey is not a valid key. Resetting to Return (also known as the Enter key)...");
            pressKey.Value = (string)pressKey.DefaultValue;
            pressKeyCode = KeyCode.Return;
        }

        return enabled.Value && pressKeyCode != KeyCode.None;
    }

    [HarmonyPatch(typeof(scnCLS), "Update")]
    private class MassUnwrapPatch
    {
        public static bool Prefix(scnCLS __instance)
        {
            if ((holdKeyCode == KeyCode.None || Input.GetKey(holdKeyCode)) && Input.GetKeyDown(pressKeyCode))
            {
                bool unwrapSound = false;
                foreach (CustomLevelData data in __instance.levelsData)
                {
                    Rank rank = Persistence.GetCustomLevelRank(data.Hash);
                    if (rank != Rank.NeverSelected && rank != Rank.NotAvailable)
                        continue;
                    CustomLevel syringe = __instance.visibleLevels.Find((l) => l.path == data.path);
                    Persistence.SetCustomLevelRank(data.Hash, Rank.NotFinished, 1f);
                    if (syringe != null)
                    {
                        RDLevelSettings settings = data.settings;
                        syringe.CurrentRank = Rank.NotFinished;

                        __instance.CanReceiveInput = false;
                        if (__instance.CurrentLevel == syringe)
                        {
                            string path = data.path;
                            if (__instance.lastLevelsPendingData.Contains(path))
                            {
                                __instance.levelDetail.ShowLevelData((LevelPendingData)__instance.lastLevelsPendingData[path]);
                                UnwrapLevel(syringe);
                            }
                            else
                                syringe.PlayUnwrapAnimation();
                        }
                        else
                            UnwrapLevel(syringe);

                        syringe.FadeOverlay(true, true);
                        syringe.SetTextAnchoredPosition(syringe.artistText, syringe.artistRect);
                        syringe.SetTextAnchoredPosition(syringe.songText, syringe.songRect);

                        __instance.CheckTextScrolling(syringe.artistRect, syringe.artistText, settings.artist, 109f);
                        __instance.CheckTextScrolling(syringe.songRect, syringe.songText, settings.song, 109f);
                        
                    }
                    if (!unwrapSound)
                    {
                        scrConductor.PlayImmediately("sndLibrarySyringeUnwrap", 1f, RDUtils.GetMixerGroup("CustomLevelSelect"));
                        unwrapSound = true;
                    }
                }
                return false;
            }
            return true;
        }

        // MODIFIED CustomLevel.PlayUnwrapAnimation as it calls a function at the end which doesn't work correctly when it's not the current selected level
        private static void UnwrapLevel(CustomLevel level)
        {
            _ = level.boxBrokenAnimation.currentAnimationData.sprites.Length;
            _ = level.boxBrokenAnimation.singleSpriteDuration;
            level.boxBrokenAnimation.Play();
            
            float delay = 4f * level.boxBrokenAnimation.singleSpriteDuration;
            Vector2 boxBrokenStartPos = level.boxBroken.anchoredPosition;
            level.boxBroken.DOAnchorPosY(-130f, 0.5f).SetEase(Ease.InFlash).SetUpdate(isIndependentUpdate: true)
                .SetDelay(delay)
                .OnComplete(delegate
                {
                    level.boxBroken.anchoredPosition = boxBrokenStartPos;
                    level.boxBrokenAnimation.Stop();
                    level.boxBroken.gameObject.SetActive(value: false);
                });
            float endValue = level.box.anchoredPosition.x - 200f;
            float positionToPlaySyringeBody = level.box.sizeDelta.x - 200f;
            Vector2 boxStartPos = level.box.anchoredPosition;
            level.box.DOAnchorPosX(endValue, 0.5f).SetEase(Ease.InQuart).SetUpdate(isIndependentUpdate: true)
            .SetDelay(delay)
            .OnUpdate(delegate
            {
                if (level.box.anchoredPosition.x <= positionToPlaySyringeBody)
                {
                    level.bodyAnimation.Play();
                }
            })
            .OnComplete(delegate
            {
                level.PlayPlungerIdle(play: true);
                level.box.anchoredPosition = boxStartPos;
                level.box.gameObject.SetActive(value: false);
                level.cls.CanReceiveInput = true;
            });
        }
    }
}
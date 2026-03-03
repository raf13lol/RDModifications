using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using OggVorbisEncoder.Setup;
using RDLevelEditor;
using UnityEngine;

namespace RDModifications;

[Modification("If to be able to remove row visuals with an option.")]
public class Blindfolded : Modification
{
    [Configuration<bool>(false,
        "The starting value of the blindfolded option.\n" +
        "Do note the option in-game (if that setting is enabled) alters this value."
    )]
    public static ConfigEntry<bool> SavedEnabled;

    [Configuration<bool>(true, "If to show the option for this in the settings menu, under Advanced.")]
    public static ConfigEntry<bool> DisplayOption;

    public class BlindfoldedVisualsPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RowEntity), nameof(RowEntity.Setup))]
        [HarmonyPatch(typeof(RowEntity), nameof(RowEntity.Show))]
        [HarmonyPatch(typeof(RowEntity), nameof(RowEntity.DoEntrance))]
        public static void RowPostfix(RowEntity __instance)
        {
            if (!SavedEnabled.Value)
                return;
            bool isClassyCC = __instance.character.customAnimation.data.name.Contains("classybeat", StringComparison.OrdinalIgnoreCase);
            __instance.Hide(__instance.character.visible && !isClassyCC, false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RowEntity), "Update")]
        public static void UpdatePostfix(RowEntity __instance)
        {
            if (!SavedEnabled.Value || !__instance.character.visible)
                return;
            bool isClassyCC = __instance.character.customAnimation.data.name.Contains("classybeat", StringComparison.OrdinalIgnoreCase);
            __instance.character.visible = !isClassyCC;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(RowEntity), nameof(RowEntity.ExpressionPlusFX))]
        public static void FXPrefix(RowEntity __instance)
            => __instance.game.currentLevel.noHitParticles |= SavedEnabled.Value;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LevelEvent_MakeSprite), "CreateSprite")]
        public static void ClassyBeatPostfix(LevelEvent_MakeSprite __instance)
        {
            if (!SavedEnabled.Value || !__instance.filename.Contains("classybeat", StringComparison.OrdinalIgnoreCase))
                return;
            CustomSprite sprite = __instance.game.currentLevel.sprites[__instance.spriteId];
            sprite.gameObject.SetActive(false);
            sprite.gameObject.AddComponent<BlindfoldedMarkedForDeath>();
        }
    }

    public class SetVisibleClassyBeatPatch
    {
        public static MethodInfo TargetMethod()
            => AccessUtils.GetFirstMethodContains(typeof(LevelEvent_SetVisible), "<Run>");

        [HarmonyPostfix]
        public static void Postfix(LevelEvent_SetVisible __instance)
        {
            if (!SavedEnabled.Value)
                return;
            Dictionary<string, CustomSprite> sprites = __instance.game.currentLevel.sprites;
            if (!sprites.TryGetValue(__instance.target, out CustomSprite sprite) || !sprite.GetComponent<BlindfoldedMarkedForDeath>())
                return;
            sprite.gameObject.SetActive(false);
        }
    }

    public class BlindfoldedOptionPatch
    {
        public const PauseContentName BlindfoldedIndex = (PauseContentName)54378;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PauseMenuData), nameof(PauseMenuData.Initialize))]
        public static void PauseDataPrefix(PauseMenuData __instance, bool ___isInitialized)
        {
            if (!DisplayOption.Value || ___isInitialized)
                return;

            foreach (PauseMenuModeData modeData in __instance.modes)
            {
                if (modeData.name != PauseModeName.AdvancedSettings)
                    continue;

                List<PauseContentName> names = [.. modeData.contentNames];
                names.Insert(0, BlindfoldedIndex);
                modeData.contentNames = [.. names];
            }

            List<PauseMenuContentData> contents = [.. __instance.contents];
            contents.Add(new()
            {
                name = BlindfoldedIndex,
                valueType = PauseContentValueType.Bool,
                falseText = "enum.ToggleBool.off",
                trueText = "enum.ToggleBool.on",
            });

            __instance.contents = [.. contents];
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PauseModeContentArrows), nameof(PauseModeContentArrows.UpdateValue))]
        public static void UpdateValuePostfix(PauseModeContentArrows __instance)
        {
            if (__instance.contentData.name != BlindfoldedIndex)
                return;
            __instance.SetStartValueText(SavedEnabled.Value);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PauseModeContentArrows), nameof(PauseModeContentArrows.ChangeContentValue))]
        public static void ChangeContentValuePostfix(PauseModeContentArrows __instance, int direction, bool shiftPressed)
        {
            if (__instance.contentData.name != BlindfoldedIndex)
                return;
            __instance.valueKey = __instance.contentData.ChangeValue(direction, shiftPressed, out var isKey);
            __instance.SetValueText(__instance.valueKey, isKey);
            SavedEnabled.Value = __instance.contentData.CurrentBoolValue;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(RDString), nameof(RDString.Get))]
        public static bool LocalisationPrefix(ref string __result, string key)
        {
            if (RDString.samuraiMode)
                return true;

            if (key == $"pauseMenu.{BlindfoldedIndex}")
            {
                __result = "Blindfolded";
                return false;
            }

            if (key == $"pauseMenu.levelDetail.{BlindfoldedIndex}")
            {
                __result = "If row visuals should be hidden. Requires restarting the level for changes to properly apply.";
                return false;
            }

            return true;
        }
    }

    public class BlindfoldedMarkedForDeath : MonoBehaviour { }
}
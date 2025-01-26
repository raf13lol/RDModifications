// i heard iemand wilt this

using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Logging;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System.Reflection;
using UnityEngine.UI;

namespace RDModifications
{
    public class PretendFOnMistake
    {
        public static ConfigEntry<bool> enabled;
        public static ConfigEntry<bool> display;
        public static ConfigEntry<bool> say;

        public static ConfigEntry<LevelRank> rankToDisplayAndSay;
        public static ConfigEntry<float> sayVolume;
        public static ConfigEntry<float> duration;

        public static string text;

        public static ManualLogSource logger;

        public static void Init(Harmony patcher, ConfigFile config, ManualLogSource logging, ref bool anyEnabled)
        {
            logger = logging;

            enabled = config.Bind("PretendFOnMistake", "Enabled", false, "Whether a pretend rank should be shown and/or said on each mistake.");
            display = config.Bind("PretendFOnMistake", "Display", true, "Whether the pretend rank should be shown on each mistake.");
            say = config.Bind("PretendFOnMistake", "Say", true, "Whether the pretend rank should be said on each mistake.");

            rankToDisplayAndSay = config.Bind("PretendFOnMistake", "RankToDisplayAndSay", LevelRank.F, "What the pretend rank should be.");
            sayVolume = config.Bind("PretendFOnMistake", "SayVolume", 1f, "How loud the said pretend rank should be said.");
            duration = config.Bind("PretendFOnMistake", "Duration", 0.5f, "How long the shown pretend rank should be shown for.\n(seconds)");

            text = rankToDisplayAndSay.Value.ToString().Replace("Minus", "-").Replace("Plus", "+");

            if (enabled.Value)
            {
                if (sayVolume.Value < 0f)
                {
                    sayVolume.Value = 1f;
                    logger.LogWarning("PretendFOnMistake: Invalid SayVolume, value is reset to 1");
                }
                if (duration.Value < 0f)
                {
                    duration.Value = 0.5f;
                    logger.LogWarning("PretendFOnMistake: Invalid Duration, value is reset to 0.5");
                }
                patcher.PatchAll(typeof(FPatch));
                anyEnabled = true;
            }
        }

        // mwehehehehe
        private class FPatch
        {
            private static TweenerCore<UnityEngine.Color, UnityEngine.Color, ColorOptions> rankscreenTween = null;
            private static TweenerCore<UnityEngine.Color, UnityEngine.Color, ColorOptions> rankTween = null;
            private static TweenerCore<UnityEngine.Color, UnityEngine.Color, ColorOptions> headerTween = null;

            private static float baseAlpha = 0.0f;

            [HarmonyPostfix]
            [HarmonyPatch(typeof(MistakesManager), nameof(MistakesManager.UpdateConsecutiveMistakes))]
            public static void ShowFPostfix(ref RDPlayer player, ref OffsetType ot)
            {
                static bool isInOver(FieldInfo field, HUD hud)
                {
                    return (int)field.GetValue(hud) > 0;
                }

                if (ot == OffsetType.Perfect)
                    return;
                HUD hud = scnGame.instance.hud;
                FieldInfo field = typeof(HUD).GetField("trueGameover", BindingFlags.NonPublic | BindingFlags.Instance);

                if (isInOver(field, hud))
                    return;

                if (say.Value)
                    scrConductor.PlayImmediately("sndJyi - Rank" + text, sayVolume.Value, RDUtils.GetMixerGroup("RDGSVoice"), 1f, 0f, false, false, false);

                if (!display.Value)
                    return;
                // F
                hud.UpdateFontSizes();
                if (rankscreenTween != null)
                    rankscreenTween.Complete();
                if (rankTween != null)
                    rankTween.Complete();
                if (headerTween != null)
                    headerTween.Complete();

                Image img = hud.rankscreen.GetComponent<Image>();
                hud.rankscreen.SetActive(true);
                hud.header.gameObject.SetActive(true);
                hud.rank.gameObject.SetActive(true);
                hud.rank.text = text;
                float duration = 0.5f;
                if (baseAlpha == 0.0f)
                    baseAlpha = img.color.a;

                rankscreenTween = img.DOFade(0f, duration).SetEase(Ease.Linear).SetUpdate(true).OnComplete(delegate
                {
                    if (!isInOver(field, hud))
                        hud.rankscreen.gameObject.SetActive(false);
                    img.DOFade(baseAlpha, 0.0f).SetEase(Ease.Linear).SetUpdate(true);
                    rankscreenTween = null;
                });
                rankTween = hud.rank.DOFade(0f, duration).SetEase(Ease.Linear).SetUpdate(true).OnComplete(delegate
                {
                    if (!isInOver(field, hud))
                        hud.rank.gameObject.SetActive(false);
                    hud.rank.DOFade(1f, 0.0f).SetEase(Ease.Linear).SetUpdate(true);
                    rankTween = null;
                });
                headerTween = hud.header.DOFade(0f, duration).SetEase(Ease.Linear).SetUpdate(true).OnComplete(delegate
                {
                    if (!isInOver(field, hud))
                        hud.header.gameObject.SetActive(false);
                    hud.header.DOFade(1f, 0.0f).SetEase(Ease.Linear).SetUpdate(true);
                    headerTween = null;
                });

                // just in case so
                hud.description.gameObject.SetActive(false);
                // hud.statusText.gameObject.SetActive(false);
                hud.descriptionLayoutGroup.gameObject.SetActive(false);
                hud.multiplayerResultsLayoutGroup.gameObject.SetActive(false);
                hud.resultsSingleplayer.gameObject.SetActive(false);
                hud.resultsP1.gameObject.SetActive(false);
                hud.resultsP2.gameObject.SetActive(false);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HUD), nameof(HUD.AdvanceGameover))]
            public static void PreventFadeOutRank()
            {
                if (rankscreenTween != null)
                    rankscreenTween.Complete();
                if (rankTween != null)
                    rankTween.Complete();
                if (headerTween != null)
                    headerTween.Complete();
            }

        }

        public enum LevelRank
        {
            FMinus,
            F,
            FPlus,
            DMinus,
            D,
            DPlus,
            CMinus,
            C,
            CPlus,
            BMinus,
            B,
            BPlus,
            AMinus,
            A,
            APlus,
            SMinus,
            S,
            SPlus
        }
    }

}
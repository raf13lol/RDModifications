// i heard someone wanted this

using System.Reflection;
using BepInEx.Configuration;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace RDModifications;

[Modification("If a fake rank should be shown and/or said on each mistake.")]
public class FakeRankOnMistake : Modification
{
    [Configuration<bool>(true, "If the fake rank should be shown on each mistake.")]
    public static ConfigEntry<bool> Display;
    [Configuration<bool>(true, "If the fake rank should be said on each mistake.")]
    public static ConfigEntry<bool> Say;

    [Configuration<LevelRank>(LevelRank.F, "What the fake rank should be.")]
    public static ConfigEntry<LevelRank> RankToDisplayAndSay;

    [Configuration<float>(1f, "How loud the said fake rank should be said.", [float.Epsilon, float.PositiveInfinity])]
    public static ConfigEntry<float> SayVolume;

    [Configuration<float>(0.5f, "How long the shown fake rank should be shown for in seconds.", [float.Epsilon, float.PositiveInfinity])]
    public static ConfigEntry<float> Duration;

    public static string SoundName;

    public static void Init()
    {
        SoundName = RankToDisplayAndSay.Value.ToString().Replace("Minus", "-").Replace("Plus", "+");
    }

    // mwehehehehe
    public class FPatch
    {
        public static TweenerCore<Color, Color, ColorOptions> RankscreenTween = null;
        public static TweenerCore<Color, Color, ColorOptions> RankTween = null;
        public static TweenerCore<Color, Color, ColorOptions> HeaderTween = null;

        public static float BaseAlpha = 0.0f;
        public static int LastFrame = -1;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(scnGame), nameof(scnGame.OnMistakeOrHeal))]
        public static void ShowFPostfix(float weight)
        {
            static bool isInOver(FieldInfo field, Rankscreen hud)
                => (int)field.GetValue(hud) > 0;

            if (weight <= 0.0f || LastFrame == Time.frameCount)
                return;
            Rankscreen rankscreen = scnGame.instance.rankscreen;
            FieldInfo field = AccessTools.Field(typeof(Rankscreen), "trueGameover");

            if (isInOver(field, rankscreen))
                return;

            LastFrame = Time.frameCount;
            if (Say.Value)
                scrConductor.PlayImmediately("sndJyi - Rank" + SoundName, SayVolume.Value * Mathf.Clamp01(weight), RDUtils.GetMixerGroup("RDGSVoice"), 1f, 0f, false, false, false);

            if (!Display.Value)
                return;
            // F
            rankscreen.UpdateFontSizes();
            RankscreenTween?.Complete();
            RankTween?.Complete();
            HeaderTween?.Complete();

            Image img = rankscreen.rankscreen.GetComponent<Image>();
            rankscreen.rankscreen.SetActive(true);
            rankscreen.header.gameObject.SetActive(true);
            rankscreen.rank.gameObject.SetActive(true);
            rankscreen.rank.text = SoundName;
            float duration = 0.5f;
            if (BaseAlpha == 0.0f)
                BaseAlpha = img.color.a;

            RankscreenTween = img.DOFade(0f, duration).SetEase(Ease.Linear).SetUpdate(true).OnComplete(delegate
            {
                if (!isInOver(field, rankscreen))
                    rankscreen.rankscreen.gameObject.SetActive(false);
                img.DOFade(BaseAlpha, 0.0f).SetEase(Ease.Linear).SetUpdate(true);
                RankscreenTween = null;
            });
            RankTween = rankscreen.rank.DOFade(0f, duration).SetEase(Ease.Linear).SetUpdate(true).OnComplete(delegate
            {
                if (!isInOver(field, rankscreen))
                    rankscreen.rank.gameObject.SetActive(false);
                rankscreen.rank.DOFade(1f, 0.0f).SetEase(Ease.Linear).SetUpdate(true);
                RankTween = null;
            });
            HeaderTween = rankscreen.header.DOFade(0f, duration).SetEase(Ease.Linear).SetUpdate(true).OnComplete(delegate
            {
                if (!isInOver(field, rankscreen))
                    rankscreen.header.gameObject.SetActive(false);
                rankscreen.header.DOFade(1f, 0.0f).SetEase(Ease.Linear).SetUpdate(true);
                HeaderTween = null;
            });

            // just in case so
            rankscreen.description.gameObject.SetActive(false);
            // hud.statusText.gameObject.SetActive(false);
            rankscreen.descriptionLayoutGroup.gameObject.SetActive(false);
            rankscreen.multiplayerResultsLayoutGroup.gameObject.SetActive(false);
            rankscreen.resultsSingleplayer.gameObject.SetActive(false);
            rankscreen.resultsP1.gameObject.SetActive(false);
            rankscreen.resultsP2.gameObject.SetActive(false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Rankscreen), nameof(Rankscreen.AdvanceGameover))]
        public static void PreventFadeOutRank()
        {
            RankscreenTween?.Complete();
            RankTween?.Complete();
            HeaderTween?.Complete();
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
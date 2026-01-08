// i heard someone wanted this

using BepInEx.Configuration;
using HarmonyLib;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine;

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

	[Configuration<float>(1f, "How loud the said fake rank should be said.")]
    public static ConfigEntry<float> SayVolume;

	[Configuration<float>(0.5f, "How long the shown fake rank should be shown for in seconds.")]
    public static ConfigEntry<float> Duration;

    private static string SoundName;

    public static void Init()
    {
        SoundName = RankToDisplayAndSay.Value.ToString().Replace("Minus", "-").Replace("Plus", "+");
        if (SayVolume.Value < 0f)
        {
            SayVolume.Value = 1f;
            Log.LogWarning("FakeRankOnMistake: Invalid SayVolume, value is reset to 1");
        }
        if (Duration.Value < 0f)
        {
            Duration.Value = 0.5f;
            Log.LogWarning("FakeRankOnMistake: Invalid Duration, value is reset to 0.5");
        }
    }

    // mwehehehehe
    private class FPatch
    {
        private static TweenerCore<Color, Color, ColorOptions> RankscreenTween = null;
        private static TweenerCore<Color, Color, ColorOptions> RankTween = null;
        private static TweenerCore<Color, Color, ColorOptions> HeaderTween = null;

        private static float BaseAlpha = 0.0f;
		private static int LastFrame = -1;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(scnGame), nameof(scnGame.OnMistakeOrHeal))]
        public static void ShowFPostfix(float weight)
        {
            static bool isInOver(FieldInfo field, Rankscreen hud)
                => (int)field.GetValue(hud) > 0;

            if (weight <= 0.0f || LastFrame == Time.frameCount)
                return;
            Rankscreen rankscreen = scnGame.instance.rankscreen;
            FieldInfo field = typeof(Rankscreen).GetField("trueGameover", BindingFlags.NonPublic | BindingFlags.Instance);

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
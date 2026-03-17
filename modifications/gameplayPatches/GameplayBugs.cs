using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RDLevelEditor;
using UnityEngine;

namespace RDModifications;

[Modification("If bugs within the game should be fixed.")]
public class GameplayBugs : Modification
{
    public class SetGameSoundCompatibilityPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LevelEvent_SetGameSound), nameof(LevelEvent_SetGameSound.Decode))]
        public static void DecodePostfix(LevelEvent_SetGameSound __instance, Dictionary<string, object> dict)
        {
            if (dict.ContainsKey("soundSubtypes"))
                return;
            if (!RDEditorConstants.gameSoundGroups.TryGetValue(__instance.soundType, out GameSoundType[] array))
                return;
            for (int i = 0; i < __instance.sounds.Length; i++)
                __instance.sounds[i].groupSubtype = __instance.sounds[i].used ? array[i] : (GameSoundType)int.MaxValue;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LevelEvent_SetGameSound), nameof(LevelEvent_SetGameSound.Run))]
        public static void RunPrefix(LevelEvent_SetGameSound __instance)
        {
            for (int i = 0; i < __instance.sounds.Length; i++)
            {
                if (!__instance.sounds[i].used || __instance.sounds[i].groupSubtype != (GameSoundType)int.MaxValue)
                    continue;
                __instance.sounds[i].used = false;
            }
        }
    }

    // [HarmonyPatch(typeof(scrChar), nameof(scrChar.OnCustomAnimEnd))]
    // public class NeutralLoopFixPatch
    // {
    //     public static void ILManipulator(ILContext il)
    //     {
    //         ILCursor cursor = new(il);

    //         void replaceNeutralWithNeutralAnimationName()
    //         {
    //             cursor.GotoNext(x => x.MatchLdstr("neutral"));
    //             cursor.Remove();
    //             cursor.Emit(OpCodes.Ldarg_0);
    //             cursor.Emit(OpCodes.Ldfld, AccessTools.Field(typeof(scrChar), nameof(scrChar.neutralAnimName)));
    //         }

    //         replaceNeutralWithNeutralAnimationName(); // customAnimation.currentClip.name != "neutral"
    //         replaceNeutralWithNeutralAnimationName(); // customAnimation.data.clips.TryGetValue("neutral", out var value)
    //         replaceNeutralWithNeutralAnimationName(); // PlayExpression("neutral", ...);
    //     }
    // }

    public class SetCountingSoundPatch
    {
        // I've done some bullshit because otherwise it just doesn't work?
        public static MethodInfo TargetMethod()
            => AccessUtils.GetFirstInnerMethodContains(typeof(LevelEvent_SetCountingSound), "<Prepare>", "MoveNext");

        [HarmonyPostfix]
        public static void Postfix(IEnumerator __instance, bool __result)
        {
            if (__result)
                return;

            LevelEvent_SetCountingSound levelEvent = (LevelEvent_SetCountingSound)AccessUtils.GetFirstFieldContains(__instance.GetType(), "this").GetValue(__instance);
            if (levelEvent.voiceSource != CountingVoiceSource.Custom)
                return;

            SoundData[] soundsData = (SoundData[])AccessTools.Field(typeof(LevelEvent_SetCountingSound), "soundsData").GetValue(levelEvent);
            for (int i = 0; i < soundsData.Length; i++)
            {
                SoundData soundData = soundsData[i];
                if (soundData == null || soundData.filename.HasAudioFileExtension() || soundData.externalClip)
                    continue;

                soundData.itsASong = true;
                if (soundData.filename.StartsWith("sndsnd"))
                    soundData.filename = soundData.filename[3..];
            }
        }
    }

    public class BurnshotPatch
    {
        public static MethodInfo TargetMethod()
            => AccessUtils.GetFirstInnerMethodContains(typeof(LevelBase), nameof(LevelBase.LoadCustomAssets), "MoveNext");

        public static void ILManipulator(ILContext il)
        {
            ILCursor cursor = new(il);

            // cursor.GotoNext(x => x.MatchCallvirt(AccessTools.Method(typeof(LevelEvent_Base), nameof(LevelEvent_Base.BarAndBeatToAbsoluteBeat))));
            cursor.GotoNext(MoveType.After, x => x.MatchIsinst(typeof(LevelEvent_AddOneshotBeat)));

            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldc_I4_0);

            cursor.GotoNext(x => x.MatchLdloc(3));
            cursor.Index--;

            cursor.Emit(OpCodes.Ldloc, 1);
            cursor.EmitDelegate((LevelBase level) =>
            {
                LevelEvent_AddOneshotBeat[] oneshots = [.. (from levelEvent in level.levelEvents
                                                            where levelEvent is LevelEvent_AddOneshotBeat
                                                            orderby ((LevelEvent_AddOneshotBeat)levelEvent).absoluteClapPos ascending,
                                                                levelEvent.bar ascending,
                                                                levelEvent.beat ascending
                                                            select (LevelEvent_AddOneshotBeat)levelEvent)];

                for (int i = 1; i < oneshots.Length; i++)
                {
                    LevelEvent_AddOneshotBeat oneshot = oneshots[i];
                    LevelEvent_AddOneshotBeat previousOneshot = oneshots[i - 1];
                    if (oneshot.bar == previousOneshot.bar || Math.Abs(oneshot.absoluteClapPos - previousOneshot.absoluteClapPos) >= 0.001f)
                        continue;

                    oneshot.beat += oneshot.BarAndBeatToAbsoluteBeat(new(oneshot.bar, 1)) - previousOneshot.BarAndBeatToAbsoluteBeat(new(previousOneshot.bar, 1));
                    oneshot.bar = previousOneshot.bar;
                }

                int maxBar = 0;
                foreach (LevelEvent_Base levelEvent in level.levelEvents)
                    maxBar = Mathf.Max(levelEvent.bar, maxBar);
                return maxBar;
            });
            cursor.Emit(OpCodes.Stloc, 3);
        }
    }

    [HarmonyPatch(typeof(RowEntity), nameof(RowEntity.ChangeCharacterCustom))]
    public class FreezeshotSpriteFreezePatch
    {
        public static void Postfix(RowEntity __instance)
            => __instance.freezeshotIceController.Setup(__instance.character.customAnimation.data.freezeTexture);
    }
}
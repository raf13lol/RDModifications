using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        public static void DecodePostfix(LevelEvent_SetGameSound __instance, Dictionary<string, object> dict, SoundData[] ___soundsData)
        {
            if (dict.ContainsKey("soundSubtypes"))
                return;
            if (!RDEditorConstants.gameSoundGroups.TryGetValue(__instance.soundType, out GameSoundType[] array))
                return;
            for (int i = 0; i < ___soundsData.Length; i++)
                ___soundsData[i].groupSubtype = ___soundsData[i].used ? array[i] : (GameSoundType)int.MaxValue;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LevelEvent_SetGameSound), nameof(LevelEvent_SetGameSound.Run))]
        public static void RunPrefix(SoundData[] ___soundsData)
        {
            for (int i = 0; i < ___soundsData.Length; i++)
            {
                if (!___soundsData[i].used || ___soundsData[i].groupSubtype != (GameSoundType)int.MaxValue)
                    continue;
                ___soundsData[i].used = false;
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

    [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.FindOrLoadAudioClip))]
    public class AllSndBugPatch
    {
        public static void Postfix(ref AudioClip __result, string clipName)
        {
            string filename = Path.GetFileName(clipName);
            if (__result != null || !filename.StartsWith("snd"))
                return;
            __result = AudioManager.Instance.FindOrLoadAudioClip(Path.Combine(Path.GetDirectoryName(clipName), filename[3..]));
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

    public class Fix2PixelOffsetRowsPatch
    {
        public static float RowLeftPosition = 19f;

        public static void AdjustTransform(Transform transform, bool increase = false)
        {
            Vector3 position = transform.localPosition;
            if (increase)
                position.x += 2; // 2px
            position.x -= 2; // 2px
            transform.localPosition = position;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RowEntity), nameof(RowEntity.Setup))]
        public static void Postfix(RowEntity __instance)
        {
            if (scnGame.instance == null || scnEditor.instance != null
            || scnGame.levelToLoadSource != LevelSource.ExternalPath || RDLevelData.current.settings.version >= 65)
            {
                RowLeftPosition = 19f;
                return;
            }
            RowLeftPosition = 17f;
            AdjustTransform(__instance.classicRowController.transform);
            AdjustTransform(__instance.lineHitContainer);
            AdjustTransform(__instance.heartContainer.parent, true);
        }

        [HarmonyILManipulator]
        [HarmonyPatch(typeof(RowEntity), nameof(RowEntity.GetRowWidth))]
        [HarmonyPatch(typeof(RowEntity), nameof(RowEntity.SetRowLength))]
        public static void ILManipulator(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.GotoNext(x => x.MatchLdcR4(19f));
            cursor.Instrs[cursor.Index].OpCode = OpCodes.Ldsfld;
            cursor.Instrs[cursor.Index].Operand = AccessTools.Field(typeof(Fix2PixelOffsetRowsPatch), nameof(RowLeftPosition));
        }
    }

    [HarmonyPatch(typeof(SoundDataStruct), nameof(SoundDataStruct.Decode))]
    public class PunchBeatsoundTooLoudPatch
    {
        public static void Postfix(ref SoundDataStruct __result, IReadOnlyDictionary<string, object> dict)
        {
            if ((string)dict["filename"] != "Punch" || RDLevelData.current.settings.version >= 51)
                return;
            float multiplier = 130f / 200f; // Around ? based off me messing around in enchanted love by kin
            __result = __result.WithNewVolume((int)Mathf.Round(__result.volume * multiplier));
        }
    }
}
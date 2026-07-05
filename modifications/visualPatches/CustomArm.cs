// TODO: support without arm templates
// TODO: sort out default arms having default sprites - material bullshit, can i make it work ?
// TODO: test test test test and test disablepalette too and then test for more bugs
// TODO: add cache (can be done after release)

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using BepInEx.Configuration;
using HarmonyLib;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RDModifications;

[Modification("If the hand and arm sprites of each slot and player can be changed. For more info on how to do this, consult the README.md on the github.")]
public class CustomArm : Modification
{
    [Configuration<bool>(false,
    "If the palettes of the custom arms should be disabled.\n" +
    "This means that you cannot change the skin colour, palm lightness nor nail colour of the arms in-game, but you are able to have as many colours as you want in the sprites."
    )]
    public static ConfigEntry<bool> DisablePalette;

    [HarmonyPatch(typeof(SlotUI), nameof(SlotUI.SetSkin))]
    public class SlotUIPatch
    {
        public static void Postfix(SlotUI __instance)
        {
            // idk where else slotui is used in game but idk 
            if (!scnMenu.instance)
                return;

            if (!CustomSprites.HasCustomArm(__instance.slot))
                return;

            ArmTextures texs = CustomSprites.GetArmTextures(__instance.slot);

            __instance.arm.material.SetTexture("_MainTex", texs.SleeveTexture);
            __instance.arm.material.SetTexture("_SleeveMask", texs.SleeveTexture);

            __instance.handFrames = texs.SlotHandFrames;
            __instance.palmFrames = texs.SlotHandPalmFrames;
            __instance.nailFrames = texs.SlotHandNailsFrames;
            __instance.SetOpen(!__instance.leftArrow.enabled);
        }
    }

    [HarmonyPatch(typeof(Rankscreen), nameof(Rankscreen.ShowSmallHand))]
    public class SmallHandPatch
    {
        public static void Postfix(Rankscreen __instance)
        {
            if (!CustomSprites.HasCustomArm())
                return;

            ArmTextures texs = CustomSprites.GetArmTextures();
            SpriteAnimation anim = __instance.smallHand.GetComponent<SpriteAnimation>();
            Image image = __instance.smallHand.GetComponent<Image>();

            Sprite[] sprites;
            if (scnGame.p1DefibMode > DefibMode.Normal)
                sprites = texs.SmallHandHardButtonFrames;
            else if (scnGame.p1DefibMode < DefibMode.Normal)
                sprites = texs.SmallHandEasyButtonFrames;
            else
                sprites = texs.SmallHandNormalButtonFrames;

            image.sprite = sprites[1];
            anim.currentAnimationData.sprites = sprites;
        }
    }

    [HarmonyPatch(typeof(SleevePreview), nameof(SleevePreview.SetArmSkin))]
    public class SleevePreviewPatch
    {
        public static ArmTextures? GetArmTextures(SleevePreview instance)
        {
            GameObject canvas = GameObject.Find("canvas");
            if (canvas == null)
                return null;

            scnSleevePaint paint = canvas.GetComponentInChildren<scnSleevePaint>();
            if (paint == null)
                return null;

            int player = (paint.loadPlayer1Preview == instance) ? 0 : 1;
            if (!CustomSprites.HasCustomArm(null, player))
                return null;

            return CustomSprites.GetArmTextures(null, player);
        }

        public static void Postfix(SleevePreview __instance)
        {
            ArmTextures? texs = GetArmTextures(__instance);
            if (texs == null)
                return;

            __instance.sleeve.sprite = texs.SleeveSprite;
            __instance.sleeve.material.SetTexture("_MainTex", texs.SleeveTexture);
            __instance.sleeve.material.SetTexture("_SleeveMask", texs.SleeveTexture);

            __instance.hand.texture = texs.SleevePaintHand;
            __instance.palm.texture = texs.SleevePaintHandPalm;
            __instance.nails.texture = texs.SleevePaintHandNails;
        }
    }

    [HarmonyPatch(typeof(scnSleevePaint), "Show")]
    public class SleevePaintPatch
    {
        public static void Postfix(scnSleevePaint __instance, RDPlayer player)
        {
            if (!CustomSprites.HasCustomArm(player))
                return;

            ArmTextures texs = CustomSprites.GetArmTextures(player);

            __instance.nails.texture = __instance.sleevePreview.nails.texture = texs.SleevePaintHandNails;
            __instance.hand.texture = __instance.sleevePreview.hand.texture = texs.SleevePaintHand;
            __instance.palm.texture = __instance.sleevePreview.palm.texture = texs.SleevePaintHandPalm;

            __instance.sleeveImage.sprite = __instance.sleevePreview.sleeve.sprite = texs.SleeveSprite;
            __instance.sleeveImage.material.SetTexture("_MainTex", texs.SleeveTexture);
            __instance.sleeveImage.material.SetTexture("_SleeveMask", texs.SleeveTexture);

            __instance.sleevePreview.sleeve.material.SetTexture("_MainTex", texs.SleeveTexture);
            __instance.sleevePreview.sleeve.material.SetTexture("_SleeveMask", texs.SleeveTexture);
        }
    }

    public class ArmPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RDArm), "Awake")]
        public static void AwakePostfix(RDArm __instance)
        {
            if (__instance.player != RDPlayer.P1 && __instance.player != RDPlayer.P2)
                return;

            if (!CustomSprites.HasCustomArm(__instance.player))
                return;

            ArmTextures texs = CustomSprites.GetArmTextures(__instance.player);
            Material handMat = __instance.hand.shaderRenderer.material;
            handMat.SetFloat("_UsesPalette", !DisablePalette.Value ? 1f : 0f);

            if (DisablePalette.Value)
                return;
            __instance.basePaletteTex = texs.Palette;
            handMat.SetTexture("_PaletteTex", texs.Palette);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RDArm), nameof(RDArm.SetSkin))]
        public static void SetSkinPostfix(RDArm __instance)
        {
            if (__instance.player != RDPlayer.P1 && __instance.player != RDPlayer.P2)
                return;

            if (!CustomSprites.HasCustomArm(__instance.player))
                return;

            ArmTextures texs = CustomSprites.GetArmTextures(__instance.player);

            __instance.drawing.material.SetTexture("_MainTex", texs.SleeveTexture);
            __instance.drawing.material.SetTexture("_SleeveMask", texs.SleeveTexture);

            Material handMat = __instance.hand.shaderRenderer.material;
            handMat.SetFloat("_UsesPalette", !DisablePalette.Value ? 1f : 0f);
            if (!DisablePalette.Value)
                handMat.SetTexture("_PaletteTex", texs.Palette);
        
            Material handSpriteMat = __instance.hand.sprite.CurrentSprite.material;
            if (!texs.HasSpritesheets)
                texs.CreateArmSpritesheets(
                    __instance.hand.animator.Library,
                    (Texture2D)handSpriteMat.mainTexture,
                    (Texture2D)handSpriteMat.GetTexture(RDShaderProperties.OutlineTexProperty),
                    (Texture2D)handSpriteMat.GetTexture(RDShaderProperties.GlowTexProperty)
                );

            handSpriteMat.mainTexture = texs.ArmSpritesheet;
            handSpriteMat.SetTexture(RDShaderProperties.OutlineTexProperty, texs.ArmOutlineSpritesheet);
            handSpriteMat.SetTexture(RDShaderProperties.GlowTexProperty, texs.ArmGlowSpritesheet);
        }
    }

    public class CustomSprites
    {
        public static int ConvertSlotPlayerToIndex(int slot, int player = 0)
            => slot + player * 3;

        public static string[] SlotPlayerHashes = new string[6];
        public static Dictionary<string, ArmTextures> HashToArmTextures = [];


        public static ArmTextures GetArmTextures(RDPlayer player)
            => GetArmTextures(null, (int)player);

        public static ArmTextures GetArmTextures(int? slot = null, int player = 0)
            => HashToArmTextures[SlotPlayerHashes[ConvertSlotPlayerToIndex(slot ?? Persistence.currentSlotIndex, player)]];

        public static bool HasCustomArm(RDPlayer player)
            => HasCustomArm(null, (int)player);

        public static bool HasCustomArm(int? slot = null, int player = 0)
            => SlotPlayerHashes[ConvertSlotPlayerToIndex(slot ?? Persistence.currentSlotIndex, player)] != null;

        public static LoadErrorCodes LoadTemplateFile(byte[] pngData, int slot, int player)
        {
            using MD5 hasher = MD5.Create();
            byte[] pngDataHashBytes = hasher.ComputeHash(pngData);
            string pngDataHash = "";

            foreach (byte b in pngDataHashBytes)
                pngDataHash += b.ToString("x2");

            if (pngDataHash == "withArmImageHash" || pngDataHash == "withoutArmImageHash")
                return LoadErrorCodes.UnchangedTemplate;
            // no need to do it all again
            if (HashToArmTextures.ContainsKey(pngDataHash))
                return LoadErrorCodes.OK;

            // TODO: check cache here once i do it

            Texture2D template = Tex2DUtils.LoadImage(pngData);
            if (template.width != 350 && template.height != 520)
            {
                UnityEngine.Object.Destroy(template);
                return LoadErrorCodes.NotValidTemplate;
            }

            Color bottomRightPixel = template.GetPixel(349, 0);
            bool isWithoutArm = bottomRightPixel.Equals(Color.black);

            // aka if pixel in bottom right is not black nor white
            if (!isWithoutArm && !bottomRightPixel.Equals(Color.white))
            {
                UnityEngine.Object.Destroy(template);
                return LoadErrorCodes.NotValidTemplate;
            }

            ArmTextures texs = new(template, isWithoutArm)
            {
                TemplateHash = pngDataHash
            };

            if (texs.ErrorCode == LoadErrorCodes.OK)
            {
                SlotPlayerHashes[ConvertSlotPlayerToIndex(slot, player)] = pngDataHash;
                HashToArmTextures[pngDataHash] = texs;
            }
            else
                texs.Dispose();

            return texs.ErrorCode;
        }
    }

    public class ArmTextures : IDisposable
    {
        public Texture2D TemplateImage;
        public string TemplateHash;
        public bool IsWithoutArmTemplate;

        public LoadErrorCodes ErrorCode = LoadErrorCodes.Unknown;

        public Texture2D SleeveTexture; // bl (0, 500), tr (261, 519)
        public Sprite SleeveSprite;

        public Texture2D Palette; // bl (263, 519), tr (274, 519)

        public Texture2D ArmSpritesheet;
        public Texture2D ArmOutlineSpritesheet;
        public Texture2D ArmGlowSpritesheet;

        public bool HasSpritesheets => ArmSpritesheet != null && ArmOutlineSpritesheet != null && ArmGlowSpritesheet != null;

        public Texture2D[] ArmSpriteTextures = new Texture2D[6];
        public Texture2D[] ArmOutlineTextures = new Texture2D[6];
        public Texture2D[] ArmGlowTextures = new Texture2D[6];

        public Texture2D SleevePaintHand; // bl (0, 105), tr (312, 139)
        public Texture2D SleevePaintHandPalm;
        public Texture2D SleevePaintHandNails;

        public Sprite[] SmallHandNormalButtonFrames = new Sprite[2];
        public Sprite[] SmallHandEasyButtonFrames = new Sprite[2];
        public Sprite[] SmallHandHardButtonFrames = new Sprite[2];

        public Sprite[] SlotHandFrames = new Sprite[2];
        public Sprite[] SlotHandPalmFrames = new Sprite[2];
        public Sprite[] SlotHandNailsFrames = new Sprite[2];


        public ArmTextures(Texture2D templateImage, bool isWithoutArmTemplate)
        {
            Vector2 middlePivot = Vector2.one / 2f;

            TemplateImage = templateImage;
            IsWithoutArmTemplate = isWithoutArmTemplate;

            SleeveTexture = Tex2DUtils.CopyCropped(templateImage, 0, 500, 262, 20, false);
            SleeveSprite = Sprite.Create(SleeveTexture, new(0, 0, 262, 20), middlePivot);
            SleeveTexture.Apply(false, !isWithoutArmTemplate);

            SleevePaintHand = Tex2DUtils.CopyCropped(templateImage, 0, 105, 313, 35, false);

            Color[] palmColours = [];
            Color[] nailColours = [];

            if (!DisablePalette.Value)
            {
                // a bit big no ? presume its for the shader's uvs
                Palette = Tex2DUtils.Create(256, 1);
                Palette.ClearTexture();
                Palette.SetPixel(13, 0, Color.black.WithAlpha(1f / 255f));
                Palette.CopyPixels(templateImage, 263, 519, 12, 1, 1, 0, false);

                palmColours = [Palette.GetPixel(7, 0), Palette.GetPixel(8, 0)];
                nailColours = [Palette.GetPixel(11, 0), Palette.GetPixel(12, 0)];

                SleevePaintHandPalm = Tex2DUtils.FilterPixels(SleevePaintHand, palmColours);
                SleevePaintHandNails = Tex2DUtils.FilterPixels(SleevePaintHand, nailColours, true);
                SleevePaintHand.Apply(false, true);
            }

            SmallHandNormalButtonFrames = [
                Sprite.Create(templateImage, new Rect(48, 447, 47, 24), middlePivot),
                Sprite.Create(templateImage, new Rect(0, 447, 47, 24), middlePivot),
            ];
            SmallHandEasyButtonFrames = [
                Sprite.Create(templateImage, new Rect(144, 447, 47, 24), middlePivot),
                Sprite.Create(templateImage, new Rect(96, 447, 47, 24), middlePivot),
            ];
            SmallHandHardButtonFrames = [
                Sprite.Create(templateImage, new Rect(240, 447, 47, 24), middlePivot),
                Sprite.Create(templateImage, new Rect(192, 447, 47, 24), middlePivot),
            ];

            Texture2D[] slotHandTexFrames = [
                Tex2DUtils.CopyCropped(templateImage, 0, 472, 45, 27, false),
                Tex2DUtils.CopyCropped(templateImage, 46, 472, 45, 27, false)
            ];

            for (int i = 0; i < slotHandTexFrames.Length; i++)
            {
                Texture2D tex = slotHandTexFrames[i];
                Rect rect = new(0, 0, tex.width, tex.height);

                if (DisablePalette.Value)
                {
                    SlotHandPalmFrames[i] = Sprite.Create(Tex2DUtils.CreateBlank(tex.width, tex.height, true), rect, middlePivot);
                    SlotHandNailsFrames[i] = Sprite.Create(Tex2DUtils.CreateBlank(tex.width, tex.height, true), rect, middlePivot);
                }
                else
                {
                    SlotHandPalmFrames[i] = Sprite.Create(Tex2DUtils.FilterPixels(tex, palmColours), rect, middlePivot);
                    SlotHandNailsFrames[i] = Sprite.Create(Tex2DUtils.FilterPixels(tex, nailColours, true), rect, middlePivot);
                }
                SlotHandFrames[i] = Sprite.Create(tex, rect, middlePivot);
            }

            slotHandTexFrames[0].Apply(false, true);
            slotHandTexFrames[1].Apply(false, true);

            HandleArmFrame(396); // catch not pressed - 0
            HandleArmFrame(345); // catch pressed frame 1 - 1
            HandleArmFrame(294); // catch pressed frame 2 - 2
            HandleArmFrame(243); // not pressed - 3
            HandleArmFrame(192); // pressed frame 1 - 4
            HandleArmFrame(141); // pressed frame 2 - 5

            ErrorCode = LoadErrorCodes.OK;
        }

        private int armFrameIndex = 0;

        private void HandleArmFrame(int bottomLeftY)
        {
            int pixelsToTouch = 350 * 50;

            Texture2D frame = Tex2DUtils.CopyCropped(TemplateImage, 0, bottomLeftY, 350, 50, false);
            Texture2D outline = tk2dSpriteCollectionBuilderUtil.OutlineTexture(frame);
            Texture2D glow = tk2dSpriteCollectionBuilderUtil.GaussianBlur(frame, 1);

            if (!DisablePalette.Value)
            {
                const int PALETTE_SIZE = 14;

                int[] palette = new int[PALETTE_SIZE];
                for (int i = 0; i <= 0x0c; i++) // #0d0000 is the last colour in the arm sprite
                    palette[i] = ARGB32.FromColor(Palette.GetPixel(i, 0)).GetHashCode();
                palette[0x0d] = new ARGB32(0xff, 0x0d, 0x00, 0x00).GetHashCode();

                NativeArray<int> frameData = frame.GetPixelData<int>(0);
                for (int i = 0; i < pixelsToTouch; i++)
                    frameData[i] = new ARGB32(0xff, (byte)UnsafeArrayUtils.IndexOf(palette, PALETTE_SIZE, frameData[i], 0), 0x00, 0x00).GetHashCode();

                frame.Apply(false, false);
            }

            NativeArray<int> outlineData = outline.GetPixelData<int>(0);
            NativeArray<int> glowData = glow.GetPixelData<int>(0);

            for (int i = 0; i < pixelsToTouch; i++)
            {
                outlineData[i] &= (int)(outlineData[i] & 0xff000000);
                glowData[i] &= (int)(glowData[i] & 0xff000000);
            }

            ArmSpriteTextures[armFrameIndex] = frame;
            ArmOutlineTextures[armFrameIndex] = Tex2DUtils.ConvertRGBA32ToARGB32(outline, false);
            ArmGlowTextures[armFrameIndex] = Tex2DUtils.ConvertRGBA32ToARGB32(glow, false);

            armFrameIndex++;
        }

        public void CreateArmSpritesheets(tk2dSpriteAnimation clipDefinitions, Texture2D _spritesheet, Texture2D _outlineSheet, Texture2D _glowSheet)
        {
            Texture2D spritesheet = Tex2DUtils.CreateCPUCopyOfGPUTexture(_spritesheet);
            Texture2D outlineSheet = Tex2DUtils.CreateCPUCopyOfGPUTexture(_outlineSheet);
            Texture2D glowSheet = Tex2DUtils.CreateCPUCopyOfGPUTexture(_glowSheet);

            foreach (tk2dSpriteAnimationClip clip in clipDefinitions.clips)
            {
                int startIndex = -1;
                switch (clip.name)
                {
                    case "HandGrey_Hand_Indexed_notpress_catch":
                        startIndex = 0;
                        break;
                    case "HandGrey_Hand_Indexed_press_catch":
                        startIndex = 1;
                        break;
                    case "HandGrey_Hand_Indexed_notpress":
                        startIndex = 3;
                        break;
                    case "HandGrey_Hand_Indexed_press":
                        startIndex = 4;
                        break;
                }

                if (startIndex == -1)
                    continue;

                for (int i = 0; i < clip.frames.Length; i++)
                {
                    tk2dSpriteAnimationFrame frame = clip.frames[i];
                    tk2dSpriteDefinition def = frame.spriteCollection.spriteDefinitions[frame.spriteId];

                    int x = Mathf.RoundToInt(def.uvs[0].x * spritesheet.width);
                    int y = Mathf.RoundToInt(def.uvs[0].y * spritesheet.height);
                    int spriteIndex = startIndex + i;

                    spritesheet.CopyPixels(ArmSpriteTextures[spriteIndex], 0, 0, 350, 50, x, y);
                    outlineSheet.CopyPixels(ArmOutlineTextures[spriteIndex], 0, 0, 350, 50, x, y);
                    glowSheet.CopyPixels(ArmGlowTextures[spriteIndex], 0, 0, 350, 50, x, y);
                }
            }

            spritesheet.Apply(false, true);
            outlineSheet.Apply(false, true);
            glowSheet.Apply(false, true);

            ArmSpritesheet = spritesheet;
            ArmOutlineSpritesheet = outlineSheet;
            ArmGlowSpritesheet = glowSheet;

            DestroyIfExists(ArmSpriteTextures);
            DestroyIfExists(ArmOutlineTextures);
            DestroyIfExists(ArmGlowTextures);
        }

        public void Dispose()
        {
            DestroyIfExists(TemplateImage);

            DestroyIfExists(SleeveSprite);

            DestroyIfExists(Palette);
            DestroyIfExists(ArmSpritesheet);
            DestroyIfExists(ArmOutlineSpritesheet);
            DestroyIfExists(ArmGlowSpritesheet);

            DestroyIfExists(SleevePaintHand);
            DestroyIfExists(SleevePaintHandNails);
            DestroyIfExists(SleevePaintHandPalm);

            DestroyIfExists(SmallHandEasyButtonFrames);
            DestroyIfExists(SmallHandNormalButtonFrames);
            DestroyIfExists(SmallHandHardButtonFrames);

            DestroyIfExists(SlotHandFrames);
            DestroyIfExists(SlotHandNailsFrames);
            DestroyIfExists(SlotHandPalmFrames);

            System.GC.SuppressFinalize(this);
        }

        void DestroyIfExists(UnityEngine.Object obj)
        {
            if (obj == null)
                return;

            try
            {
                if (obj is Sprite sprite)
                    DestroyIfExists(sprite.texture);
                UnityEngine.Object.Destroy(obj);
            }
            catch
            {
                // idc
            }
        }

        void DestroyIfExists(UnityEngine.Object[] objs)
        {
            foreach (UnityEngine.Object obj in objs)
                DestroyIfExists(obj);
        }
    }


    public enum LoadErrorCodes
    {
        OK,
        NotValidTemplate,
        UnchangedTemplate,
        Unknown
    }
}
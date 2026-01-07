using System.Collections.Generic;
using System.IO;
using GIF;
using HarmonyLib;
using RDLevelEditor;
using SFB;
using Unity.Collections;
using UnityEngine;

namespace RDModifications;

[Modification(
	"If APNGs and GIFs should be automatically split/turned into a spritesheet upon being imported.\n" + 
	"(WARNING: Do not attempt to import images that are quite big, as this may cause a crash.)"
, true)]
public class ImportAnimatedImages : Modification
{
	public static float AverageFPS = 0f;
	public static bool SetFPS = true;
	public static bool ImagesSelectorOpen = false;
	public static IAnimatedImageFile? AnimatedImage;
	public static string ImagePath;

	[HarmonyPatch(typeof(RDEditorUtils), nameof(RDEditorUtils.ShowFileSelectorForImages))]
    private class ImageSelectorPatch
    {
		public static void Prefix()
        	=> ImagesSelectorOpen = true;

        public static void Postfix(ref string[] __result)
        {
			ImagesSelectorOpen = false;
			if (AnimatedImage == null)
				return;

			List<string> values = [];
			double totalDelay = 0d;
			for (int i = 0; i < AnimatedImage.FrameCount; i++)
            {
				OutputFrame output = AnimatedImage.GetFrame();
                Texture2D frame = output.Texture;
				string path = AnimatedImage.IsAnimated ? $"{ImagePath}{i + 1}.png" : $"{ImagePath}.png";

				File.WriteAllBytes(path, ImageConversion.EncodeToPNG(frame));
				totalDelay += output.FrameDuration;

				values.Add(Path.GetFileName(path));
				Object.Destroy(frame);
            }

			AverageFPS = (float)(AnimatedImage.FrameCount / totalDelay);
			SetFPS = values.Count > 1;
			
			__result = [.. values];
			AnimatedImage.Dispose();
        }
    }

	private class SkipOverwritePatch
    {
		[HarmonyPostfix]
		[HarmonyPatch(typeof(StandaloneFileBrowser), nameof(StandaloneFileBrowser.OpenFilePanel), [typeof(string), typeof(string), typeof(ExtensionFilter[]), typeof(bool)])]
		public static void OpenFilePanelPostfix(ref string[] __result)
        {
			if (!ImagesSelectorOpen)
				return;
			ImagesSelectorOpen = false;
			AnimatedImage = null;
			if (__result.Length != 1)
				return;

			AnimatedImage = AnimatedImageUtils.GetAnimatedImage(__result[0]);
			if (AnimatedImage == null || (AnimatedImage is not GIFFile && !AnimatedImage.IsAnimated))
            {
				AnimatedImage?.Dispose();
				AnimatedImage = null;
                return;
            }
			ImagesSelectorOpen = true;
			ImagePath = Path.Combine(RDEditorUtils.GetCurrentLevelFolderPath(), Path.GetFileNameWithoutExtension(__result[0]));
        }

		[HarmonyPostfix]
        [HarmonyPatch(typeof(RDFile), nameof(RDFile.Exists))]
		public static void DirectToOverwritePostfix(ref bool __result)
        {
            if (!ImagesSelectorOpen)
				return;
			__result = true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(RDDialog), nameof(RDDialog.Show))]
		public static bool PreventCopyPrefix()
			=> !ImagesSelectorOpen; // if open => returns false, prevents function, so overwrite doesn't happen
    }

	[HarmonyPatch(typeof(PropertyControl_Image), nameof(PropertyControl_Image.Save))]
	private class FPSPatch
    {
        public static void Prefix(LevelEvent_Base levelEvent)
        {
			if (!SetFPS)
				return;
			// reflection isn't really needed...
            if (levelEvent is LevelEvent_MaskRoom mask)
				mask.fps = AverageFPS;
			if (levelEvent is LevelEvent_SetForeground foreground)
				foreground.fps = AverageFPS;
			if (levelEvent is LevelEvent_SetBackgroundColor background)
				background.fps = AverageFPS;

			levelEvent.inspectorPanel.properties.Find(prop => prop.propertyInfo.propertyInfo.Name == "fps").control.UpdateUI(levelEvent);
        }
    }

	[HarmonyPatch(typeof(InspectorPanel_MakeSprite), nameof(InspectorPanel_MakeSprite.LoadCustomCharacterFromPath))]
	private class SpritesheetPatch
    {
        public static void Prefix(ref string spritePath)
        {
			using IAnimatedImageFile? img = AnimatedImageUtils.GetAnimatedImage(spritePath);
			if (img == null || !img.IsAnimated)
				return;

			int width = 1;
			int height = 1;
			int padding = 12;
			int paddingCentre = padding >> 1;
			for (float possibleWidth = Mathf.Floor(Mathf.Sqrt(img.FrameCount)); possibleWidth <= img.FrameCount; possibleWidth++)
            {
				if (Mathf.Floor(img.FrameCount / possibleWidth) == (img.FrameCount / possibleWidth))
                {
					width = (int)possibleWidth;
					break;
                }
            }
			height = img.FrameCount / width;

			int paddingWidth = img.Width + padding;
			int paddingHeight = img.Height + padding;
			int totalWidth = (img.Width + padding) * width;
			int totalHeight = (img.Height + padding) * height;
			int frameArea = img.Width * img.Height;

			Texture2D spritesheet = new(totalWidth, totalHeight, CommonConstants.Format, false, true)
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			spritesheet.ClearTexture();

			NativeArray<uint> spritesheetData = spritesheet.GetPixelData<uint>(0);
			
			double totalDelay = 0d;
			string noExt = Path.Combine(RDEditorUtils.GetCurrentLevelFolderPath(), Path.GetFileNameWithoutExtension(spritePath));
			string path = $"{noExt}_spritesheet.png";
			string pathJson = $"{noExt}_spritesheet.json";

			string frames = "";
			for (int i = 0; i < img.FrameCount; i++)
            {
				OutputFrame output = img.GetFrame();
                Texture2D frame = output.Texture;
				NativeArray<uint> frameData = frame.GetPixelData<uint>(0);

				int column = i % width;
				int row = height - (i / width) - 1;
				for (int j = 0; j < frameArea; j++)
                {
                    int x = j % img.Width + column * paddingWidth + paddingCentre;
					int y = j / img.Width + row * paddingHeight + paddingCentre;
					spritesheetData[x + y * totalWidth] = frameData[j];
                }

				totalDelay += output.FrameDuration;
				Object.Destroy(frame);

				if (i != 0)
					frames += ", ";
				frames += i;
            }
			spritesheet.Apply(false, false);

			File.WriteAllBytes(path, ImageConversion.EncodeToPNG(spritesheet));

			char tab = '\t';
			File.WriteAllText(pathJson, 
			"{\n" + 
			tab + $"\"size\": [{paddingWidth}, {paddingHeight}],\n" +
			tab + $"\"clips\": [\n" +
			tab + tab + "{\"name\": \"neutral\", \"loop\": \"yes\", " + $"\"frames\": [{frames}], \"fps\": {(float)(img.FrameCount / totalDelay)} }}\n" +
			tab + "],\n" +
			tab + "\"meta\": \"Generated by RDModifications.ImportAnimatedImages\"\n" +
			"}");

			spritePath = pathJson;
			Object.Destroy(spritesheet);
        }
    }	
}
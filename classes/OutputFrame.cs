using Unity.Collections;
using UnityEngine;

namespace RDModifications;

public class OutputFrame
{
	public Texture2D Texture;
	public double FrameDuration;

	public OutputFrame(Texture2D texture, double frameDuration)
	{
		Texture = texture;
		FrameDuration = frameDuration;
	}

	public OutputFrame(ARGB32[] texture, int width, int height, double frameDuration)
    {
		Texture = new(width, height, CommonConstants.Format, false, true, true)
		{
			hideFlags = HideFlags.HideAndDontSave
		};
		FrameDuration = frameDuration;

		NativeArray<ARGB32> pixels = Texture.GetPixelData<ARGB32>(0);
		for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
				pixels[x + (height - y - 1) * width] = texture[x + y * width];

		Texture.Apply(false, false);
    }
}
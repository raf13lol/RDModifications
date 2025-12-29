using Unity.Collections;
using UnityEngine;

public static class Texture2DExtensions
{
    public static void CopyPixels(this Texture2D dest, Texture2D src, int srcX, int srcY, int srcWidth, int srcHeight, int destX, int destY, bool removeCPUCopy = false)
    {
        NativeArray<uint> srcPixels = src.GetPixelData<uint>(0);
        NativeArray<uint> destPixels = dest.GetPixelData<uint>(0);

        int srcTexWidth = src.width;
        int destTexWidth = dest.width;

        int pixelsToCopy = srcWidth * srcHeight;
        for (int i = 0; i < pixelsToCopy; i++)
        {
            int relativeX = i % srcWidth;    
            int relativeY = i / srcWidth; // int division automatically floors    
            
            int srcPixelIndex = srcX + relativeX + (srcY + relativeY) * srcTexWidth;
            int destPixelIndex = destX + relativeX + (destY + relativeY) * destTexWidth;

            destPixels[destPixelIndex] = srcPixels[srcPixelIndex];
        }

        dest.Apply(false, removeCPUCopy);
    }

	/// <summary>
	/// Requires the textures to be the same size
	/// </summary>
	public static void CopyPixelsQuick(this Texture2D dest, Texture2D src, int x, int y, int width, int height, bool removeCPUCopy = false)
    {
        NativeArray<uint> srcPixels = src.GetPixelData<uint>(0);
        NativeArray<uint> destPixels = dest.GetPixelData<uint>(0);

        int texWidth = src.width;
        int pixelsToCopy = width * height;
        for (int i = 0; i < pixelsToCopy; i++)
        {
            int relativeX = i % width;    
            int relativeY = i / width; // int division automatically floors    
            int pixelIndex = x + relativeX + (y + relativeY) * texWidth;

            destPixels[pixelIndex] = srcPixels[pixelIndex];
        }

        dest.Apply(false, removeCPUCopy);
    }

    public static void CopyPixelsRaw(this Texture2D dest, Texture2D src, bool removeCPUCopy = false)
    {
        dest.SetPixelData(src.GetPixelData<uint>(0), 0);
        dest.Apply(false, removeCPUCopy);
    }

    public static void MergePixels(this Texture2D dest, Texture2D src, int srcX, int srcY, int srcWidth, int srcHeight, int destX, int destY, bool removeCPUCopy = false)
    {
        NativeArray<ARGB32> srcPixels = src.GetPixelData<ARGB32>(0);
        NativeArray<ARGB32> destPixels = dest.GetPixelData<ARGB32>(0);

        int srcTexWidth = src.width;
        int destTexWidth = dest.width;

        int pixelsToCopy = srcWidth * srcHeight;
        for (int i = 0; i < pixelsToCopy; i++)
        {
            int relativeX = i % srcWidth;    
            int relativeY = i / srcWidth; // int division automatically floors    
            
            int srcPixelIndex = srcX + relativeX + (srcY + relativeY) * srcTexWidth;
            int destPixelIndex = destX + relativeX + (destY + relativeY) * destTexWidth;

            ARGB32 foreground = srcPixels[srcPixelIndex];
            if (foreground.a != 255 && foreground.a != 0)
            {
                ARGB32 background = destPixels[destPixelIndex];      
                double foregroundMult = foreground.a / 255d; 
                double backgroundMult = 1d - foregroundMult;

                destPixels[destPixelIndex] = new(
                    (byte)((double)foregroundMult * foreground.a + backgroundMult * background.a),
                    (byte)((double)foregroundMult * foreground.r + backgroundMult * background.r), 
                    (byte)((double)foregroundMult * foreground.g + backgroundMult * background.g), 
                    (byte)((double)foregroundMult * foreground.b + backgroundMult * background.b)
                );
                continue;
            }
            if (foreground.a == 0)
                continue;
            destPixels[destPixelIndex] = foreground;
        }

        dest.Apply(false, removeCPUCopy);
    }

    public static void ClearTexture(this Texture2D texture)
    {
        NativeArray<uint> pixels = texture.GetPixelData<uint>(0);

        int pixelBytes = texture.width * texture.height;
        for (int i = 0; i < pixelBytes; i++)
            pixels[i] = 0;

        texture.Apply(false, false);
    }

	public static void ClearTextureArea(this Texture2D texture, int x, int y, int width, int height, bool removeCPUCopy = false)
    {
        NativeArray<uint> pixels = texture.GetPixelData<uint>(0);

        int texWidth = texture.width;
        int pixelsToCopy = width * height;
        for (int i = 0; i < pixelsToCopy; i++)
        {
            int relativeX = i % width;    
            int relativeY = i / width; // int division automatically floors    
            int pixelIndex = x + relativeX + (y + relativeY) * texWidth;

			pixels[pixelIndex] = 0;
        }

        texture.Apply(false, removeCPUCopy);
    }
}
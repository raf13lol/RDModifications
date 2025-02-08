using System;
using Unity.Collections;
using UnityEngine;

public static class Extensions
{
    public static void CopyPixels(this Texture2D dest, Texture2D src, int srcX, int srcY, int srcWidth, int srcHeight, int destX, int destY, bool removeCPUCopy = false)
    {
        NativeArray<Color32> srcPixels = src.GetPixelData<Color32>(0);
        NativeArray<Color32> destPixels = dest.GetPixelData<Color32>(0);

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
}
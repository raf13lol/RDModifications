using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace RDModifications;

public class Tex2DUtils
{
    public static Texture2D CreateBlank(int width, int height, bool removeCPUCopy = false)
    {
        Texture2D tex = Create(width, height);
        tex.ClearTexture();
        tex.Apply(false, removeCPUCopy);
        return tex;
    }

    public static Texture2D Create(int width = 2, int height = 2, bool createUninitialised = true, FilterMode filter = FilterMode.Point)
        => new(width, height, CommonConstants.Format, false, true, createUninitialised)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = filter,
            wrapMode = TextureWrapMode.Clamp,
        };

    public static Texture2D LoadImage(byte[] imageData, bool removeCPUCopy = false, FilterMode filter = FilterMode.Point)
    {
        Texture2D tex = Create();
        tex.LoadImage(imageData, removeCPUCopy);
        tex.filterMode = filter;
        return tex;
    }

    public static Texture2D CopyCropped(Texture2D src, int x, int y, int width, int height, bool removeCPUCopy = true)
    {
        Texture2D tex = Create(width, height);
        tex.CopyPixels(src, x, y, width, height, 0, 0, removeCPUCopy);
        return tex;
    }

    public static Texture2D FilterPixels(Texture2D image, Color[] allowedColours, bool turnWhite = false)
            => FilterPixels(image, [.. allowedColours.Select(col => ARGB32.FromColor(col))], turnWhite);

    public static Texture2D FilterPixels(Texture2D image, ARGB32[] allowedColours, bool turnWhite = false)
    {
        Texture2D tex = Create(image.width, image.height, true, image.filterMode);
        int[] allowedCols = [.. allowedColours.Select(col => col.GetHashCode())];
        int allowedColCount = allowedCols.Length;

        NativeArray<int> srcPixels = image.GetPixelData<int>(0);
        NativeArray<int> destPixels = tex.GetPixelData<int>(0);

        int pixelsToTouch = image.width * image.height;
        for (int i = 0; i < pixelsToTouch; i++)
        {
            int srcCol = srcPixels[i];

            if (UnsafeArrayUtils.IndexOf(allowedCols, allowedColCount, srcCol) == -1)
                destPixels[i] = 0;
            else // -1 = 0xffffffff
                destPixels[i] = turnWhite ? -1 : srcCol;
        }

        tex.Apply(false, true);
        return tex;
    }

    /// <summary>
    /// destroys original texture btw
    /// </summary>
    public static Texture2D ConvertRGBA32ToARGB32(Texture2D tex, bool removeCPUCopy = true)
    {
        Texture2D converted = Create(tex.width, tex.height, true, tex.filterMode);

        NativeArray<uint> texData = tex.GetPixelData<uint>(0);
        NativeArray<uint> convertedData = converted.GetPixelData<uint>(0);
        int pixelsToTouch = tex.width * tex.height;

        for (int i = 0; i < pixelsToTouch; i++)
        {
            uint pixel = texData[i];
            uint alpha = (pixel & 0xff000000) >> 24;

            convertedData[i] = (pixel << 8) | alpha;
        }

        Object.Destroy(tex);
        converted.Apply(false, removeCPUCopy);
        return converted;
    }

    public static Texture2D CreateCPUCopyOfGPUTexture(Texture2D tex)
    {
        RenderTexture surf = RenderTexture.GetTemporary(tex.width, tex.height);
        Texture2D cpuCopy = Create(tex.width, tex.height, true, tex.filterMode);
    
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = surf;

        Graphics.Blit(tex, surf);

        cpuCopy.ReadPixels(new(0, 0, tex.width, tex.height), 0, 0);
        cpuCopy.Apply(false, false);

        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(surf);

        return cpuCopy;
    }
}
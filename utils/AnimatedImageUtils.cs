using System.IO;
using APNG;
using GIF;

namespace RDModifications;

public class AnimatedImageUtils
{
    public static IAnimatedImageFile? GetAnimatedImage(string path)
    {
        using FileStream file = File.OpenRead(path);
		int b1 = file.ReadByte();
		int b2 = file.ReadByte();
		int b3 = file.ReadByte();
		int b4 = file.ReadByte();
		file.Position = 0;

		bool isGIF = b1 == 0x47 && b2 == 0x49 && b3 == 0x46 && b4 == 0x38;
		bool isPNG = b1 == 0x89 && b2 == 0x50 && b3 == 0x4E && b4 == 0x47;
		if (!isGIF && !isPNG)
			return null;
		if (isGIF)
			return new GIFFile(file);
		return new APNGFile(file);
    }
}
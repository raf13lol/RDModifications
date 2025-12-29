using System.Collections.Generic;
using System.IO;

namespace GIF;

public class Frame
{
	public Image ParentImage;

	public int TopLeftX;
	public int TopLeftY;
	public int Width;
	public int Height;

	public bool Interlaced;
	public ARGB32[]? LocalColourTable;

	public int MinimumLZWCodeSize;
	public byte[] LZWCompressedData;

	public Frame(BinaryReader reader, Image image)
	{
		ParentImage = image;
		TopLeftX = reader.ReadUInt16();
		TopLeftY = reader.ReadUInt16();
		Width = reader.ReadUInt16();
		Height = reader.ReadUInt16();

		byte packedFields = reader.ReadByte();

		LocalColourTable = null;
		if ((packedFields & 0x80) == 0x80)
			LocalColourTable = GIFFile.ReadColourTable(reader, 1 << ((packedFields & 0x07) + 1));
		Interlaced = (packedFields & 0x40) == 0x40;

		MinimumLZWCodeSize = reader.ReadByte();
		LZWCompressedData = [];

		byte blockSize = reader.ReadByte();
		while (blockSize > 0)
		{
			LZWCompressedData = [.. LZWCompressedData, .. reader.ReadBytes(blockSize)];
			blockSize = reader.ReadByte();
		}
	}

	public ARGB32[] Decompress(ARGB32[] globalColourTable)
	{
		ARGB32[] colourTable = LocalColourTable ?? globalColourTable;
		List<ARGB32> colourData = new(Width * Height);
		// interlaced...
		for (int i = 0; i < Width * Height; i++)
			colourData.Add(new(0, 0, 0, 0));

		byte[] data = LZW.Decompress(LZWCompressedData, MinimumLZWCodeSize);
		int transparentColour = (ParentImage.Info == null || !ParentImage.Info.HasTransparentColour) ? 256 : ParentImage.Info.TransparentColourIndex;
		int index = 0;
		foreach (byte b in data)
		{
			int pos = index;
			if (Interlaced)
            {
				int x = index % Width;
				int yIndex = index / Width;
				int y = yIndex * (1 << 3);
				// i wonder if there's a math formula to help... hmmm
				for (int i = 3; i > 0; i--)
				{
					if (yIndex <= (Height >> i))
						break;
					y = (yIndex - (Height >> i) - 1) * (1 << i) + (1 << (i - 1));
				}
				pos = x + y * Width;
            }
			
			index++;
			if (b == transparentColour)
				continue;

			colourData[pos] = colourTable[b];
		}

		return [.. colourData];
	}
}
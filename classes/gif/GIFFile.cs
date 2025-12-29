using System.Collections.Generic;
using System.IO;
using System.Linq;
using RDModifications;

namespace GIF;

public class GIFFile : IAnimatedImageFile
{
    public bool IsValidImage { get => Images.Count > 0; }
    public bool IsAnimated { get => Images.Count > 1; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int FrameCount { get => Images.Count; }

    public ARGB32[]? GlobalColourTable;
    public byte BackgroundColourIndex;

    private List<Image> Images = [];
    private int ImageIndex = 0;

    private ARGB32[]? OutputBufferPrevious;
    private ARGB32[]? OutputBufferCurrent;

    public GIFFile(Stream data)
    {
        using BinaryReader reader = new(data);

        string version = new(reader.ReadChars(6));
        if (version != "GIF87a" && version != "GIF89a")
            return;

        Width = reader.ReadUInt16();
        Height = reader.ReadUInt16();

        OutputBufferCurrent = new ARGB32[Width * Height];

        byte packedFields = reader.ReadByte();
        BackgroundColourIndex = reader.ReadByte();
        reader.ReadByte(); // pixel aspect ratio

        GlobalColourTable = null;
        if ((packedFields & 0x80) == 0x80)
            GlobalColourTable = ReadColourTable(reader, 1 << ((packedFields & 0x07) + 1));

        FrameInfo? latestFrameInfo = null;
        byte sectionIdentifier = reader.ReadByte();
        while (sectionIdentifier != 0x3B)
        {
            if (sectionIdentifier == 0x2C)
            {
                if (Images.Count == 0)
                    Images.Add(new());
                Images.Last().AddFrame(reader);
            }
            else if (sectionIdentifier == 0x21)
            {
                byte extensionIdentifier = reader.ReadByte();
                byte length = reader.ReadByte();
                if (extensionIdentifier == 0xF9 && length == 0x04)
                {
                    latestFrameInfo = new(reader);
                    Images.Add(new(latestFrameInfo));
                }
                else
                    reader.BaseStream.Position += length + 1;
            }
            sectionIdentifier = reader.ReadByte();
        }

        if (IsAnimated && Images.Any(image => image.Info?.Disposal == DisposalMethod.RestoreToPrevious))
            OutputBufferPrevious = new ARGB32[Width * Height];
    }

    public OutputFrame GetFrame()
    {
        if (OutputBufferCurrent == null)
            return new(Images[0].Frames[0].Decompress(GlobalColourTable!), Width, Height, 0d);
		
        Image image = Images[ImageIndex++];
        foreach (Frame frame in image.Frames)
        {
            ARGB32[] imageData = frame.Decompress(GlobalColourTable!);
            int pixelCount = imageData.Length;
            for (int i = 0; i < pixelCount; i++)
            {
                if (imageData[i].a == 0)
                    continue;

                int bufferX = (i % frame.Width) + frame.TopLeftX;
                int bufferY = (i / frame.Width) + frame.TopLeftY;
                OutputBufferCurrent[bufferX + bufferY * Width] = imageData[i];
            }
        }
		
        OutputFrame output = new(OutputBufferCurrent!, Width, Height, !IsAnimated || image.Info == null ? 0 : image.Info!.Delay);
        if (image.Info == null || image.Info.Disposal <= DisposalMethod.DoNotDispose)
            goto Previous;

        if (image.Info.Disposal == DisposalMethod.RestoreToPrevious)
        {
            foreach (Frame frame in image.Frames)
            {
                int pixelCount = frame.Width * frame.Height;
                for (int i = 0; i < pixelCount; i++)
                {
                    int bufferX = (i % frame.Width) + frame.TopLeftX;
					int bufferY = (i / frame.Width) + frame.TopLeftY;
					OutputBufferCurrent[bufferX + bufferY * Width] = OutputBufferPrevious[bufferX + bufferY * Width];
                }
            }
            goto OutputReturn;
        }


        foreach (Frame frame in image.Frames)
        {
            int pixelCount = frame.Width * frame.Height;
            for (int i = 0; i < pixelCount; i++)
            {
                int bufferX = (i % frame.Width) + frame.TopLeftX;
                int bufferY = (i / frame.Width) + frame.TopLeftY;
                OutputBufferCurrent[bufferX + bufferY * Width] = GlobalColourTable[BackgroundColourIndex];
            }
        }

    Previous:
        if (ImageIndex == 0 || OutputBufferPrevious == null)
            goto OutputReturn;


        foreach (Frame frame in image.Frames)
        {
            int pixelCount = frame.Width * frame.Height;
            for (int i = 0; i < pixelCount; i++)
            {
                int bufferX = (i % frame.Width) + frame.TopLeftX;
                int bufferY = (i / frame.Width) + frame.TopLeftY;
                OutputBufferPrevious[bufferX + bufferY * Width] = OutputBufferCurrent[bufferX + bufferY * Width];
            }
        }

    OutputReturn:
        return output;
    }

	~GIFFile() => Dispose();

    public void Dispose()
    {
		GlobalColourTable = null;
		OutputBufferCurrent = null;
		OutputBufferPrevious = null;
		foreach (Image image in Images)
        {
            image.Frames = null;
            image.Info = null;
        }
		Images = null;
		System.GC.SuppressFinalize(this);
    }

    internal static ARGB32[] ReadColourTable(BinaryReader reader, int size = 256)
    {
        ARGB32[] colourTable = new ARGB32[size];
        for (int i = 0; i < size; i++)
        {
            // if (i == 255)
            //     colourTable[i] = new(0xff, 0xff, 0xff);
            // else
            //     colourTable[i] = new(0);
			byte r = reader.ReadByte();
			byte g = reader.ReadByte();
			byte b = reader.ReadByte();
            colourTable[i] = new(255, r, g, b);
        }
        return colourTable;
    }
}
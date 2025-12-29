using System.Collections.Generic;
using System.IO;

namespace APNG;

class Frame
{
    public int Width;
    public int Height;
    public int XOffset;
    public int YOffset;

    public double FrameDuration { get => (double)FrameNumerator / FrameDenominator; }
    public ushort FrameNumerator;
    public ushort FrameDenominator;

    public DisposeOutputBufferOperation DisposeOperation;
    public BlendOutputBufferOperation BlendOperation;

    public List<Chunk> ImageDataChunks;

    public Frame(APNGFile parent, Chunk frameInfoChunk, List<Chunk> imageChunks, bool staticIsAnimation = false)
    {
        using MemoryStream stream = new(frameInfoChunk.Data);
        using BinaryReader reader = new(stream);
        reader.SkipBytes(4); // skip sequence number

        if (staticIsAnimation)
        {
            Width = parent.Width;
            Height = parent.Height;
            XOffset = YOffset = 0;
            reader.SkipBytes(16);
        }
        else
        {
            Width = reader.ReadInt32BE();
            Height = reader.ReadInt32BE();
            XOffset = reader.ReadInt32BE();
            YOffset = reader.ReadInt32BE();
        }

        FrameNumerator = reader.ReadUInt16BE();
        FrameDenominator = reader.ReadUInt16BE();
        if (FrameDenominator == 0)
            FrameDenominator = 100;

        DisposeOperation = (DisposeOutputBufferOperation)reader.ReadByte();
        BlendOperation = (BlendOutputBufferOperation)reader.ReadByte();

        ImageDataChunks = imageChunks;
    }

    public byte[] ToImageBytes(IHDRChunk infoChunk, byte[] preIDATChunks, byte[] postIDATChunks)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        
        writer.Write(APNGFile.PNGSignature);

        infoChunk.Width = Width;
        infoChunk.Height = Height;
        infoChunk.WriteBytes(writer);

        writer.Write(preIDATChunks);
        foreach (Chunk chunk in ImageDataChunks)
            chunk.WriteBytes(writer);
        writer.Write(postIDATChunks);

        writer.Write(APNGFile.IENDChunk);
        return Utils.GetDataFromStream(stream);
    }
}
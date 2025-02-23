// I made this in a separate project and merged back here so you know this is some real shit

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace APNGP;

public class APNG : IDisposable
{
    public const TextureFormat Format = TextureFormat.ARGB32;
    public static readonly byte[] PNGSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    public static readonly byte[] IENDChunk = [0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82];

    public bool IsAnimated;
    public int Width;
    public int Height;
    public int FrameCount { get => frames.Count; }

    private readonly List<Frame> frames = [];

    private readonly byte[] preIDATChunksData = [];
    private readonly byte[] postIDATChunksData = [];
    private readonly IHDRChunk infoChunk = new();

    public APNG(Stream data)
    {
        using BinaryReader fileReader = new(data);
        if (!fileReader.CheckSignature(PNGSignature))
            return;
            
        infoChunk = new(fileReader); 
        if (infoChunk.name != "IHDR" || infoChunk.length != 13)
            return;

        Width = infoChunk.Width;
        Height = infoChunk.Height;

        Chunk frameInfoChunk = new();
        List<Chunk> frameDataChunks = [];

        bool foundFirstDataChunk = false;
        bool staticIsAnimation = false;
        bool endOfChunks = false;

        List<Chunk> preIDATChunks = [];
        List<Chunk> postIDATChunks = [];
        while (!endOfChunks)
        {
            Chunk chunk = new(fileReader);
            switch (chunk.name)
            {
                default:
                    if (!foundFirstDataChunk)
                        preIDATChunks.Add(chunk);
                    else
                        postIDATChunks.Add(chunk);
                    break;

                case "acTL":
                    IsAnimated = true;
                    break;

                case "fcTL":
                    if (frameDataChunks.Count > 0)
                    {
                        frames.Add(new(this, frameInfoChunk, frameDataChunks, staticIsAnimation));
                        frameDataChunks = [];
                        staticIsAnimation = false;
                    }
                    frameInfoChunk = chunk;
                    break;

                case "IDAT":
                    if (!IsAnimated)
                        return;
                    if (frameInfoChunk.name != "EMPTY")
                    {
                        frameDataChunks.Add(chunk);
                        if (!foundFirstDataChunk)
                            staticIsAnimation = true;
                    }
                    foundFirstDataChunk = true;
                    break;

                case "IEND":
                    if (frameDataChunks.Count > 0)
                    {
                        frames.Add(new(this, frameInfoChunk, frameDataChunks));
                        frameDataChunks = [];
                    }
                    endOfChunks = true;
                    break;
            }
        }
        preIDATChunksData = Utils.GetDataFromChunkList(preIDATChunks);
        postIDATChunksData = Utils.GetDataFromChunkList(postIDATChunks);

        emptyTexture = new(Width, Height, Format, false, true, false);
        emptyTexture.ClearTexture();

        outputBufferCurrent = new(Width, Height, Format, false, true, false);
        outputBufferPrevious = new(Width, Height, Format, false, true, false);

        outputBufferCurrent.CopyPixelsRaw(emptyTexture);
        outputBufferPrevious.CopyPixelsRaw(emptyTexture);
    }

    private int currentFrame;
    private readonly Texture2D outputBufferCurrent;
    private readonly Texture2D outputBufferPrevious;
    private readonly Texture2D emptyTexture;

    public OutputFrame GetFrame()
    {
        Frame frame = frames[currentFrame++];

        // load the frame image
        Texture2D frameImage = new(2, 2, Format, false, true, true);
        byte[] data = frame.ToImageBytes(infoChunk, preIDATChunksData, postIDATChunksData);
        frameImage.LoadImage(data);

        // merge the frames
        int yOffset = Height - (frame.Height + frame.YOffset); // unity y starts at the bottom and goes to the top
        if (frame.BlendOperation == BlendOutputBufferOperation.APNG_BLEND_OP_SOURCE)
            outputBufferCurrent.CopyPixels(frameImage, 0, 0, frame.Width, frame.Height, frame.XOffset, yOffset);
        else
            outputBufferCurrent.MergePixels(frameImage, 0, 0, frame.Width, frame.Height, frame.XOffset, yOffset);

        // render the frame
        Texture2D output = new(Width, Height, Format, false, true, true);
        output.CopyPixelsRaw(outputBufferCurrent, false);
        
        // Disposal of the frame after it's rendered
        if (frame.DisposeOperation == DisposeOutputBufferOperation.APNG_DISPOSE_OP_PREVIOUS)
            outputBufferCurrent.CopyPixelsEqual(outputBufferPrevious, frame.XOffset, yOffset, frame.Width, frame.Height);
        else
            outputBufferPrevious.CopyPixelsRaw(outputBufferCurrent);

        if (frame.DisposeOperation == DisposeOutputBufferOperation.APNG_DISPOSE_OP_BACKGROUND)
            outputBufferCurrent.CopyPixelsEqual(emptyTexture, frame.XOffset, yOffset, frame.Width, frame.Height);

        // and then return
        return new(output, frame.FrameDuration);
    }

    ~APNG() => Dispose();

    public void Dispose()
    {
        UnityEngine.Object.Destroy(outputBufferCurrent);
        UnityEngine.Object.Destroy(outputBufferPrevious);
        UnityEngine.Object.Destroy(emptyTexture);
        System.GC.SuppressFinalize(this);
    }
}
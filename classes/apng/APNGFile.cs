// I made this in a separate project and merged back here so you know this is some real shit

using System.Collections.Generic;
using System.IO;
using RDModifications;
using UnityEngine;

namespace APNG;

public class APNGFile : IAnimatedImageFile
{
    public static readonly byte[] PNGSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    public static readonly byte[] IENDChunk = [0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82];

	public bool IsValidImage { get; set; } 
    public bool IsAnimated { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int FrameCount { get => Frames.Count; }

    private readonly List<Frame> Frames = [];

    private readonly byte[] PreIDATChunksData = [];
    private readonly byte[] PostIDATChunksData = [];
    private readonly IHDRChunk InfoChunk = new();

	private readonly Texture2D NonAnimatedImage;

    public APNGFile(Stream data)
    {
        using BinaryReader fileReader = new(data);
        if (!fileReader.CheckSignature(PNGSignature))
            return;
            
        InfoChunk = new(fileReader); 
        if (InfoChunk.Name != "IHDR" || InfoChunk.Length != 13)
            return;

        Width = InfoChunk.Width;
        Height = InfoChunk.Height;

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
            switch (chunk.Name)
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
                        Frames.Add(new(this, frameInfoChunk, frameDataChunks, staticIsAnimation));
                        frameDataChunks = [];
                        staticIsAnimation = false;
                    }
                    frameInfoChunk = chunk;
                    break;

                case "IDAT":
                    if (!IsAnimated)
					{
						Texture2D image = new(2, 2, CommonConstants.Format, false, true, true)
						{
							hideFlags = HideFlags.HideAndDontSave
						};
						fileReader.BaseStream.Position = 0;
						image.LoadImage(fileReader.ReadBytes((int)fileReader.BaseStream.Length));
						NonAnimatedImage = image;
						IsValidImage = true;
						IsAnimated = false;
                        return;
					}
					if (frameInfoChunk.Name != "EMPTY")
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
                        Frames.Add(new(this, frameInfoChunk, frameDataChunks));
                        frameDataChunks = [];
                    }
                    endOfChunks = true;
                    break;
            }
        }

		IsValidImage = true;

        PreIDATChunksData = Utils.GetDataFromChunkList(preIDATChunks);
        PostIDATChunksData = Utils.GetDataFromChunkList(postIDATChunks);

        OutputBufferCurrent = new(Width, Height, CommonConstants.Format, false, true, false)
		{
			hideFlags = HideFlags.HideAndDontSave
		};
        OutputBufferPrevious = new(Width, Height, CommonConstants.Format, false, true, false)
		{
			hideFlags = HideFlags.HideAndDontSave
		};
    }

    private int CurrentFrame;
    private readonly Texture2D OutputBufferCurrent;
    private readonly Texture2D OutputBufferPrevious;

    public OutputFrame GetFrame()
    {
		if (!IsAnimated)
			return new(NonAnimatedImage, 0);
        Frame frame = Frames[CurrentFrame++];

        // load the frame image
        Texture2D frameImage = new(2, 2, CommonConstants.Format, false, true, true)
		{
			hideFlags = HideFlags.HideAndDontSave
		};
        byte[] data = frame.ToImageBytes(InfoChunk, PreIDATChunksData, PostIDATChunksData);
        frameImage.LoadImage(data);

        // merge the frames
        int yOffset = Height - (frame.Height + frame.YOffset); // unity y starts at the bottom and goes to the top
        if (frame.BlendOperation == BlendOutputBufferOperation.Source)
            OutputBufferCurrent.CopyPixels(frameImage, 0, 0, frame.Width, frame.Height, frame.XOffset, yOffset);
        else
            OutputBufferCurrent.MergePixels(frameImage, 0, 0, frame.Width, frame.Height, frame.XOffset, yOffset);

		Object.Destroy(frameImage);
        // render the frame
        Texture2D output = new(Width, Height, CommonConstants.Format, false, true, true)
		{
			hideFlags = HideFlags.HideAndDontSave
		};
        output.CopyPixelsRaw(OutputBufferCurrent, false);
        
        // Disposal of the frame after it's rendered
        if (frame.DisposeOperation == DisposeOutputBufferOperation.Previous)
            OutputBufferCurrent.CopyPixelsQuick(OutputBufferPrevious, frame.XOffset, yOffset, frame.Width, frame.Height);
        else
            OutputBufferPrevious.CopyPixelsRaw(OutputBufferCurrent);

        if (frame.DisposeOperation == DisposeOutputBufferOperation.Background)
            OutputBufferCurrent.ClearTextureArea(frame.XOffset, yOffset, frame.Width, frame.Height);

        // and then return
        return new(output, frame.FrameDuration);
    }

    ~APNGFile() => Dispose();

    public void Dispose()
    {
		if (!IsAnimated)
			Object.Destroy(NonAnimatedImage);
		Object.Destroy(OutputBufferCurrent);
		Object.Destroy(OutputBufferPrevious);
        System.GC.SuppressFinalize(this);
    }
}
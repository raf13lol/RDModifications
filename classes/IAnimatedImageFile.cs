using System;

namespace RDModifications;

public interface IAnimatedImageFile : IDisposable
{
	public bool IsValidImage { get; }
	public bool IsAnimated { get; }
	public int Width { get; }
	public int Height { get; }
	public int FrameCount { get; }

	public OutputFrame GetFrame();
}
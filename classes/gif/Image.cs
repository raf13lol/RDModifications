using System.Collections.Generic;
using System.IO;

namespace GIF;

public class Image(FrameInfo? Info = null)
{
    public FrameInfo? Info = Info;
    public List<Frame> Frames = [];

    public void AddFrame(BinaryReader reader)
        => Frames.Add(new(reader, this));
}
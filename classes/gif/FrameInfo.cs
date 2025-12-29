using System.IO;

namespace GIF;

public class FrameInfo
{
    public int DelayHundredthsSecond;
    public double Delay => DelayHundredthsSecond / 100d; 

    public bool HasTransparentColour;
    public int TransparentColourIndex;

    public DisposalMethod Disposal;

    public FrameInfo(BinaryReader reader)
    {
        byte packedFields = reader.ReadByte();
        HasTransparentColour = (packedFields & 0x01) == 0x01;
        Disposal = (DisposalMethod)((packedFields >> 2) & 0x07);

        DelayHundredthsSecond = reader.ReadUInt16(); 
        TransparentColourIndex = reader.ReadByte();

        reader.ReadByte(); // end block terminator
    }
}
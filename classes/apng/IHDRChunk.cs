using System.IO;

namespace APNG;

class IHDRChunk(BinaryReader? reader = null) : Chunk(reader)
{
    public int Width 
    { 
        get
        {
            int val = GetInt32(0);
            width = val;
            return val;
        }
        set
        {
            if (width != value)
                SetInt32(0, value);
            width = value;
        }
    }
    public int Height 
    {
        get
        {
            int val = GetInt32(4);
            height = val;
            return val;
        }
        set
        {
            if (height != value)
                SetInt32(4, value);
            height = value;
        }
    }

    private int width;
    private int height;

    private int GetInt32(int pos, byte[]? readFrom = null)
    {
        readFrom ??= Data;

        return (readFrom[pos] << 24) 
        | (readFrom[pos + 1] << 16) 
        | (readFrom[pos + 2] << 8) 
        | readFrom[pos + 3]; 
    }

    private void SetInt32(int pos, int value, byte[]? writeInto = null)
    {
        writeInto ??= Data;
        // simple i guess
        writeInto[pos] = (byte)((value >> 24) & 0xff);
        writeInto[pos + 1] = (byte)((value >> 16) & 0xff);
        writeInto[pos + 2] = (byte)((value >> 8) & 0xff);
        writeInto[pos + 3] = (byte)(value & 0xff);

        CrcDirty = true;
    }
}
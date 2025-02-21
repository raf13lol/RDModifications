using System.IO;

namespace APNGP;

class IHDRChunk(BinaryReader? reader = null) : Chunk(reader)
{
    public int Width 
    { 
        get
        {
            int val = GetInt32(0);
            _width = val;
            return val;
        }
        set
        {
            if (_width != value)
                SetInt32(0, value);
            _width = value;
        }
    }
    public int Height 
    {
        get
        {
            int val = GetInt32(4);
            _height = val;
            return val;
        }
        set
        {
            if (_height != value)
                SetInt32(4, value);
            _height = value;
        }
    }

    private int _width;
    private int _height;

    private int GetInt32(int pos, byte[]? readFrom = null)
    {
        readFrom ??= data;

        return (readFrom[pos] << 24) 
        | (readFrom[pos + 1] << 16) 
        | (readFrom[pos + 2] << 8) 
        | (readFrom[pos + 3]); 
    }

    private void SetInt32(int pos, int value, byte[]? writeInto = null)
    {
        writeInto ??= data;
        // simple i guess
        writeInto[pos] = (byte)((value >> 24) & 0xff);
        writeInto[pos + 1] = (byte)((value >> 16) & 0xff);
        writeInto[pos + 2] = (byte)((value >> 8) & 0xff);
        writeInto[pos + 3] = (byte)(value & 0xff);

        crcDirty = true;
    }
}
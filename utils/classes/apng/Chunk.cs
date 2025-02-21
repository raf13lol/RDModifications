using System.IO;

namespace APNGP;

class Chunk
{
    public int length;
    public string name = "";
    public byte[] data = [];

    public uint crc;
    public bool crcDirty;

    public Chunk(BinaryReader? reader = null)
    {
        if (reader == null)
            return;

        length = reader.ReadInt32BE();
        name = string.Join("", reader.ReadChars(4));

        // animation data chunk is literally just a data chunk with something extra at the start
        if (name == "fdAT")
        {
            name = "IDAT";
            length -= 4;
            reader.SkipBytes(4); // skip sequence number
            crcDirty = true; // this does affect the crc
        }

        data = reader.ReadBytes(length);
        crc = reader.ReadUInt32BE(); // CRC32
    }

    public Chunk(int length, string name, byte[] data)
    {
        this.length = length;
        this.name = name;
        this.data = data;
    }

    public Chunk Clone()
        => new(length, name, data);

    public void WriteBytes(BinaryWriter writer)
    {
        writer.WriteBE(length);
    
        byte[] nameAndDataBytes = [0x00, 0x00, 0x00, 0x00, .. data];
        for (int i = 0; i < name.Length; i++)
            nameAndDataBytes[i] = (byte)name[i];

        writer.Write(nameAndDataBytes);
        if (crcDirty)
            crc = Crc32.Hash(nameAndDataBytes);
        writer.WriteBE(crc);
    }
}
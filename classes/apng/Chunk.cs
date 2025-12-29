using System.IO;

namespace APNG;

class Chunk
{
    public int Length;
    public string Name = "";
    public byte[] Data = [];

    public uint Crc;
    public bool CrcDirty;

    public Chunk(BinaryReader? reader = null)
    {
        if (reader == null)
            return;

        Length = reader.ReadInt32BE();
        Name = string.Join("", reader.ReadChars(4));

        // animation data chunk is literally just a data chunk with something extra at the start
        if (Name == "fdAT")
        {
            Name = "IDAT";
            Length -= 4;
            reader.SkipBytes(4); // skip sequence number
            CrcDirty = true; // this does affect the crc
        }

        Data = reader.ReadBytes(Length);
        Crc = reader.ReadUInt32BE(); // CRC32
    }

    public Chunk(int length, string name, byte[] data)
    {
        this.Length = length;
        this.Name = name;
        this.Data = data;
    }

    public Chunk Clone()
        => new(Length, Name, Data);

    public void WriteBytes(BinaryWriter writer)
    {
        writer.WriteBE(Length);
    
        byte[] nameAndDataBytes = [0x00, 0x00, 0x00, 0x00, .. Data];
        for (int i = 0; i < Name.Length; i++)
            nameAndDataBytes[i] = (byte)Name[i];

        writer.Write(nameAndDataBytes);
        if (CrcDirty)
            Crc = Crc32.Hash(nameAndDataBytes);
        writer.WriteBE(Crc);
    }
}
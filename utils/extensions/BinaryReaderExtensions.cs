using System;
using System.IO;
using System.Buffers.Binary;

public static class BinaryReaderExtensions
{
    public static bool CheckSignature(this BinaryReader reader, byte[] signature)
    {
        byte[] dataSignature = reader.ReadBytes(signature.Length);
        
        for (int i = 0; i < signature.Length; i++)
            if (dataSignature[i] != signature[i])
                return false;
        
        return true;
    }

    public static ReadOnlySpan<byte> InternalRead(BinaryReader reader, int byteAmount)
        => new(reader.ReadBytes(byteAmount));

    // BIG ENDIAN
    public static int ReadInt32BE(this BinaryReader reader) => BinaryPrimitives.ReadInt32BigEndian(InternalRead(reader, sizeof(int)));
    public static uint ReadUInt32BE(this BinaryReader reader) => BinaryPrimitives.ReadUInt32BigEndian(InternalRead(reader, sizeof(int)));
    public static ushort ReadUInt16BE(this BinaryReader reader) => BinaryPrimitives.ReadUInt16BigEndian(InternalRead(reader, sizeof(ushort)));
    public static void SkipBytes(this BinaryReader reader, int bytesToSkip) => reader.BaseStream.Position += bytesToSkip;

    public static void WriteBE(this BinaryWriter writer, int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        writer.BaseStream.Write(buffer);
    }

    public static void WriteBE(this BinaryWriter writer, uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        writer.BaseStream.Write(buffer);
    }
}
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace APNGP;

class Utils
{
    public static byte[] GetDataFromStream(MemoryStream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);

        byte[] streamOutput = new byte[stream.Length];
        stream.Read(streamOutput, 0, streamOutput.Length);
        return streamOutput;
    }

    // ONLY USE IF NO PRE-EXISTING STREAM
    public static byte[] GetDataFromChunkList(List<Chunk> chunks)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        foreach (Chunk chunk in chunks)
            chunk.WriteBytes(writer);
            
        return GetDataFromStream(stream);
    }
}
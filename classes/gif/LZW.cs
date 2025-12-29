using System.Collections.Generic;

namespace GIF;

public class LZW
{
    public const int MaxDictionarySize = 1 << 12;

    public static byte[] Decompress(byte[] compressedData, int minimumCodeWidth)
    {
        List<byte> decompressedData = [];
        byte[][] dictionary = new byte[MaxDictionarySize][];
        int currentCodeWidth = minimumCodeWidth;
        int clearCode = 1 << minimumCodeWidth;
        int stopCode = clearCode + 1;

        for (int i = 0; i < clearCode; i++)
            dictionary[i] = [(byte)i];

        dictionary[clearCode] = [0]; // clear code
        dictionary[stopCode] = [0]; // stop code
        int dictionaryUsedCount = stopCode + 1;

        currentCodeWidth++;
        int currentCodeWidthMaxSize = 1 << currentCodeWidth;
        int bitMask = currentCodeWidthMaxSize - 1;
        int bits = 0;
        uint data = 0;

        int index = 0;
        byte[] previousDataEmitted = [];

        while (index < compressedData.Length || bits >= currentCodeWidth)
        {
            while (bits < currentCodeWidth)
            {
                data |= (uint)(compressedData[index++] << bits);
                bits += 8;
            }
            int code = (int)(data & bitMask);
            data >>= currentCodeWidth;
            bits -= currentCodeWidth;

            if (code == stopCode)
                break;
            if (code == clearCode)
            {
                currentCodeWidth = minimumCodeWidth + 1;
                currentCodeWidthMaxSize = 1 << currentCodeWidth;
                bitMask = currentCodeWidthMaxSize - 1;
                dictionaryUsedCount = stopCode + 1;
                previousDataEmitted = [];
                continue;
            }
            if (previousDataEmitted.Length == 0)
            {
                decompressedData.Add((byte)code);
                previousDataEmitted = [(byte)code];
                continue;
            }

            if (code == dictionaryUsedCount)
            {
                byte[] sequence = [.. previousDataEmitted, previousDataEmitted[0]];
                dictionary[dictionaryUsedCount++] = sequence;

                foreach (byte b in sequence)
                    decompressedData.Add(b);
                previousDataEmitted = sequence;
            }
            else
            {
                byte[] dictSequence = dictionary[code];
                dictionary[dictionaryUsedCount++] = [.. previousDataEmitted, dictSequence[0]];

                foreach (byte b in dictSequence)
                    decompressedData.Add(b);
                previousDataEmitted = dictSequence; 
            }

            if (dictionaryUsedCount < currentCodeWidthMaxSize || currentCodeWidth == 12)
                continue;
            
            currentCodeWidth++;
            currentCodeWidthMaxSize = 1 << currentCodeWidth;
            bitMask = currentCodeWidthMaxSize - 1;
        }

        return [.. decompressedData];
    }
}
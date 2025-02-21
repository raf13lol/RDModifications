
using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

[StructLayout(LayoutKind.Explicit)]
public struct Color32ARGB : IFormattable
{
    [FieldOffset(0)]
    [Ignore(DoesNotContributeToSize = true)]
    private int argb;

    [FieldOffset(0)]
    public byte a;

    [FieldOffset(1)]
    public byte r;

    [FieldOffset(2)]
    public byte g;

    [FieldOffset(3)]
    public byte b;

    public byte this[int index]
    {
        get
        {
            return index switch
            {
                0 => a,
                1 => r,
                2 => g,
                3 => b,
                _ => throw new IndexOutOfRangeException("Invalid Color32 index(" + index + ")!"),
            };
        }
        set
        {
            switch (index)
            {
                case 0:
                    a = value;
                    break;
                case 1:
                    r = value;
                    break;
                case 2:
                    g = value;
                    break;
                case 3:
                    b = value;
                    break;
                default:
                    throw new IndexOutOfRangeException("Invalid Color32 index(" + index + ")!");
            }
        }
    }

    public Color32ARGB(byte a, byte r, byte g, byte b)
    {
        argb = 0;
        this.a = a;
        this.r = r;
        this.g = g;
        this.b = b;
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
        throw new NotImplementedException();
    }
}
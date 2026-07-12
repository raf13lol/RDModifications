using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Explicit)]
public struct ARGB32(byte a, byte r, byte g, byte b)
{
    [FieldOffset(0)]
    public byte a = a;

    [FieldOffset(1)]
    public byte r = r;

    [FieldOffset(2)]
    public byte g = g;

    [FieldOffset(3)]
    public byte b = b;

    public static ARGB32 FromColor(Color col)
        => new(
                (byte)Mathf.RoundToInt(col.a * 255f),
                (byte)Mathf.RoundToInt(col.r * 255f),
                (byte)Mathf.RoundToInt(col.g * 255f),
                (byte)Mathf.RoundToInt(col.b * 255f)
            );
        
    public static bool operator ==(ARGB32 a, ARGB32 b)
        => a.a == b.a && a.r == b.r && a.g == b.g && a.b == b.b;

    public static bool operator !=(ARGB32 a, ARGB32 b)
        => a.a != b.a || a.r != b.r || a.g != b.g || a.b != b.b;

    public override readonly int GetHashCode()
        => (b << 24) | (g << 16) | (r << 8) | a;

    public override readonly bool Equals(object obj)
        => (obj is ARGB32 other) && this == other;

    public override readonly string ToString()
        => $"#{a:x2}{r:x2}{g:x2}{b:x2}";
}
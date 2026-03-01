using System.Runtime.InteropServices;

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
}
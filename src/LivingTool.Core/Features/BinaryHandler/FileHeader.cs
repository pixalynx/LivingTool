using System.Runtime.InteropServices;

namespace LivingTool.Core.Features.BinaryHandler;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FileHeader
{
    public uint Pointer0;
    public uint Pointer1;
    public uint Pointer2;
    public uint Pointer3;
    public uint Pointer4;
    public uint Pointer5;
    public uint Pointer6;
    public uint Pointer7;

    public uint[] ToArray() => new[] {
        Pointer0, Pointer1, Pointer2, Pointer3,
        Pointer4, Pointer5, Pointer6, Pointer7
    };
}
using System.Runtime.InteropServices;

namespace LivingTool.Core.Features.BinaryHandler;

public class BinaryFileService : IBinaryFileService
{
    public FileHeader ReadFileHeader(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new BinaryReader(stream);

        byte[] buffer = reader.ReadBytes(Marshal.SizeOf<FileHeader>());
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        FileHeader header = Marshal.PtrToStructure<FileHeader>(handle.AddrOfPinnedObject());
        handle.Free();
        return header;
    }

    public byte[] ReadBytes(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new BinaryReader(stream);
        byte[] buffer = reader.ReadBytes((int)stream.Length);
        return buffer;
    }
}
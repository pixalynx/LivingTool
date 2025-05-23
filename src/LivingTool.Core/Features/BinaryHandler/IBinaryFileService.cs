namespace LivingTool.Core.Features.BinaryHandler;

public interface IBinaryFileService
{
    FileHeader ReadFileHeader(string filePath);
    byte[] ReadBytes(string filePath);
}
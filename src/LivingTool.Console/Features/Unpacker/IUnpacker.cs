namespace LivingTool.Console.Features.Unpacker;

public interface IUnpacker
{
    Task Unpack(string filePath, string outputDirectory, string locSectorsFile);
    Task UnpackLocSectors(string filePath, string outputFileName);
    Task Repack();
}
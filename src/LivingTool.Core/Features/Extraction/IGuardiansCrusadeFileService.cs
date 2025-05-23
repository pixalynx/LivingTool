namespace LivingTool.Core.Features.Extraction;

public interface IGuardiansCrusadeFileService
{
    byte[] ExtractFile(string filePath, int sectorNumber, int sectorSize);
    byte[] ExtractSectionFromFile(string filePath, int startOffset, int endOffset);
    void CreateFolders(string filePath);
    Task WriteFileAsync(string filePath, byte[] contents);
    IEnumerable<(int SectorNumber, int SizeInSectors)> ReadLocSectors(string filePath);
    string GetFolderNameFromSector(int sectorNumber, List<KeyValuePair<string, int>> sortedFolders);
}
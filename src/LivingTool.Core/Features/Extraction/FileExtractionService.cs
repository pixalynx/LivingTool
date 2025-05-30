using System.Text;

namespace LivingTool.Core.Features.Extraction;

public class FileExtractionService : IFileExtractionService
{
    public byte[] ExtractFile(string filePath, int sectorNumber, int lengthInSectors)
    {
        long startOffset = sectorNumber * SectorConstants.SectorSize;
        byte[] contents = new byte[lengthInSectors * SectorConstants.SectorSize];
        int mSectorNumber = FolderConstants.Folders["M"];

        using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
        fs.Seek(startOffset, SeekOrigin.Begin);
        for (int i = 0; i < lengthInSectors; i++)
        {
            if (sectorNumber >= mSectorNumber)
            {
                // Skip STR header
                fs.Seek(SectorConstants.StrHeaderSize, SeekOrigin.Current);

                // ReadBytes STR content
                byte[] buffer = new byte[SectorConstants.StrContentSize];
                int bytesRead = fs.Read(buffer, 0, SectorConstants.StrContentSize);

                if (bytesRead != SectorConstants.StrContentSize)
                {
                    throw new IOException($"Failed to read STR content. Expected {SectorConstants.StrContentSize} bytes, but got {bytesRead}.");
                }

                Buffer.BlockCopy(buffer, 0, contents, i * SectorConstants.StrContentSize, SectorConstants.StrContentSize);
            }

            else
            {
                // Skip header
                fs.Seek(SectorConstants.HeaderSize, SeekOrigin.Current);

                // ReadBytes content
                byte[] buffer = new byte[SectorConstants.ContentSize];
                int bytesRead = fs.Read(buffer, 0, SectorConstants.ContentSize);

                if (bytesRead != SectorConstants.ContentSize)
                {
                    throw new IOException($"Failed to read content. Expected {SectorConstants.ContentSize} bytes, but got {bytesRead}.");
                }

                // Skip footer
                fs.Seek(SectorConstants.FooterSize, SeekOrigin.Current);

                Buffer.BlockCopy(buffer, 0, contents, i * SectorConstants.ContentSize, SectorConstants.ContentSize);
            }
        }

        return contents;
    }

    public byte[] ExtractSectionFromFile(string filePath, int startOffset, int endOffset)
    {
        // Extract a section from the file
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        fs.Seek(startOffset, SeekOrigin.Begin);
        var length = endOffset - startOffset;
        var buffer = new byte[length];
        fs.ReadExactly(buffer, 0, length);
        return buffer;
    }

    public void CreateFolders(string filePath)
    {
        var sortedFolders = FolderConstants.Folders.OrderBy(kv => kv.Value).ToList();
        var seen = new HashSet<(int, int)>();

        foreach (var folder in FolderConstants.Folders.Keys)
        {
            Directory.CreateDirectory($"{filePath}/{folder}");
        }
    }

    public async Task WriteFileAsync(string filePath, byte[] contents)
    {
        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await fs.WriteAsync(contents, 0, contents.Length);
    }

    public IEnumerable<(int SectorNumber, int SizeInSectors)> ReadLocSectors(string filePath)
    {
        const int entrySize = 8;
        var buffer = new byte[entrySize];

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        while (stream.Read(buffer, 0, entrySize) == entrySize)
        {
            int minute = DecodeBcd(buffer[0]);
            int second = DecodeBcd(buffer[1]);
            int sector = DecodeBcd(buffer[2]);

            int sizeInSectors = BitConverter.ToInt32(buffer, 4);
            int sectorNumber = (minute * 60 + second) * 75 + sector - 150;

            yield return (sectorNumber, sizeInSectors);
        }
    }

    public string GetFolderNameFromSector(int sectorNumber, List<KeyValuePair<string, int>> sortedFolders)
    {
        for (int i = 0; i < sortedFolders.Count - 1; i++)
        {
            int currentStart = sortedFolders[i].Value;
            int nextStart = sortedFolders[i + 1].Value;

            if (sectorNumber >= currentStart && sectorNumber < nextStart)
            {
                return sortedFolders[i].Key;
            }
        }

        return sortedFolders.Last().Key;
    }

    private int DecodeBcd(byte value)
    {
        return ((value & 0xF0) >> 4) * 10 + (value & 0x0F);
    }
}
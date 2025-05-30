using LivingTool.Core.Features.Extraction;
using Spectre.Console;

namespace LivingTool.Console.Features.Unpacker;

public class GameUnpacker(IFileExtractionService fileExtractionService) : IUnpacker
{
    public async Task Unpack(string filePath, string outputDirectory, string locSectorsFile)
    {
        // Create the folders if they don't exist
        if (!Directory.Exists(outputDirectory))
        {
            AnsiConsole.WriteLine($"Creating output directory: {outputDirectory}");
            Directory.CreateDirectory(outputDirectory);
        }

        // Create the folders for each sector
        AnsiConsole.WriteLine($"Creating folders in output directory: {outputDirectory}");
        fileExtractionService.CreateFolders(outputDirectory);

        // Sorted folders
        var sortedFolders = FolderConstants.Folders
            .OrderBy(kv => kv.Value)
            .ToList();

        var seenEntries = new HashSet<(int Sector, int Size)>();
        int index = 1;

        // ReadBytes the LOC sectors
        foreach (var (sectorNumber, sizeInSectors) in fileExtractionService.ReadLocSectors(locSectorsFile))
        {
            // Check if the entry has already been seen
            if (seenEntries.Contains((sectorNumber, sizeInSectors)))
            {
                AnsiConsole.WriteLine($"Skipping already seen entry: {sectorNumber}");
                continue;
            }

            // Extract the file
            AnsiConsole.WriteLine($"Extracting file from sector {sectorNumber} with size {sizeInSectors}");
            var contents = fileExtractionService.ExtractFile(filePath, sectorNumber, sizeInSectors);

            // Get the folder name from the sector number
            string folderName = fileExtractionService.GetFolderNameFromSector(sectorNumber, sortedFolders);
            string outputFilePath = Path.Combine(outputDirectory, folderName, $"{index:D3}.bin");

            // Write the file to disk
            AnsiConsole.WriteLine($"Writing file to {outputFilePath}");
            await fileExtractionService.WriteFileAsync(outputFilePath, contents);

            seenEntries.Add((sectorNumber, sizeInSectors));
            index++;
        }

        // Rename files in each folder
        AnsiConsole.WriteLine($"Renaming files in output directory: {outputDirectory}");
        RenameFiles(outputDirectory);

        // Report gaps in the sectors
        AnsiConsole.WriteLine("Reporting gaps in sectors");
        ReportSectorGaps(seenEntries);

        // Report the number of files extracted
        AnsiConsole.WriteLine($"Extracted {seenEntries.Count} files");
        AnsiConsole.WriteLine("Unpacking completed successfully.");
    }

    public async Task UnpackLocSectors(string filePath, string outputFileName)
    {
        AnsiConsole.WriteLine($"Unpacking LOC sectors from {filePath} to {outputFileName}");
        byte[] contents = fileExtractionService.ExtractSectionFromFile(filePath, LocSectorConstants.StartOffset, LocSectorConstants.EndOffset);
        AnsiConsole.WriteLine($"Writing LOC sectors to {outputFileName}");
        await fileExtractionService.WriteFileAsync(outputFileName, contents);
    }

    public Task Repack()
    {
        throw new NotImplementedException();
    }

    private void RenameFiles(string outputDirectory)
    {
        foreach (var folder in FolderConstants.Folders.Keys)
        {
            var folderPath = Path.Combine(outputDirectory, folder);

            if (!Directory.Exists(folderPath))
                continue;

            string fileNameFormat = FolderConstants.FileNameFormats.TryGetValue(folder, out var format)
                ? format
                : folder;

            // Get all file names sorted numerically by their name (sector number)
            var files = Directory.GetFiles(folderPath)
                .OrderBy(path => int.Parse(Path.GetFileNameWithoutExtension(path)))
                .ToList();

            for (int i = 0; i < files.Count; i++)
            {
                string oldPath = files[i];
                string newFileName;

                if (fileNameFormat.Contains("{")) // C# string format equivalent to Python's `%`
                    newFileName = string.Format(fileNameFormat, i + 1);
                else
                    newFileName = fileNameFormat;

                string newPath = Path.Combine(folderPath, newFileName);

                File.Move(oldPath, newPath, overwrite: true);
            }
        }
    }

    private void ReportSectorGaps(HashSet<(int SectorNumber, int SizeInSectors)> seenEntries)
    {
        var sortedEntries = seenEntries
            .OrderBy(entry => entry.SectorNumber)
            .ToList();

        for (int i = 1; i < sortedEntries.Count; i++)
        {
            var prev = sortedEntries[i - 1];
            var curr = sortedEntries[i];

            int prevEnd = prev.SectorNumber + prev.SizeInSectors - 1;
            int currStart = curr.SectorNumber;

            if (currStart - prevEnd > 1)
            {
                int gapStart = prevEnd + 1;
                int gapEnd = currStart - 1;

                string gapStartTime = SectorToTimeString(gapStart);
                string gapEndTime = SectorToTimeString(gapEnd);
                int gapLength = gapEnd - gapStart + 1;

                AnsiConsole.WriteLine(
                    $"Gap between sectors: {gapStart}-{gapEnd} or {gapStartTime} to {gapEndTime} with length {gapLength} Sectors");
            }
        }
    }

    private string SectorToTimeString(int sector)
    {
        int total = sector + 150; // offset for CD-ROM sector timing
        int minute = (total / 75) / 60;
        int second = (total / 75) % 60;
        int frame = total % 75;
        return $"{minute:D2}:{second:D2}:{frame:D2}";
    }
}
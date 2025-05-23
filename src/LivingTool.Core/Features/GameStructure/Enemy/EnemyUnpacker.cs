using System.Runtime.InteropServices;
using LivingTool.Core.Features.BinaryHandler;

namespace LivingTool.Core.Features.GameStructure.Enemy;

public class EnemyUnpacker(IBinaryFileService binaryFileService)
{
    public EnemyData Unpack(string filePath)
    {
        var fileHeader = binaryFileService.ReadFileHeader(filePath);
        using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));

        // Read name from pointer6 + 2
        reader.BaseStream.Seek(fileHeader.Pointer6 + 0x2, SeekOrigin.Begin);
        var enemyName = ReadNullTerminatedAscii(reader);

        var fileSize = new FileInfo(filePath).Length;

        // Read moves from pointer6 + 2
        var moves = ReadMoveEntries(reader, fileHeader.Pointer6, (int)fileHeader.Pointer7, fileSize);

        return new EnemyData
        {
            Name = enemyName,
            Moves = moves.Select(m => m.Name).ToList()
        };
    }

    public List<MoveEntry> ReadMoveEntries(BinaryReader reader, long pointer6, int pointer7, long fileSize)
    {
        var moves = new List<MoveEntry>();

        // Calculate metadata block size
        int metadataLength = pointer7 > 0 ? pointer7 : (int)(fileSize - pointer6);
        long metadataEnd = pointer6 + metadataLength;

        // Step 1: Seek to Pointer6 + 2 (skip first 2 bytes)
        reader.BaseStream.Seek(pointer6 + 2, SeekOrigin.Begin);

        // Step 2: Read enemy name
        string enemyName = ReadNullTerminatedAscii(reader);

        // Step 3: Skip all padding (0x00) bytes after name
        SkipNullPadding(reader);

        // Step 4: Read move entries until end of metadata
        while (reader.BaseStream.Position + 2 < metadataEnd)
        {
            ushort prefix = reader.ReadUInt16();
            string moveName = ReadNullTerminatedAscii(reader);

            if (string.IsNullOrWhiteSpace(moveName))
                break;

            moves.Add(new MoveEntry
            {
                Prefix = prefix,
                Name = moveName
            });
        }

        return moves;
    }

    private void SkipNullPadding(BinaryReader reader)
    {
        while (reader.PeekChar() == 0x00)
        {
            reader.ReadByte();
        }
    }

    private string ReadNullTerminatedAscii(BinaryReader reader, int maxLength = 32)
    {
        var bytes = new List<byte>();
        for (int i = 0; i < maxLength; i++)
        {
            byte b = reader.ReadByte();
            if (b == 0x00) break;
            bytes.Add(b);
        }
        return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
    }
}
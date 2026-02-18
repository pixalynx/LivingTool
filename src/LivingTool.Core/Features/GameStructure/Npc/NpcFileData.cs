using System.Text;

namespace LivingTool.Core.Features.GameStructure.Npc;

public class NpcFileData
{
    public int HeaderSize { get; private set; }
    public ushort GroupACount { get; private set; }
    public ushort GroupBCount { get; private set; }
    public int EntitySectionOffset { get; private set; }
    public int EntitySectionLength { get; private set; }
    public int EntityRecordCount { get; private set; }

    public List<PointerEntry> EntriesA { get; } = new();
    public List<PointerEntry> EntriesB { get; } = new();
    public List<NpcEntityRecord> EntityRecords { get; } = new();
    public List<int> NamePointerOffsets { get; } = new();
    public List<int> DialoguePointerOffsets { get; } = new();
    public List<string> Names { get; } = [];
    public List<string> Dialogues { get; } = [];
    public List<int> TmdOffsets { get; } = new();
    public List<int> TimOffsets { get; } = new();


    public NpcFileData(byte[] data)
    {
        ParseTopLevelPointers(data);
        ParseEntry0Metadata(data);
        ParseEntry0PointerTable(data);
        ParseEntitySection(data);
        GetTmdOffsets(data);
        GetTimOffsets(data);
    }

    private void GetTimOffsets(byte[] data)
    {
        foreach (var entry in EntriesA)
        {
            if (!CanRead(data, entry.Offset, 4))
                continue;

            var value = BitConverter.ToUInt32(data, entry.Offset);
            if (value == 0x10)
            {
                TimOffsets.Add(entry.Offset);
            }
        }
    }

    private void GetTmdOffsets(byte[] data)
    {
        foreach (var entry in EntriesA)
        {
            if (!CanRead(data, entry.Offset, 4))
                continue;

            var value = BitConverter.ToUInt32(data, entry.Offset);
            if (value == 0x41)
            {
                TmdOffsets.Add(entry.Offset);
            }
        }
    }

    private void ParseTopLevelPointers(byte[] data)
    {
        if (!CanRead(data, 0, 4))
            return;

        int headerSizeInBytes = BitConverter.ToInt32(data, 0);
        if (headerSizeInBytes <= 0)
            return;

        HeaderSize = headerSizeInBytes;
        int headerCount = headerSizeInBytes / 4;

        for (int ptrIndex = 0; ptrIndex < headerCount; ptrIndex++)
        {
            int pointerOffset = ptrIndex * 4;
            if (!CanRead(data, pointerOffset, 4))
                break;

            int fixedPtr = BitConverter.ToInt32(data, pointerOffset);
            if (fixedPtr < 0 || fixedPtr >= data.Length)
                continue;

            EntriesA.Add(new PointerEntry
            {
                Index = ptrIndex,
                Offset = fixedPtr
            });
        }
    }

    private void ParseEntry0Metadata(byte[] data)
    {
        if (EntriesA.Count == 0)
            return;

        int entry0Offset = EntriesA[0].Offset;
        if (!CanRead(data, entry0Offset, 4))
            return;

        GroupACount = BitConverter.ToUInt16(data, entry0Offset);
        GroupBCount = BitConverter.ToUInt16(data, entry0Offset + 2);
    }

    private void ParseEntry0PointerTable(byte[] data)
    {
        if (EntriesA.Count == 0)
            return;

        int entry0Offset = EntriesA[0].Offset;
        int index = 0;

        for (int offset = entry0Offset + 4; CanRead(data, offset, 4); offset += 4)
        {
            int relativePointer = BitConverter.ToInt32(data, offset);
            if (relativePointer == 0)
                break;

            int absolutePointer = entry0Offset + relativePointer;
            if (absolutePointer < 0 || absolutePointer >= data.Length)
                continue;

            EntriesB.Add(new PointerEntry
            {
                Index = index,
                Offset = absolutePointer
            });
            index++;
        }
    }

    private void ParseEntitySection(byte[] data)
    {
        if (EntriesA.Count < 2)
            return;

        int entityOffset = EntriesA[1].Offset;
        int entityEnd = GetSectionEndOffset(1, data.Length);
        if (entityEnd <= entityOffset || !CanRead(data, entityOffset, 4))
            return;

        EntitySectionOffset = entityOffset;
        EntitySectionLength = entityEnd - entityOffset;
        EntityRecordCount = BitConverter.ToInt32(data, entityOffset);

        int maxRecordCount = Math.Max(0, (EntitySectionLength - 4) / 16);
        int safeRecordCount = Math.Clamp(EntityRecordCount, 0, maxRecordCount);

        byte[] entitySection = new byte[EntitySectionLength];
        Buffer.BlockCopy(data, entityOffset, entitySection, 0, EntitySectionLength);

        for (int i = 0; i < safeRecordCount; i++)
        {
            int recordOffset = 4 + (i * 16);
            if (!CanRead(entitySection, recordOffset, 16))
                break;

            EntityRecords.Add(new NpcEntityRecord
            {
                Index = i,
                ScriptAOffset = BitConverter.ToInt32(entitySection, recordOffset),
                PackedValue = BitConverter.ToUInt32(entitySection, recordOffset + 4),
                ScriptCOffset = BitConverter.ToInt32(entitySection, recordOffset + 8),
                Flags = BitConverter.ToUInt32(entitySection, recordOffset + 12)
            });
        }

        ExtractTextPointers(entitySection);
    }

    private void ExtractTextPointers(byte[] entitySection)
    {
        var scriptStarts = EntityRecords
            .Select(x => x.ScriptAOffset)
            .Where(x => x >= 0 && x < entitySection.Length)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var nameOffsets = new HashSet<int>();
        var dialogueOffsets = new HashSet<int>();

        foreach (var record in EntityRecords)
        {
            if (record.ScriptAOffset < 0 || record.ScriptAOffset >= entitySection.Length)
                continue;

            int scriptEnd = GetNextScriptStart(scriptStarts, record.ScriptAOffset, entitySection.Length);
            int scanEnd = Math.Min(scriptEnd, record.ScriptAOffset + 0x180);

            if (TryFindPointerByOpcode(entitySection, 0x1F, record.ScriptAOffset, scanEnd, out int nameOffset) &&
                TryReadNormalizedString(entitySection, nameOffset, out string name))
            {
                record.NamePointerOffset = nameOffset;
                record.Name = name;

                if (nameOffsets.Add(nameOffset))
                {
                    NamePointerOffsets.Add(nameOffset);
                    Names.Add(name);
                }
            }

            for (int pos = record.ScriptAOffset; pos + 2 < scanEnd; pos++)
            {
                if (entitySection[pos] != 0x01)
                    continue;

                int dialogueOffset = BitConverter.ToUInt16(entitySection, pos + 1);
                if (dialogueOffset < 0 || dialogueOffset >= entitySection.Length)
                    continue;

                if (nameOffsets.Contains(dialogueOffset))
                    continue;

                if (!TryReadNormalizedString(entitySection, dialogueOffset, out string dialogue))
                    continue;

                if (dialogue.Length < 4)
                    continue;

                if (dialogueOffsets.Add(dialogueOffset))
                {
                    DialoguePointerOffsets.Add(dialogueOffset);
                    Dialogues.Add(dialogue);
                }
            }
        }
    }

    private static bool TryFindPointerByOpcode(
        byte[] data,
        byte opcode,
        int startOffset,
        int endOffset,
        out int pointerOffset)
    {
        pointerOffset = -1;

        if (startOffset < 0 || endOffset <= startOffset || endOffset > data.Length)
            return false;

        for (int i = startOffset; i + 2 < endOffset; i++)
        {
            if (data[i] != opcode)
                continue;

            int pointer = BitConverter.ToUInt16(data, i + 1);
            if (pointer < 0 || pointer >= data.Length)
                continue;

            pointerOffset = pointer;
            return true;
        }

        return false;
    }

    private static bool TryReadNormalizedString(byte[] data, int offset, out string value)
    {
        value = string.Empty;

        if (!CanRead(data, offset, 1))
            return false;

        byte first = data[offset];
        if (!IsTextByte(first))
            return false;

        var sb = new StringBuilder();

        for (int i = offset; i < data.Length; i++)
        {
            byte b = data[i];
            if (b == 0x00)
                break;

            if (b == 0x06 || b == 0x07)
            {
                if (sb.Length > 0 && sb[^1] != '\n')
                {
                    sb.Append('\n');
                }

                continue;
            }

            if (b >= 0x20 && b <= 0x7E)
            {
                sb.Append((char)b);
                continue;
            }

            break;
        }

        value = sb.ToString().Trim();
        return !string.IsNullOrWhiteSpace(value);
    }

    private int GetSectionEndOffset(int entryIndex, int fileLength)
    {
        if (entryIndex < 0 || entryIndex >= EntriesA.Count)
            return fileLength;

        return entryIndex + 1 < EntriesA.Count ? EntriesA[entryIndex + 1].Offset : fileLength;
    }

    private static int GetNextScriptStart(List<int> sortedStarts, int currentStart, int defaultEnd)
    {
        if (sortedStarts.Count == 0)
            return defaultEnd;

        int index = sortedStarts.BinarySearch(currentStart);
        if (index < 0)
            index = ~index;

        while (index < sortedStarts.Count && sortedStarts[index] <= currentStart)
        {
            index++;
        }

        return index < sortedStarts.Count ? sortedStarts[index] : defaultEnd;
    }

    private static bool CanRead(byte[] data, int offset, int length)
    {
        return offset >= 0 && length >= 0 && offset <= data.Length - length;
    }

    private static bool IsTextByte(byte value)
    {
        return (value >= 0x20 && value <= 0x7E) || value == 0x06 || value == 0x07;
    }
}

public class NpcEntityRecord
{
    public int Index { get; set; }
    public int ScriptAOffset { get; set; }
    public uint PackedValue { get; set; }
    public int ScriptCOffset { get; set; }
    public uint Flags { get; set; }
    public int? NamePointerOffset { get; set; }
    public string? Name { get; set; }

    public ushort TypeId => (ushort)(PackedValue & 0xFFFF);
    public sbyte ParameterA => unchecked((sbyte)((PackedValue >> 16) & 0xFF));
    public sbyte ParameterB => unchecked((sbyte)((PackedValue >> 24) & 0xFF));

    public override string ToString()
    {
        return $"[{Index}] A=0x{ScriptAOffset:X4} C=0x{ScriptCOffset:X4} Type=0x{TypeId:X4} Flags=0x{Flags:X4}";
    }
}

public class PointerEntry
{
    public int Index { get; set; }
    public int Offset { get; set; } // Absolute file offset

    public override string ToString()
    {
        return $"[{Index}] => 0x{Offset:X5}";
    }
}

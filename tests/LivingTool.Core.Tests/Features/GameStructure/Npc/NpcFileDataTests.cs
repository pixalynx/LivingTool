using FluentAssertions;
using LivingTool.Core.Features.GameStructure.Npc;

namespace LivingTool.Core.Tests.Features.GameStructure.Npc;

public class NpcFileDataTests
{
    [Theory]
    [InlineData("NPC07.BIN", 0x60, 24, 0x60, 0x23C)]
    [InlineData("NPC11.BIN", 0x78, 30, 0x78, 0x13C)]
    public void NpcFileHeader_ShouldHaveCorrectEntries_BasedOnFileHeaderProvided(
        string file,
        int headerSize,
        int entryCount,
        int firstOffset,
        int secondOffset) => Test(
        arrange: () =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Features/GameStructure/Npc/TestData", file);
            return File.ReadAllBytes(filePath);
        },
        assert: (header) =>
        {
            header.HeaderSize.Should().Be(headerSize);
            header.EntriesA.Should().HaveCount(entryCount);
            header.EntriesA[0].Offset.Should().Be(firstOffset);
            header.EntriesA[1].Offset.Should().Be(secondOffset);
        });

    [Theory]
    [InlineData("NPC07.BIN", 12, 10)]
    [InlineData("NPC11.BIN", 19, 9)]
    public void NpcFileEntry0_ShouldExposeExpectedGroupCounts(string file, int groupA, int groupB) => Test(
        arrange: () =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Features/GameStructure/Npc/TestData", file);
            return File.ReadAllBytes(filePath);
        },
        assert: (header) =>
        {
            header.GroupACount.Should().Be((ushort)groupA);
            header.GroupBCount.Should().Be((ushort)groupB);
            (header.GroupACount + header.GroupBCount + 2).Should().Be(header.EntriesA.Count);
        });

    [Theory]
    [InlineData("NPC07.BIN", 59, 0x1894, 0x1CE0, 0x0000, 0x0002)]
    [InlineData("NPC11.BIN", 54, 0x142C, 0x1814, 0x0057, 0x0002)]
    public void NpcFileEntitySection_ShouldParseExpectedRecordShape(
        string file,
        int entityRecordCount,
        int firstScriptA,
        int firstScriptC,
        int firstTypeId,
        uint firstFlags) => Test(
        arrange: () =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Features/GameStructure/Npc/TestData", file);
            return File.ReadAllBytes(filePath);
        },
        assert: (header) =>
        {
            header.EntityRecordCount.Should().Be(entityRecordCount);
            header.EntityRecords.Should().HaveCount(entityRecordCount);
            header.EntityRecords[0].ScriptAOffset.Should().Be(firstScriptA);
            header.EntityRecords[0].ScriptCOffset.Should().Be(firstScriptC);
            header.EntityRecords[0].TypeId.Should().Be((ushort)firstTypeId);
            header.EntityRecords[0].Flags.Should().Be(firstFlags);
        });

    [Theory]
    [InlineData("NPC07.BIN", 13, 0, 0x23F0)]
    [InlineData("NPC11.BIN", 19, 0, 0x1D60)]
    public void NpcFileModelSignatures_ShouldExposeExpectedTmdAndTimOffsets(
        string file,
        int tmdCount,
        int timCount,
        int firstTmdOffset) => Test(
        arrange: () =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Features/GameStructure/Npc/TestData", file);
            return File.ReadAllBytes(filePath);
        },
        assert: (header) =>
        {
            header.TmdOffsets.Should().HaveCount(tmdCount);
            header.TimOffsets.Should().HaveCount(timCount);
            header.TmdOffsets[0].Should().Be(firstTmdOffset);
        });

    [Theory]
    [InlineData("NPC07.BIN", "Algo", 0x450, "There are a lot more")]
    [InlineData("NPC11.BIN", "Informer", 0x51C, "Welcome to Zed Harbor")]
    public void NpcFileEntityText_ShouldExposeNamesAndDialoguePointers(
        string file,
        string expectedName,
        int expectedNamePointer,
        string expectedDialogueText) => Test(
        arrange: () =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Features/GameStructure/Npc/TestData", file);
            return File.ReadAllBytes(filePath);
        },
        assert: (header) =>
        {
            header.Names.Should().Contain(expectedName);
            header.NamePointerOffsets.Should().Contain(expectedNamePointer);
            header.Dialogues.Should().Contain(x => x.Contains(expectedDialogueText));
        });

    private static void Test(
        Func<byte[]> arrange,
        Action<NpcFileData> assert)
    {
        var file = arrange();

        var sut = new NpcFileData(file);

        assert(sut);
    }
}

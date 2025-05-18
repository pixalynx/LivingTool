namespace LivingTool.Core.Features.Extraction;

public static class SectorConstants
{
    // Constants for CD-ROM sector sizes for the ps1
    public const int SectorSize = 0x930; // 2352 bytes
    public const int HeaderSize = 0x18; // 24 bytes
    public const int FooterSize = 0x118; // 280 bytes
    public const int ContentSize = 0x800; // 2048 bytes
    public const int StrHeaderSize = 0x10; // 16 bytes
    public const int StrContentSize = 0x920; // 2336 bytes
}
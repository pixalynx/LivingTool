namespace LivingTool.Core.Features.Extraction;

public static class FolderConstants
{
    public static readonly Dictionary<string, int> Folders = new()
    {
        { "LT", 179 },
        { "BF", 1849 },
        { "KN", 4261 },
        { "PB", 4641 },
        { "ENE", 5650 },
        { "SYS", 14784 },
        { "B", 15729 },
        { "FE", 16030 },
        { "BGM", 16952 },
        { "F", 20010 },
        { "NPC", 85511 },
        { "M", 88391 },
        { "M2", 164074 }
    };

    public static readonly Dictionary<string, string> FileNameFormats = new()
    {
        { "LT", "TOY{0:00}.BIN" },
        { "BF", "BF{0:00}.BIN" },
        { "KN", "KNIGHT{0:00}.BIN" },
        { "PB", "PB{0:00}.BIN" },
        { "ENE", "ENEMY{0:000}.BIN" },
        { "SYS", "SYS{0:00}.BIN" },
        { "B", "OVLY{0:00}.BIN" },
        { "FE", "FE{0:00}.BIN" },
        { "BGM", "BGM{0:00}.BIN" },
        { "F", "F.BIN" },
        { "NPC", "NPC{0:00}.BIN" },
        { "M", "VIDEOS{0:00}.STR" },
        { "M2", "ATTRACT.STR" }
    };
}

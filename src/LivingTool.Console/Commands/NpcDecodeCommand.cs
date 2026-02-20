using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using LivingTool.Core.Features.GameStructure.Npc;
using Spectre.Console;
using Spectre.Console.Cli;

namespace LivingTool.Console.Commands;

public class NpcDecodeCommandSettings : CommandSettings
{
    [CommandOption("-f|--file")]
    [Description("Path to an NPC BIN file (for example: output/NPC/NPC07.BIN)")]
    public required string FilePath { get; init; }

    [CommandOption("--format")]
    [DefaultValue("json")]
    [Description("Output format: json or text")]
    public string Format { get; init; } = "json";
}

public class NpcDecodeCommand : AsyncCommand<NpcDecodeCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, NpcDecodeCommandSettings settings)
    {
        bool outputJson = !string.Equals(settings.Format, "text", StringComparison.OrdinalIgnoreCase);

        if (!File.Exists(settings.FilePath))
        {
            if (outputJson)
            {
                WriteJson(new NpcDecodeErrorOutput
                {
                    Error = "file_not_found",
                    Message = $"NPC BIN file not found: {settings.FilePath}",
                    FilePath = settings.FilePath
                }, NpcJsonSerializerContext.Default.NpcDecodeErrorOutput);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]NPC BIN file not found:[/] {settings.FilePath}");
            }

            return 1;
        }

        byte[] data = await File.ReadAllBytesAsync(settings.FilePath);
        var npcData = new NpcFileData(data);

        if (outputJson)
        {
            WriteJson(new NpcDecodeOutput
            {
                FilePath = settings.FilePath,
                HeaderSize = npcData.HeaderSize,
                TopLevelEntries = npcData.EntriesA.Count,
                GroupACount = npcData.GroupACount,
                GroupBCount = npcData.GroupBCount,
                EntityRecordCount = npcData.EntityRecordCount,
                NameCount = npcData.Names.Count,
                DialogueCount = npcData.Dialogues.Count,
                TmdOffsets = npcData.TmdOffsets,
                TimOffsets = npcData.TimOffsets,
                NamePointerOffsets = npcData.NamePointerOffsets,
                DialoguePointerOffsets = npcData.DialoguePointerOffsets,
                Names = npcData.Names,
                Dialogues = npcData.Dialogues
            }, NpcJsonSerializerContext.Default.NpcDecodeOutput);

            return 0;
        }

        AnsiConsole.MarkupLine($"[green]File:[/] {settings.FilePath}");
        AnsiConsole.MarkupLine($"[green]Header Size:[/] 0x{npcData.HeaderSize:X}");
        AnsiConsole.MarkupLine($"[green]Top-Level Entries:[/] {npcData.EntriesA.Count}");
        AnsiConsole.MarkupLine($"[green]Entity Records:[/] {npcData.EntityRecordCount}");
        AnsiConsole.MarkupLine($"[green]Names:[/] {npcData.Names.Count}");
        AnsiConsole.MarkupLine($"[green]Dialogues:[/] {npcData.Dialogues.Count}");

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]NPC Names[/]");

        if (npcData.Names.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No names decoded.[/]");
        }
        else
        {
            for (int i = 0; i < npcData.Names.Count; i++)
            {
                AnsiConsole.WriteLine($"{i + 1}. {npcData.Names[i]}");
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]NPC Dialogues[/]");

        if (npcData.Dialogues.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No dialogues decoded.[/]");
        }
        else
        {
            for (int i = 0; i < npcData.Dialogues.Count; i++)
            {
                AnsiConsole.WriteLine($"{i + 1}. {npcData.Dialogues[i]}");
                AnsiConsole.WriteLine();
            }
        }

        return 0;
    }

    private static void WriteJson<TValue>(TValue value, JsonTypeInfo<TValue> typeInfo)
    {
        string json = JsonSerializer.Serialize(value, typeInfo);
        System.Console.WriteLine(json);
    }
}

public class NpcDecodeOutput
{
    public required string FilePath { get; init; }
    public int HeaderSize { get; init; }
    public int TopLevelEntries { get; init; }
    public ushort GroupACount { get; init; }
    public ushort GroupBCount { get; init; }
    public int EntityRecordCount { get; init; }
    public int NameCount { get; init; }
    public int DialogueCount { get; init; }
    public required List<int> TmdOffsets { get; init; }
    public required List<int> TimOffsets { get; init; }
    public required List<int> NamePointerOffsets { get; init; }
    public required List<int> DialoguePointerOffsets { get; init; }
    public required List<string> Names { get; init; }
    public required List<string> Dialogues { get; init; }
}

public class NpcDecodeErrorOutput
{
    public required string Error { get; init; }
    public required string Message { get; init; }
    public required string FilePath { get; init; }
}

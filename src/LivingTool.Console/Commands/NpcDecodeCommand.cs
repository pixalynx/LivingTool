using System.ComponentModel;
using LivingTool.Core.Features.GameStructure.Npc;
using Spectre.Console;
using Spectre.Console.Cli;

namespace LivingTool.Console.Commands;

public class NpcDecodeCommandSettings : CommandSettings
{
    [CommandOption("-f|--file")]
    [Description("Path to an NPC BIN file (for example: output/NPC/NPC07.BIN)")]
    public required string FilePath { get; init; }
}

public class NpcDecodeCommand : AsyncCommand<NpcDecodeCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, NpcDecodeCommandSettings settings)
    {
        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.MarkupLine($"[red]NPC BIN file not found:[/] {settings.FilePath}");
            return 1;
        }

        byte[] data = await File.ReadAllBytesAsync(settings.FilePath);
        var npcData = new NpcFileData(data);

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
}

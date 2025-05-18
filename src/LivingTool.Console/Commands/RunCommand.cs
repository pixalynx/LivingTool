using System.ComponentModel;
using LivingTool.Console.Features.Unpacker;
using LivingTool.Core.Features.Extraction;
using Spectre.Console;
using Spectre.Console.Cli;

namespace LivingTool.Console.Commands;

public class RunCommandSettings : CommandSettings
{
    [CommandOption("-f|--file")]
    [DefaultValue("gc.bin")]
    [Description("The file to unpack")]
    public required string File { get; set; }

    [CommandOption("-o|--output-directory")]
    [DefaultValue("output")]
    [Description("The output directory for the unpacked files")]
    public required string OutputDirectory { get; set; }

    [CommandOption("-l|--loc-sectors-file")]
    [DefaultValue("locsectors.bin")]
    [Description("The loc sectors file")]
    public required string LocSectorsFile { get; set; }
}

public class RunCommand : AsyncCommand<RunCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RunCommandSettings settings)
    {
        var guardiansCrusadeFileService = new GuardiansCrusadeFileService();
        var unpacker = new GameUnpacker(guardiansCrusadeFileService);

        // log the settings
        AnsiConsole.MarkupLine($"[green] File is : {settings.File}[/]");
        AnsiConsole.MarkupLine($"[green] Output Folder is : {settings.OutputDirectory}[/]");
        AnsiConsole.MarkupLine($"[green] Loc Sectors File is : {settings.LocSectorsFile}[/]");

        // Unpack the file
        AnsiConsole.WriteLine($"Unpacking file: {settings.File}");
        await unpacker.Unpack(settings.File, settings.OutputDirectory, settings.LocSectorsFile);

        return 0;
    }
}
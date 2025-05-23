using LivingTool.Console.Features.Structure;
using Spectre.Console.Cli;

namespace LivingTool.Console.Commands;

public class ReadCommandSettings : CommandSettings
{
    [CommandOption("-f|--file")]
    public required string FilePath { get; init; }
}

public class ReadCommand : AsyncCommand<ReadCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ReadCommandSettings settings)
    {
        var structureReader = new StructureReader();
        structureReader.ReadStructure(settings.FilePath);

        return 0;
    }
}
using LivingTool.Core.Features.BinaryHandler;
using LivingTool.Core.Features.GameStructure.Enemy;
using Spectre.Console;

namespace LivingTool.Console.Features.Structure;

public class StructureReader
{
    public void ReadStructure(string filePath)
    {
        var binaryFileService = new BinaryFileService();
        var enemyUnpacker = new EnemyUnpacker(binaryFileService);

        var enemyData = enemyUnpacker.Unpack(filePath);

        // Process the enemy data as needed
        AnsiConsole.MarkupLine($"[green]Enemy Name:[/] {enemyData.Name}");
        AnsiConsole.MarkupLine($"[green]Moves:[/]");
        foreach (var move in enemyData.Moves)
        {
            AnsiConsole.MarkupLine($"- {move}");
        }
    }
}
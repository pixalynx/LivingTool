// See https://aka.ms/new-console-template for more information

using LivingTool.Console.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

AnsiConsole.MarkupLine("[bold green]Welcome to LivingTool Console![/]");
var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<RunCommand>("run");
    config.AddCommand<ReadCommand>("read");
});
return app.Run(args);
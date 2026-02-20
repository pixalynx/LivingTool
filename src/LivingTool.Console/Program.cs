// See https://aka.ms/new-console-template for more information

using LivingTool.Console.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<RunCommand>("run");
    config.AddCommand<ReadCommand>("read");
    config.AddCommand<NpcDecodeCommand>("npc")
        .WithDescription("Decode an NPC BIN file and list names/dialogues.");
});
return app.Run(args);

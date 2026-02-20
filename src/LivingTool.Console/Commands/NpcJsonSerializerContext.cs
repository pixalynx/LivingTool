using System.Text.Json.Serialization;

namespace LivingTool.Console.Commands;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(NpcDecodeOutput))]
[JsonSerializable(typeof(NpcDecodeErrorOutput))]
public partial class NpcJsonSerializerContext : JsonSerializerContext
{
}

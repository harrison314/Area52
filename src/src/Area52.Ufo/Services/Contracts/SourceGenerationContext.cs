using System.Text.Json.Serialization;

namespace Area52.Ufo.Services.Contracts;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(ClefObject))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}

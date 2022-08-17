using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace Area52.Tool;

public class ClefObject
{
    [JsonPropertyName("@t")]
    public DateTimeOffset Timestamp
    {
        get;
        set;
    }

    [JsonPropertyName("@m")]
    public string? Message
    {
        get;
        set;
    }

    [JsonPropertyName("@mt")]
    public string? MessageTemplate
    {
        get;
        set;
    }

    [JsonPropertyName("@l")]
    public string? Level
    {
        get;
        set;
    }

    [JsonPropertyName("@x")]
    public string? Exception
    {
        get;
        set;
    }

    [JsonPropertyName("@i")]
    public long? EventId
    {
        get;
        set;
    }

    [JsonPropertyName("@r")]
    public string[]? Renderings
    {
        get;
        set;
    }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Properties
    {
        get;
        set;
    }

    public ClefObject()
    {
        this.Properties = new Dictionary<string, JsonElement>();
    }
}


[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(ClefObject))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}

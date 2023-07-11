using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Area52.Ufo.Services.Contracts;

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
    [JsonConverter(typeof(NumberOrTextConvertor))]
    public string? EventId
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
    public Dictionary<string, object> Properties
    {
        get;
        set;
    }

    public ClefObject()
    {
        this.Properties = new Dictionary<string, object>();
    }
}

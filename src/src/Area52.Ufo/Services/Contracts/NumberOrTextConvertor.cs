using System;
using System.Formats.Asn1;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Area52.Ufo.Services.Contracts;

public class NumberOrTextConvertor : JsonConverter<string?>
{
    public NumberOrTextConvertor()
    {
        
    }

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
       return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetInt64().ToString(),
            _ => throw new InvalidProgramException("Unsuported type of property @i. Must by string or number.") //TODO: other exception
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Area52.Ufo.Services.Contracts;

internal class ClefValueJsonConverter : JsonConverter<ClefValue>
{
    public override ClefValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, ClefValue value, JsonSerializerOptions options)
    {
        switch(value.Type)
        {
            case ClefValueType.Int:
                writer.WriteNumberValue(value.AsInt);
                break;

            case ClefValueType.Double:
                writer.WriteNumberValue(value.AsDouble);
                break;

            case ClefValueType.Bool:
                writer.WriteBooleanValue(value.AsBool);
                break;

            case ClefValueType.String:
                writer.WriteBooleanValue(value.AsString);
                break;
        }
    }
}

using Area52.Services.Contracts;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Area52.Infrastructure.Clef;

// Format: https://docs.datalust.co/docs/posting-raw-events
public static class ClefParser
{
    public static LogEntity Read(ReadOnlySpan<byte> line)
    {
        LogEntity entry = new LogEntity();
        entry.Level = "informational";
        string[]? renderings = null;

        System.Text.Json.Utf8JsonReader jsonReader = new System.Text.Json.Utf8JsonReader(line);
        LogEntityProperty[] tmpArray = ArrayPool<LogEntityProperty>.Shared.Rent(32);
        try
        {
            int arrayIndex = 0;
            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == System.Text.Json.JsonTokenType.PropertyName)
                {
                    string propName = jsonReader.GetString()!;
                    jsonReader.Read();

                    switch (propName)
                    {
                        case "@t":
                            entry.Timestamp = jsonReader.GetDateTimeOffset().ToUniversalTime();
                            break;

                        case "@m":
                            entry.Message = jsonReader.GetString()!;
                            break;

                        case "@mt":
                            entry.MessageTemplate = jsonReader.GetString();
                            break;

                        case "@l":
                            entry.Level = jsonReader.GetString() ?? "informational";
                            break;

                        case "@x":
                            entry.Exception = jsonReader.GetString()!;
                            break;

                        case "@i":
                            entry.EventId = (jsonReader.TokenType == System.Text.Json.JsonTokenType.Null)
                                ? default
                                : jsonReader.GetInt64();
                            break;

                        case "@r":
                            System.Text.Json.Nodes.JsonArray? array = System.Text.Json.Nodes.JsonNode.Parse(ref jsonReader) as System.Text.Json.Nodes.JsonArray;
                            if (array != null)
                            {
                                renderings = new string[array.Count];
                                for (int i = 0; i < array.Count; i++)
                                {
                                    renderings[i] = ((string?)array[i])!;
                                }
                            }
                            break;
                        default:
                            if (propName.StartsWith('@'))
                            {
                                if (propName.Length > 2 && propName[1] != '@')
                                {
                                    break;
                                }

                                propName = propName[1..];
                            }

                            (LogEntityProperty newProp, bool addProperty) = GetInnerProperty(propName, ref jsonReader);
                            if (addProperty)
                            {
                                tmpArray[arrayIndex] = newProp;
                                arrayIndex++;
                            }
                            break;
                    }
                }
            }

            entry.Properties = tmpArray.AsSpan(0, arrayIndex).ToArray();
        }
        finally
        {
            ArrayPool<LogEntityProperty>.Shared.Return(tmpArray, true);
        }

        if (string.IsNullOrEmpty(entry.Message))
        {
            if (string.IsNullOrEmpty(entry.MessageTemplate))
            {
                entry.Message = string.Empty;
            }
            else
            {
                RenderMessage(entry, renderings);
            }
        }

        EnshureLoglevel(entry);
        return entry;
    }

    public static async Task Write(LogEntity entity, Stream stream)
    {
        await using System.Text.Json.Utf8JsonWriter writer = new System.Text.Json.Utf8JsonWriter(stream, new System.Text.Json.JsonWriterOptions()
        {
            Indented = false
        });

        writer.WriteStartObject();

        writer.WritePropertyName("@t");
        writer.WriteStringValue(entity.Timestamp);

        writer.WritePropertyName("@m");
        writer.WriteStringValue(entity.Message);

        if (entity.MessageTemplate != null)
        {
            writer.WritePropertyName("@mt");
            writer.WriteStringValue(entity.MessageTemplate);
        }

        writer.WritePropertyName("@l");
        writer.WriteStringValue(entity.Level);
        await writer.FlushAsync();

        if (entity.EventId.HasValue)
        {
            writer.WritePropertyName("@i");
            writer.WriteNumberValue(entity.EventId.Value);
        }

        if (entity.Exception != null)
        {
            writer.WritePropertyName("@x");
            writer.WriteStringValue(entity.Exception);
            await writer.FlushAsync();
        }

        foreach (LogEntityProperty property in entity.Properties)
        {
            writer.WritePropertyName(property.Name);
            if (property.Valued.HasValue)
            {
                writer.WriteNumberValue(property.Valued.Value);
            }
            else if (property.Values != null)
            {
                writer.WriteStringValue(property.Values);
            }
            else
            {
                throw new InvalidProgramException("LogEntityProperty has invalid value.");
            }
        }

        writer.WriteEndObject();
        await writer.FlushAsync();
    }

    private static void EnshureLoglevel(LogEntity entry)
    {
        if (string.IsNullOrEmpty(entry.Level))
        {
            entry.Level = "Info";
            entry.LevelNumeric = (int)LogLevel.Information;
            return;
        }

        LogLevel level = char.ToLower(entry.Level[0]) switch
        {
            'd' => LogLevel.Debug,
            'v' or 't' => LogLevel.Trace,
            'e' => LogLevel.Error,
            'i' => LogLevel.Information,
            'w' => LogLevel.Warning,
            'f' or 'c' => LogLevel.Critical,
            _ => LogLevel.None
        };

        entry.LevelNumeric = (int)level;
    }

    private static (LogEntityProperty prop, bool success) GetInnerProperty(string name, ref System.Text.Json.Utf8JsonReader jsonReader)
    {
        if (jsonReader.TokenType == System.Text.Json.JsonTokenType.String)
        {
            return (new LogEntityProperty(name, jsonReader.GetString()!), true);
        }
        else if (jsonReader.TokenType == System.Text.Json.JsonTokenType.Number)
        {
            double valueNumber;
            if (jsonReader.TryGetInt64(out long valueLong))
            {
                valueNumber = (double)valueLong;
            }
            else
            {
                valueNumber = jsonReader.GetDouble();
            }

            return (new LogEntityProperty(name, valueNumber), true);
        }
        else if (jsonReader.TokenType == System.Text.Json.JsonTokenType.Null)
        {
            return (new LogEntityProperty(name, string.Empty), false);
        }
        else if (jsonReader.TokenType == System.Text.Json.JsonTokenType.True)
        {
            return (new LogEntityProperty(name, "true"), true);
        }
        else if (jsonReader.TokenType == System.Text.Json.JsonTokenType.False)
        {
            return (new LogEntityProperty(name, "false"), true);
        }
        else if (jsonReader.TokenType == System.Text.Json.JsonTokenType.StartObject
            || jsonReader.TokenType == System.Text.Json.JsonTokenType.StartArray)
        {
            string? json = System.Text.Json.Nodes.JsonNode.Parse(ref jsonReader)?.ToJsonString();

            return (new LogEntityProperty(name, json ?? string.Empty), true);
        }

        throw new InvalidProgramException($"Invalid token for read property {jsonReader.TokenType}.");
    }

    private static void RenderMessage(LogEntity entry, string[]? renderes)
    {
        StringBuilder sb = new StringBuilder(entry.MessageTemplate);
        for (int i = 0; i < entry.Properties.Length; i++)
        {
            string template = string.Concat("{", entry.Properties[i].Name, "}");
            sb.Replace(template, entry.Properties[i].GetValueString());
        }

        entry.Message = sb.ToString();

        if (renderes != null)
        {
            int index = 0;
            entry.Message = Regex.Replace(entry.Message, "\\{[^\\}]+\\}", match =>
            {
                return renderes[index++];
            });
        }
    }
}

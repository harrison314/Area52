using Area52.Services.Contracts;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Area52.Infrastructure.Clef;

// Format: https://docs.datalust.co/docs/posting-raw-events
public static class ClefParser
{
    public static LogEntity Read(ReadOnlySpan<byte> line, bool strictParsing)
    {
        LogEntity entry = new LogEntity();
        entry.Level = "informational";
        string[]? renderings = null;
        bool timestampPresent = false;

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
                            timestampPresent = true;
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
                            entry.EventId = jsonReader.TokenType switch
                            {
                                System.Text.Json.JsonTokenType.Null => null,
                                System.Text.Json.JsonTokenType.String => jsonReader.GetString(),
                                System.Text.Json.JsonTokenType.Number => jsonReader.GetInt64().ToString(),
                                _ when strictParsing => ThrowInvalidJsonInvalidEventId(jsonReader.TokenType, line),
                                _ => null
                            };

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

                            (LogEntityProperty newProp, bool addProperty) = GetInnerProperty(propName, ref jsonReader, line, strictParsing);
                            if (addProperty)
                            {
                                if (arrayIndex >= tmpArray.Length)
                                {
                                    LogEntityProperty[] newArray = ArrayPool<LogEntityProperty>.Shared.Rent(tmpArray.Length + 32);
                                    tmpArray.CopyTo(newArray.AsSpan());
                                    ArrayPool<LogEntityProperty>.Shared.Return(tmpArray, true);
                                    tmpArray = newArray;
                                }

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
        if (!timestampPresent)
        {
            ThrowInvalidJsonMissingTimestamp(line);
        }

        return entry;
    }

    public static async Task Write(LogEntity entity, Stream stream)
    {
        await using Utf8JsonWriter writer = new Utf8JsonWriter(stream, new System.Text.Json.JsonWriterOptions()
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

        if (entity.EventId != null)
        {
            writer.WritePropertyName("@i");
            writer.WriteStringValue(entity.EventId);
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
                throw new ClefInvalidFormatException("LogEntityProperty has invalid value.");
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

        LogLevel level = entry.Level[0] switch
        {
            'd' or 'D' => LogLevel.Debug,
            'v' or 't' or 'V' or 'T' => LogLevel.Trace,
            'e' or 'E' => LogLevel.Error,
            'i' or 'I' => LogLevel.Information,
            'w' or 'W' => LogLevel.Warning,
            'f' or 'c' or 'F' or 'C' => LogLevel.Critical,
            _ => LogLevel.None
        };

        entry.LevelNumeric = (int)level;
    }

    private static (LogEntityProperty prop, bool success) GetInnerProperty(string name, ref System.Text.Json.Utf8JsonReader jsonReader, ReadOnlySpan<byte> line, bool strictParsing)
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

        if (strictParsing)
        {
            ThrowInvalidJsonInvalidTypeOfProperty(name, jsonReader.TokenType, line);
        }

        return (new LogEntityProperty(name, string.Empty), false);
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

    [DoesNotReturn]
    private static void ThrowInvalidJsonMissingTimestamp(ReadOnlySpan<byte> line)
    {
        throw new ClefInvalidFormatException($"Invalid JSON missing timestamp '@t'. In line: '{SafeDecodeLine(line)}'");
    }

    [DoesNotReturn]
    private static string? ThrowInvalidJsonInvalidEventId(JsonTokenType jsonTokenType, ReadOnlySpan<byte> line)
    {
        throw new ClefInvalidFormatException($"Unsuported type ({jsonTokenType}) of property @i. Must by string or number. In line: '{SafeDecodeLine(line)}'");
    }

    [DoesNotReturn]
    private static void ThrowInvalidJsonInvalidTypeOfProperty(string name, JsonTokenType jsonTokenType, ReadOnlySpan<byte> line)
    {
        throw new ClefInvalidFormatException($"Invalid token for read property {name} of type {jsonTokenType}. In line: '{SafeDecodeLine(line)}'");
    }

    private static string SafeDecodeLine(ReadOnlySpan<byte> line)
    {
        try
        {
            return Encoding.UTF8.GetString(line);
        }
        catch (ArgumentException)
        {
            return string.Concat("hex:", Convert.ToHexString(line));
        }
    }
}

using System;
using System.Text.Json.Serialization;

namespace Area52.Ufo.Services.Contracts;

[JsonConverter(typeof(ClefValueJsonConverter))]
public class ClefValue
{
    private int intValue;
    private double doubleValue;
    private bool boolValue;
    private string? stringValue;

    public ClefValueType Type
    {
        get;
    }

    public int AsInt
    {
        get
        {
            this.Enshure(ClefValueType.Int);
            return this.intValue;
        }
    }

    public double AsDouble
    {
        get
        {
            this.Enshure(ClefValueType.Double);
            return this.doubleValue;
        }
    }

    public bool AsBool
    {
        get
        {
            this.Enshure(ClefValueType.Bool);
            return this.boolValue;
        }
    }

    public bool AsString
    {
        get
        {
            this.Enshure(ClefValueType.String);
            return this.boolValue;
        }
    }

    public ClefValue(int value)
    {
        this.Type = ClefValueType.Int;
        this.intValue = value;
        this.doubleValue = default;
        this.boolValue = default;
        this.stringValue = default;
    }

    public ClefValue(double value)
    {
        this.Type = ClefValueType.Double;
        this.intValue = default;
        this.doubleValue = value;
        this.boolValue = default;
        this.stringValue = default;
    }

    public ClefValue(bool value)
    {
        this.Type = ClefValueType.Bool;
        this.intValue = default;
        this.doubleValue = default;
        this.boolValue = value;
        this.stringValue = default;
    }

    public ClefValue(string value)
    {
        this.Type = ClefValueType.String;
        this.intValue = default;
        this.doubleValue = default;
        this.boolValue = default;
        this.stringValue = value;
    }

    public static ClefValue FromObject(object instance)
    {
        return instance switch
        {
            int intValue => new ClefValue(intValue),
            double doubleValue => new ClefValue(doubleValue),
            bool boolValue => new ClefValue(boolValue),
            string stringValue => new ClefValue(stringValue),
            _ => throw new ArgumentException("Invalid object type.")
        };
    }

    private void Enshure(ClefValueType type)
    {
        if (this.Type != type)
        {
            throw new InvalidProgramException($"Invalid value type. real value is type {this.Type}.");
        }
    }

    public override string ToString()
    {
        return this.Type switch
        {
            ClefValueType.Int => this.intValue.ToString(),
            ClefValueType.Double => this.doubleValue.ToString(),
            ClefValueType.Bool => this.boolValue.ToString(),
            ClefValueType.String => this.stringValue!,
            _ => throw new InvalidProgramException($"Enum value {this.Type} is not supported.")
        };
    }
}

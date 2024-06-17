/*
  LXD (Lightweight Exchange of Data) Format:
  
  LXD (Lightweight Exchange of Data) is a compact and hierarchical serialization format designed for the 
  efficient exchange of structured data in UTF-8 text format. Unlike JSON, LXD is not intended for human 
  readability but serves as a streamlined communication protocol between clients and servers. It utilizes 
  uncommon Unicode characters for delimiters and incorporates type prefixes to clearly specify data types.
  See ReadMe.md (alias !lxd.md)
  Author: Andrew Kingdom 2024
  License: MIT
*/

using System;
using System.Collections;
using System.Globalization;
using System.Text;

public class LxdVar
{
    public char TypeLetter { get; private set; }
    public object Value { get; private set; }
    public bool Strict { get; set; }

    // Strict -- if true, an error is raised if attempting to assign this variable to a strictly different type.
    public LxdVar(object value, bool? strict = null)
    {
        if (value is LxdVar)
        {
            LxdVar lxdVar = (LxdVar)value;
            Value = lxdVar.Value;
            TypeLetter = lxdVar.TypeLetter;
            Strict = strict ?? lxdVar.Strict;
        }
        else
        {
            Value = value;
            Strict = strict ?? false;
            TypeLetter = DetermineTypeLetter(value);
        }
    }

    private char DetermineTypeLetter(object value)
    {
        return value switch
        {
            string _ => 's',
            int _ => 'i',
            float _ => 'f',
            bool _ => 'b',
            DateTime _ => 'd',
            IDictionary _ => 'D',
            IList _ => 'L',
            _ => throw new NotSupportedException($"Unsupported type: {value.GetType()}")
        };
    }

    public T As<T>()
    {
        if (Strict && !(Value is T))
        {
            throw new InvalidCastException($"Cannot cast {Value.GetType()} to {typeof(T)}");
        }
        return (T)Value;
    }

    public static implicit operator LxdVar(string value) => new LxdVar(value);
    public static implicit operator LxdVar(int value) => new LxdVar(value);
    public static implicit operator LxdVar(float value) => new LxdVar(value);
    public static implicit operator LxdVar(bool value) => new LxdVar(value);
    public static implicit operator LxdVar(DateTime value) => new LxdVar(value);
    public static implicit operator LxdVar(Dictionary<string, LxdVar> value) => new LxdVar(value);
    public static implicit operator LxdVar(List<LxdVar> value) => new LxdVar(value);
    public static implicit operator string(LxdVar lxdVar) => lxdVar.As<string>();
    public static implicit operator int(LxdVar lxdVar) => lxdVar.As<int>();
    public static implicit operator float(LxdVar lxdVar) => lxdVar.As<float>();
    public static implicit operator bool(LxdVar lxdVar) => lxdVar.As<bool>();
    public static implicit operator DateTime(LxdVar lxdVar) => lxdVar.As<DateTime>();
    public static implicit operator Dictionary<string, LxdVar>(LxdVar lxdVar) => lxdVar.As<Dictionary<string, LxdVar>>();
    public static implicit operator List<LxdVar>(LxdVar lxdVar) => lxdVar.As<List<LxdVar>>();


    public override string ToString() => Value?.ToString() ?? "null";

}

public static class LXD
{
    private const char RecordStart = '╾';   // U+257E
    private const char RecordEnd = '╼';     // U+257C
    private const char FieldDelimiter = '╽';// U+257D
    private const char KeyValueSeparator = '꞉'; // U+A789

    public static string Serialize<T>(T value)
    {
        StringBuilder sb = new StringBuilder();
        SerializeValue(new LxdVar(value!), sb);
        string result = sb.ToString();
        Console.WriteLine($"•> {result}");
        return result;
    }

    private static void SerializeValue<T>(T value, StringBuilder sb)
    {
        if (value is LxdVar lxdVar)
        {
            SerializeValue(lxdVar.Value, sb); // Recursively serialize LxdVar's inner value
            return;
        }

        switch (value)
        {
            case string s:
                sb.Append('s');
                sb.Append(EscapeString(s));
                break;
            case int i:
                sb.Append('i');
                sb.Append(i.ToString(CultureInfo.InvariantCulture));
                break;
            case float f:
                sb.Append('f');
                sb.Append(f.ToString(CultureInfo.InvariantCulture));
                break;
            case bool b:
                sb.Append('b');
                sb.Append(b ? "true" : "false");
                break;
            case DateTime dt:
                sb.Append('d');
                sb.Append(dt.ToString("O", CultureInfo.InvariantCulture));
                break;
            case IList list:
                sb.Append('L');
                sb.Append(RecordStart);
                foreach (var item in list)
                {
                    SerializeValue(item, sb);
                    sb.Append(FieldDelimiter);
                }
                sb.Append(RecordEnd);
                break;
            case IDictionary<string, object> dict:
                sb.Append('D');
                sb.Append(RecordStart);
                bool isFirst = true;
                foreach (var kvp in dict)
                {
                    if (!isFirst)
                    {
                        sb.Append(FieldDelimiter);
                    }
                    sb.Append(EscapeString(kvp.Key.ToString()!));
                    sb.Append(KeyValueSeparator);
                    SerializeValue(kvp.Value, sb);
                    isFirst = false;
                }
                sb.Append(RecordEnd);
                break;
            case IDictionary<string, LxdVar> dict:
                sb.Append('D'); // Type letter prefix for dictionary
                sb.Append(RecordStart);
                isFirst = true;
                foreach (var kvp in dict)
                {
                    if (!isFirst)
                    {
                        sb.Append(FieldDelimiter);
                    }
                    sb.Append(EscapeString(kvp.Key.ToString()!));
                    sb.Append(KeyValueSeparator);
                    SerializeValue(kvp.Value, sb);
                    isFirst = false;
                }
                sb.Append(RecordEnd);
                break;
            default:
                string type = value?.GetType().ToString() ?? "null";
                throw new NotSupportedException($"Unsupported type: {type}");
        }
    }


    private static void SerializeDictionary(IDictionary<string, LxdVar> data, StringBuilder sb)
    {
        sb.Append('D'); // Type letter prefix for dictionary
        sb.Append(RecordStart);
        bool isFirst = true;
        foreach (var kvp in data)
        {
            if (!isFirst)
            {
                sb.Append(FieldDelimiter);
            }
            sb.Append(EscapeString(kvp.Key.ToString()!));
            sb.Append(KeyValueSeparator);
            SerializeValue(new LxdVar(kvp.Value!), sb);
            isFirst = false;
        }
        sb.Append(RecordEnd);
    }

    private static void SerializeList(IList<LxdVar> list, StringBuilder sb)
    {
        sb.Append('L'); // Type letter prefix for list
        sb.Append(RecordStart);
        foreach (var item in list)
        {
            SerializeValue(new LxdVar(item), sb);
            sb.Append(FieldDelimiter);
        }
        sb.Append(RecordEnd);
    }

    public static T Deserialize<T>(string input)
    {
        Console.WriteLine($"•<- {input}");
        var index = 0;
        return (T)DeserializeValue(typeof(T), input, ref index).Value;
    }

    private static LxdVar DeserializeValue(Type expectedType, string input, ref int index)
    {
        switch (input[index])
        {
            case 's':
                index++; // Skip type letter prefix
                return new LxdVar(UnescapeString(ReadUntil(input, ref index, FieldDelimiter, RecordEnd)));
            case 'i':
                index++; // Skip type letter prefix
                return new LxdVar(int.Parse(ReadUntil(input, ref index, FieldDelimiter, RecordEnd), CultureInfo.InvariantCulture));
            case 'f':
                index++; // Skip type letter prefix
                return new LxdVar(float.Parse(ReadUntil(input, ref index, FieldDelimiter, RecordEnd), CultureInfo.InvariantCulture));
            case 'b':
                index++; // Skip type letter prefix
                return new LxdVar(ReadUntil(input, ref index, FieldDelimiter, RecordEnd) == "true");
            case 'd':
                index++; // Skip type letter prefix
                return new LxdVar(DateTime.ParseExact(ReadUntil(input, ref index, FieldDelimiter, RecordEnd), "O", CultureInfo.InvariantCulture));
            case 'D':
                index++; // Skip type letter prefix (and point to RecordStart char)
                var dictionary = new Dictionary<string, LxdVar>();
                if (input[index] != RecordStart) throw new FormatException("Invalid format: RecordStart expected");
                index++;  // point past RecordStart (to either a RecordEnd or start of next key)
                while (input[index] != RecordEnd)
                {
                    string key = UnescapeString(ReadUntil(input, ref index, KeyValueSeparator));
                    index++;  // point past KeyValueSeparator
                    LxdVar value = DeserializeValue(typeof(LxdVar), input, ref index);
                    dictionary[key] = value;
                    if (input[index] == FieldDelimiter) index++;  // point past FieldDelimiter to either a RecordEnd or start of next key
                }
                index++;  // point past RecordEnd
                return new LxdVar(dictionary);
            case 'L':
                index++; // Skip type letter prefix and point to RecordStart char
                var list = new List<LxdVar>();
                if (input[index] != RecordStart) throw new FormatException("Invalid format: RecordStart expected");
                index++;  // point past KeyValueSeparator
                while (input[index] != RecordEnd)
                {
                    var value = DeserializeValue(expectedType.GetGenericArguments()[0], input, ref index);
                    list.Add(value);
                    if (input[index] == FieldDelimiter) index++;  // point past FieldDelimiter to either a RecordEnd or type letter prefix
                }
                index++; // Move past RecordEnd
                return new LxdVar(list);
            default:
                throw new FormatException("Invalid format: Unknown type letter prefix");
        }
    }

    private static string ReadUntil(string input, ref int index, params char[] delimiters)
    {
        var start = index;
        while (Array.IndexOf(delimiters, input[index]) == -1)
        {
            index++;
        }
        return input.Substring(start, index - start);
    }

    private static string EscapeString(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
        {
            if (c == RecordStart || c == RecordEnd || c == FieldDelimiter || c == KeyValueSeparator)
            {
                sb.AppendFormat("\\x{0:X2}", (int)c);
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static string UnescapeString(string s)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && s[i + 1] == 'x')
            {
                var hex = s.Substring(i + 2, 2);
                sb.Append((char)Convert.ToInt32(hex, 16));
                i += 3; // Skip over the escape sequence
            }
            else
            {
                sb.Append(s[i]);
            }
        }
        return sb.ToString();
    }
}

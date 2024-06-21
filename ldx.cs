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

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text;

// LxdVar is a dynamic type with the aim to act similarly to how JavaScript works.
// In some cases this requires casting from LxdVar to a specific type (e.g. foreach loop on a List<> wrapped in LxdVar).
public class LxdVar : IList<LxdVar>, IDictionary<string, LxdVar>, IEnumerable<KeyValuePair<string, LxdVar>>
{
private object _value;

    public char TypeLetter { get; private set; }
    public object Value
    {
        get => _value;
        private set
        {
            _value = value;
            TypeLetter = DetermineTypeLetter(_value);
        }
    }
    // Strict -- if true, an error is raised if attempting to assign this variable to a strictly different type.
    public bool Strict { get; set; }

    public LxdVar(object value, bool? strict = null)
    {
        if (value is LxdVar lxdVar)
        {
            _value = lxdVar.Value;
            TypeLetter = DetermineTypeLetter(_value);
            Strict = strict ?? lxdVar.Strict;  // inherit strictness if not specified (important)
        }
        else
        {
            _value = value;
            TypeLetter = DetermineTypeLetter(_value);
            Strict = strict ?? false;
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
    public static implicit operator LxdVar(double value) => new LxdVar(value);
    public static implicit operator LxdVar(int value) => new LxdVar(value);
    public static implicit operator LxdVar(float value) => new LxdVar(value);
    public static implicit operator LxdVar(bool value) => new LxdVar(value);
    public static implicit operator LxdVar(DateTime value) => new LxdVar(value);
    public static implicit operator LxdVar(Dictionary<string, LxdVar> value) => new LxdVar(value);
    public static implicit operator LxdVar(List<LxdVar> value) => new LxdVar(value);

    public static implicit operator string(LxdVar lxdVar) => lxdVar.As<string>();
    public static implicit operator double(LxdVar lxdVar) => lxdVar.As<double>();
    public static implicit operator int(LxdVar lxdVar) => lxdVar.As<int>();
    public static implicit operator float(LxdVar lxdVar) => lxdVar.As<float>();
    public static implicit operator bool(LxdVar lxdVar) => lxdVar.As<bool>();
    public static implicit operator DateTime(LxdVar lxdVar) => lxdVar.As<DateTime>();
    public static implicit operator Dictionary<string, LxdVar>(LxdVar lxdVar) => lxdVar.As<Dictionary<string, LxdVar>>();
    public static implicit operator List<LxdVar>(LxdVar lxdVar) => lxdVar.As<List<LxdVar>>();

    public override string ToString() => Value?.ToString() ?? "null";

    // Custom hash code and equality implementation
    public override bool Equals(object? obj)
    {
        if (obj is LxdVar other)
        {
            return Equals(Value, other.Value);
        }
        return Equals(Value, obj);
    }

    public override int GetHashCode()
    {
        return Value != null ? Value.GetHashCode() : 0;
    }

    #region IList<LxdVar> AND IDictionary<string, LxdVar> shared implementation

    private InvalidOperationException InvalidListOrDictionary()
    {
        if (Value is IList)
            return new InvalidOperationException("The list contains non-LxdVar elements.");
        else if (Value is IDictionary)
            return new InvalidOperationException("The dictionary contains non-LxdVar elements.");
        else
        {
            string type = Value?.GetType().ToString() ?? "null";
            return new InvalidOperationException($"{type} value is not a recognized list or dictionary");
        }
    }
    
    // IsReadOnly - IList, IDictionary
    public bool IsReadOnly => false;

    // Clear - IList, IDictionary
    public void Clear() {
        if (Value is IList<LxdVar> list)
            list.Clear();
        else if (Value is IDictionary<string, LxdVar> dictionary)
            dictionary.Clear();
        else throw InvalidListOrDictionary();
    }
    
    // Count - ICollection (IList, IDictionary)
    public int Count
    {
        get
        {
            if (Value is IList<LxdVar> list)
                return list.Count;
            else if (Value is IDictionary<string, LxdVar> dictionary)
                return dictionary.Count;
            else throw InvalidListOrDictionary();
        }
    }

    #endregion
    #region IList<LxdVar> implementation

    public LxdVar this[int index]
    {
        get => EnsureList()[index];
        set => EnsureList()[index] = value;
    }

    private IList<LxdVar> EnsureList()
    {
        if (Value is IList<LxdVar> list)
            return list;
        if (Value is IList)
            throw new InvalidOperationException("The list contains non-LxdVar elements.");

        string type = Value?.GetType().ToString() ?? "null";
        throw new InvalidOperationException($"{type} is not a recognised list");
    }

    public void Add(LxdVar item) => EnsureList().Add(item);

    public bool Contains(LxdVar item) => EnsureList().Contains(item);

    // CopyTo - ICollection (IList)
    public void CopyTo(LxdVar[] array, int arrayIndex) => EnsureList().CopyTo(array, arrayIndex);
    
    public int IndexOf(LxdVar item) => EnsureList().IndexOf(item);

    public void Insert(int index, LxdVar item) => EnsureList().Insert(index, item);

    public bool Remove(LxdVar item) => EnsureList().Remove(item);

    public void RemoveAt(int index) => EnsureList().RemoveAt(index);

    #endregion
    #region IDictionary<string, LxdVar> implementation

    public LxdVar this[string key]
    {
        get => EnsureDictionary()[key];
        set => EnsureDictionary()[key] = value;
    }

    private IDictionary<string, LxdVar> EnsureDictionary()
    {
        if (Value is IDictionary<string, LxdVar> dictionary)
            return dictionary;

        if (Value is IDictionary)
            throw new InvalidOperationException("The dictionary contains non-LxdVar elements.");

        throw new InvalidOperationException("Not a dictionary");
    }

    public ICollection<string> Keys => EnsureDictionary().Keys;

    public ICollection<LxdVar> Values => EnsureDictionary().Values;

    
    public void Add(string key, LxdVar value) => EnsureDictionary().Add(key, value);
    public void Add(KeyValuePair<string, LxdVar> item) => EnsureDictionary().Add(item.Key, item.Value);

    public bool ContainsKey(string key) => EnsureDictionary().ContainsKey(key);
    public bool Contains(KeyValuePair<string, LxdVar> item) => EnsureDictionary().Contains(item);

    public void CopyTo(KeyValuePair<string, LxdVar>[] array, int arrayIndex) => EnsureDictionary().CopyTo(array, arrayIndex);

    public bool Remove(string key) => EnsureDictionary().Remove(key);
    public bool Remove(KeyValuePair<string, LxdVar> item) => EnsureDictionary().Remove(item);

    public bool TryGetValue(string key, out LxdVar value)
    {
        bool result = EnsureDictionary().TryGetValue(key, out var tempValue);
        value = tempValue ?? throw new KeyNotFoundException(); // Assign a non-null value or handle null appropriately
        return result;
    }

    // Explicit implementation for IEnumerator<KeyValuePair<string, LxdVar>>
    IEnumerator<KeyValuePair<string, LxdVar>> IEnumerable<KeyValuePair<string, LxdVar>>.GetEnumerator() => EnsureDictionary().GetEnumerator();
    
    #endregion

    #region Custom Enumerator for IList<LxdVar>

    // Custom enumerator for IEnumerator<LxdVar>
    public IEnumerator<LxdVar> GetEnumerator()
    {
        return new LxdVarEnumerator(this);
    }
    
    // Explicit implementation of IEnumerable.GetEnumerator() for IEnumerator<LxdVar>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    // Custom enumerator ennumerates 'Value' within the LxdVar class.
    // It supports Dictionary, List and String type of Value.
    private class LxdVarEnumerator : IEnumerator<LxdVar>
    {
        private IEnumerator _innerEnumerator;

        public LxdVarEnumerator(LxdVar lxdVar)
        {
            if (lxdVar.Value is IList<LxdVar> list)
                _innerEnumerator = list.GetEnumerator();
            else if (lxdVar.Value is IDictionary<string, LxdVar> dictionary)
                _innerEnumerator = dictionary.Values.GetEnumerator();
            else if (lxdVar.Value is string str)
                _innerEnumerator = new StringEnumerator(str);
            else
                throw lxdVar.InvalidListOrDictionary();
        }

        public LxdVar Current => GetValueFromEnumerator(_innerEnumerator);

        object IEnumerator.Current => GetValueFromEnumerator(_innerEnumerator);

        public void Dispose()
        {
            if (_innerEnumerator is IDisposable disposable)
                disposable.Dispose();
        }

        public bool MoveNext() => _innerEnumerator.MoveNext();

        public void Reset() => _innerEnumerator.Reset();

        private LxdVar GetValueFromEnumerator(IEnumerator innerEnumerator)
        {
            return innerEnumerator switch
            {
                IEnumerator<LxdVar> enumerator => (LxdVar)enumerator.Current,
                CharEnumerator charEnumerator => new LxdVar(charEnumerator.Current.ToString()),
                _ => throw new InvalidOperationException("Unsupported enumerator type."),
            };
        }

        private class StringEnumerator : IEnumerator
        {
            private readonly string _str;
            private int _index;

            public StringEnumerator(string str)
            {
                _str = str;
                _index = -1;
            }

            public object Current => new LxdVar(_str[_index].ToString());

            public bool MoveNext()
            {
                _index++;
                return _index < _str.Length;
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }

    // Custom enumerator for KeyValuePair<string, LxdVar> as distinct from the above
    private class LxdVarKeyValuePairEnumerator : IEnumerator<KeyValuePair<string, LxdVar>>
    {
        private IEnumerator<KeyValuePair<string, LxdVar>> _innerEnumerator;

        public LxdVarKeyValuePairEnumerator(IDictionary<string, LxdVar> dictionary)
        {
            _innerEnumerator = dictionary.GetEnumerator();
        }

        public KeyValuePair<string, LxdVar> Current => _innerEnumerator.Current;

        object IEnumerator.Current => _innerEnumerator.Current;

        public void Dispose()
        {
            _innerEnumerator.Dispose();
        }

        public bool MoveNext()
        {
            return _innerEnumerator.MoveNext();
        }

        public void Reset()
        {
            _innerEnumerator.Reset();
        }
    }

    #endregion
}

public static class LXD
{
    private const char EncodeMark = '〻';   // U+303B
    private const char RecordStart = '╾';   // U+257E
    private const char RecordEnd = '╼';     // U+257C
    private const char FieldDelimiter = '╽';// U+257D
    private const char KeyValueSeparator = '꞉'; // U+A789
    public static bool Debug = false;
    public static string Serialize<T>(T value, bool? debug = null) where T : notnull
    {
        StringBuilder sb = new StringBuilder();
        SerializeValue(new LxdVar(value), sb); // Serialize with LxdVar constructor
        string result = sb.ToString();
        if (debug != null ? debug == true : Debug == true) Console.WriteLine($"•> {result}");
        return result;
    }

    private static void SerializeValue<T>(T value, StringBuilder sb)
    {
        if (value is LxdVar lxdVar)
        {
            SerializeValue(lxdVar.Value, sb); // Unwrap and serialize LxdVar's inner value
            return;
        }
        else switch (value)
        {
            case string s: SerializeString(s, sb); break;
            case double d: SerializeNumber(d, sb); break;
            case int i: SerializeNumber(i, sb); break;
            case float f: SerializeNumber(f, sb); break;
            case bool b: SerializeBool(b, sb); break;
            case DateTime dt: SerializeDateTime(dt, sb); break;
            case IList list: SerializeList(list, sb); break;
            case IDictionary<string, object> dict: SerializeDictionary(dict, sb); break;
            case IDictionary<string, LxdVar> lxdDict: SerializeLxdDictionary(lxdDict, sb); break;
            default:
                string type = value?.GetType().ToString() ?? "null";
                throw new NotSupportedException($"Unsupported type: {type}");
        }
    }

    private static void SerializeLxdDictionary(IDictionary<string, LxdVar> lxdDict, StringBuilder sb)
    {
        SerializeDictionary(lxdDict.ToDictionary(kv => kv.Key, kv => kv.Value.Value), sb); // Ensure inner value is serialized
    }

    private static void SerializeDictionary(IDictionary<string, object> dict, StringBuilder sb)
    {
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
    }

    private static void SerializeList(IList list, StringBuilder sb)
    {
        sb.Append('L');
        sb.Append(RecordStart);
        foreach (var item in list)
        {
            SerializeValue(item, sb);
            sb.Append(FieldDelimiter);
        }
        sb.Append(RecordEnd);
    }

    private static void SerializeDateTime(DateTime dt, StringBuilder sb)
    {
        sb.Append('d');
        sb.Append(dt.ToString("O", CultureInfo.InvariantCulture));
    }

    private static void SerializeBool(bool b, StringBuilder sb)
    {
        sb.Append('b');
        sb.Append(b ? "true" : "false");
    }

    private static void SerializeNumber(object n, StringBuilder sb)
    {
        switch (n)
        {
            case double d:
                sb.Append('n');
                sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
                break;
            case float f:
                sb.Append('n');
                sb.Append(f.ToString("G9", CultureInfo.InvariantCulture));
                break;
            case int i:
                sb.Append('n');
                sb.Append(i.ToString("G17", CultureInfo.InvariantCulture));
                break;
            default:
                string type = n?.GetType().ToString() ?? "null";
                throw new NotSupportedException($"Unsupported type: {type}");
        }
    }

    
    private static void SerializeString(string s, StringBuilder sb)
    {
        sb.Append('s');
        sb.Append(EscapeString(s));
    }



    public static LxdVar Deserialize(string input, bool? debug = null)
    {
        if (debug != null ? debug == true : Debug == true) Console.WriteLine($"•<- {input}");
        var index = 0;
        return DeserializeValue(typeof(LxdVar), input, ref index);
    }


    private static LxdVar DeserializeValue(Type expectedType, string input, ref int index)
    {
        switch (input[index])
        {
            case 's':
                index++; // Skip type letter prefix
                return new LxdVar(UnescapeString(ReadUntil(input, ref index, FieldDelimiter, RecordEnd)));
            case 'n':
                index++; // Skip type letter prefix
                double doubleValue = double.Parse(ReadUntil(input, ref index, FieldDelimiter, RecordEnd), CultureInfo.InvariantCulture);
                object value;
                // Check if it should be converted back to int or float
                if (Math.Abs(doubleValue % 1) < double.Epsilon * 100)
                {
                    value = (int)doubleValue; // Convert to int if effectively an integer
                }
                else
                {
                    value = (float)doubleValue; // Otherwise, keep it as float
                }
                return new LxdVar(value);
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
                    var value = DeserializeValue(typeof(LxdVar), input, ref index);
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

    // Escape control characters using their hex representations -- used to make data distinct from controls.
    private static string EscapeString(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
        {
            if (c == RecordStart || c == RecordEnd || c == FieldDelimiter || c == KeyValueSeparator)
            {
                sb.AppendFormat("\\u{0:X4}", (int)c);
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString()
                 .Replace("\\", "\\\\")
                 .Replace("\r", "\\r")
                 .Replace("\n", "\\n")
                 .Replace("\t", "\\t")
                 .Replace("'", "\\'")
                 .Replace("\"", "\\\"");
    }

    private static string UnescapeString(string s)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && s[i + 1] == 'u')
            {
                var hex = s.Substring(i + 2, 4);
                sb.Append((char)Convert.ToInt32(hex, 16));
                i += 5; // Skip over the escape sequence
            }
            else if (s[i] == '\\' && s.Length > i + 1)
            {
                switch (s[i + 1])
                {
                    case '\\': sb.Append('\\'); i++; break;
                    case 'r': sb.Append('\r'); i++; break;
                    case 'n': sb.Append('\n'); i++; break;
                    case 't': sb.Append('\t'); i++; break;
                    case '\'': sb.Append('\''); i++; break;
                    case '"': sb.Append('"'); i++; break;
                    default: sb.Append(s[i]); break;
                }
            }
            else
            {
                sb.Append(s[i]);
            }
        }
        return sb.ToString();
    }
}

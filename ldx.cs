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
    private const char RecordStart = '╾';   // U+257E
    private const char RecordEnd = '╼';     // U+257C
    private const char FieldDelimiter = '╽';// U+257D
    private const char KeyValueSeparator = '꞉'; // U+A789
    public static bool debug = false;
    public static string Serialize<T>(T value) where T : notnull
    {
        StringBuilder sb = new StringBuilder();
        SerializeValue(new LxdVar(value), sb); // Serialize with LxdVar constructor
        string result = sb.ToString();
        if (debug) Console.WriteLine($"•> {result}");
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

    public static LxdVar Deserialize(string input)
    {
        if (debug) Console.WriteLine($"•<- {input}");
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

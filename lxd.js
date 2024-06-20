/*
  LXD (Lightweight Exchange of Data) Format:
  See ReadMe.md (alias !lxd.md)
  Author: Andrew Kingdom 2024
  License: MIT
*/

// Put LXD in global space so it can be accessed directly (LDX === window.LXD).
window.LXD = (() => {
    // private:

    // Markers
    RecordStart = '\u257E';       // U+257E BOX DRAWINGS LIGHT LEFT
    RecordEnd = '\u257C';         // U+257C BOX DRAWINGS LIGHT LEFT AND RIGHT
    FieldDelimiter = '\u257D';    // U+257D BOX DRAWINGS LIGHT UP
    KeyValueSeparator = '\uA789'; // U+A789 MODIFIER LETTER COLON

    const specialChars = [RecordStart, RecordEnd, FieldDelimiter, KeyValueSeparator];
    const specialCharsMap = {
        '\u257E': '\\u257E',
        '\u257C': '\\u257C',
        '\u257D': '\\u257D',
        '\uA789': '\\uA789'
    };

    function serialize(value) {
        return serializeValue(value);
    }

    function serializeValue(value) {
        switch (typeof value) {
            case 'string':
                return `s${escapeString(value)}`;
            case 'number':
                return Number.isInteger(value) ? `i${value}` : `f${value}`;
            case 'boolean':
                return `b${value}`;
            case 'object':
                if (value instanceof Date) {
                    return `d${value.toISOString()}`;
                } else if (Array.isArray(value)) {
                    return `L${RecordStart}${value.map(serializeValue).join(FieldDelimiter)}${RecordEnd}`;
                } else if (value !== null && typeof value === 'object') {
                    return `D${RecordStart}${Object.entries(value).map(([k, v]) => `${escapeString(k)}${KeyValueSeparator}${serializeValue(v)}`).join(FieldDelimiter)}${RecordEnd}`;
                } else {
                    throw new Error(`Unsupported type: ${typeof value}`);
                }
            default:
                throw new Error(`Unsupported type: ${typeof value}`);
        }
    }

    function deserialize(input) {
        let index = 0;
        return deserializeValue(input, index).value;
    }

    function deserializeValue(input, index) {
        const typeLetter = input[index++];
        switch (typeLetter) {
            case 's':
                const strData = readUntil(input, index, FieldDelimiter, RecordEnd);
                const str = unescapeString(strData.value);
                index = strData.newIndex;
                return { value: str, newIndex: index };
            case 'i':
                const intData = readUntil(input, index, FieldDelimiter, RecordEnd);
                const intValue = parseInt(unescapeString(intData.value), 10);  // base-10
                index = intData.newIndex;
                return { value: intValue, newIndex: index };
            case 'f':
                const floatData = readUntil(input, index, FieldDelimiter, RecordEnd);
                const floatValue = parseFloat(unescapeString(floatData.value));
                index = floatData.newIndex;
                return { value: floatValue, newIndex: index };
            case 'b':
                const boolData = readUntil(input, index, FieldDelimiter, RecordEnd);
                const boolValue = unescapeString(boolData.value) === "true";
                index = boolData.newIndex;
                return { value: boolValue, newIndex: index };
            case 'd':
                const dateData = readUntil(input, index, FieldDelimiter, RecordEnd);
                const dateValue = new Date(unescapeString(dateData.value));
                index = dateData.newIndex;
                return { value: dateValue, newIndex: index };
            case 'D':
                let dictionary = {};
                if (input[index] !== RecordStart) {
                    throw new Error("Invalid format: RecordStart expected");
                }
                index++;  // Skip RecordStart, point to first data char (or RecordEnd)
                while (index < input.length && input[index] !== RecordEnd) {
                    // Get key
                    const { value: escapedKey, newIndex: nextIndex } = readUntil(input, index, KeyValueSeparator);
                    index = nextIndex;
                    index++;  // Skip KeyValueSeparator, point to value type letter
                    const key = unescapeString(escapedKey);
                    // Get value
                    let valueField = deserializeValue(input, index);
                    dictionary[key] = valueField.value;
                    index = valueField.newIndex; // Update index to newIndex after deserialization
                    // Skip FieldDelimiter, point to next field (if present)
                    if (input[index] === FieldDelimiter) {
                        index++;
                    }
                }
                if (index >= input.length) {
                    throw new Error("Invalid format: RecordEnd expected");
                }
                index++;  // Skip RecordEnd
                return { value: dictionary, newIndex: index };
            case 'L':
                let list = [];
                if (input[index] !== RecordStart) {
                    throw new Error("Invalid format: RecordStart expected");
                }
                index++; // Skip RecordStart, point to first data char (or RecordEnd)
                while (index < input.length && input[index] !== RecordEnd) {
                    let valueField = deserializeValue(input, index);
                    list.push(valueField.value);
                    index = valueField.newIndex;
                    if (input[index] === FieldDelimiter) {
                        index++;
                    }
                }
                if (index >= input.length) {
                    throw new Error("Invalid format: RecordEnd expected");
                }
                index++;  // point past RecordEnd
                return { value: list, newIndex: index };
            default:
                throw new Error(`Invalid format: Unsupported type letter: ${typeLetter}`);
        }
    }

    function readUntil(input, index, ...delimiters) {
        const start = index;
        while (index < input.length && !delimiters.includes(input[index])) {
            index++;
        }
        return { value: input.substring(start, index), newIndex: index };
    }

    // Escape control characters using their hex representations -- used to make data distinct from controls.
    function escapeString(str) {
        return str.replace(/[\u257E\u257C\u257D\uA789]/g, char => specialCharsMap[char])
            .replace(/\\/g, '\\\\')
            .replace(/\r/g, '\\r')
            .replace(/\n/g, '\\n')
            .replace(/\t/g, '\\t')
            .replace(/'/g, "\\'")
            .replace(/"/g, '\\"');
    }

    // Unescape unicode, hex and common escape sequences -- used to restore escaped data.
    function unescapeString(str) {
        if (typeof str !== 'string') {
            throw new TypeError(`Input must be a string (not ${typeof str})`);
        }
        return str.replace(/\\u257E/g, RecordStart)
            .replace(/\\u257C/g, RecordEnd)
            .replace(/\\u257D/g, FieldDelimiter)
            .replace(/\\uA789/g, KeyValueSeparator)
            .replace(/\\\\/g, '\\')
            .replace(/\\r/g, '\r')
            .replace(/\\n/g, '\n')
            .replace(/\\t/g, '\t')
            .replace(/\\'/g, "'")
            .replace(/\\"/g, '"')
            .replace(/\\x([0-9A-Fa-f]{2})/g, (match, p1) => String.fromCharCode(parseInt(p1, 16)))
            .replace(/\\u([0-9A-Fa-f]{4})/g, (match, p1) => String.fromCharCode(parseInt(p1, 16)));
    }

    // public:
    return { serialize, deserialize };
})();

// Export the LXD module for Node.js or CommonJS environments
//if (typeof module !== 'undefined' && module.exports) {
//    module.exports = LXD;
//}

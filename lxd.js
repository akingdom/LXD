/*
  LXD (Lightweight Exchange of Data) Format:
  See ReadMe.md (alias !lxd.md)
  Author: Andrew Kingdom 2024
  License: MIT
*/

window.LXD = {  // put LXD in global space so it can be accessed directly (LDX === window.LXD).

    MetaEnd: '\u2B1A',           // U+2B1A BLACK LARGE SQUARE
    RecordStart: '\u257E',       // U+257E BOX DRAWINGS LIGHT LEFT
    RecordEnd: '\u257C',         // U+257C BOX DRAWINGS LIGHT LEFT AND RIGHT
    FieldDelimiter: '\u257D',    // U+257D BOX DRAWINGS LIGHT UP
    KeyValueSeparator: '\uA789', // U+A789 MODIFIER LETTER COLON

    // Escape control characters using their hex representations -- used to make data distinct from controls.
    escapeString: function (str) {
        return str.replace(this.lxdSpecialCharsPattern, char => {
            switch (char) {
                case this.MetaEnd:
                    return '\\u2B1A';
                case this.RecordStart:
                    return '\\u257E';
                case this.RecordEnd:
                    return '\\u257C';
                case this.FieldDelimiter:
                    return '\\u257D';
                case this.KeyValueSeparator:
                    return '\\uA789';
                default:
                    return char;
            }
        }).replace(/\\/g, '\\\\')
            .replace(/\r/g, '\\r')
            .replace(/\n/g, '\\n')
            .replace(/\t/g, '\\t')
            .replace(/'/g, "\\'")
            .replace(/"/g, '\\"');
    },

    // Unescape hex and common escape sequences -- used to restore escaped data.
    unescapeString: function (str) {
        if (typeof str !== 'string') {
            throw new TypeError('Input must be a string');
        }
        return str.replace(/\\u2B1A/g, this.MetaEnd)
            .replace(/\\u257E/g, this.RecordStart)
            .replace(/\\u257C/g, this.RecordEnd)
            .replace(/\\u257D/g, this.FieldDelimiter)
            .replace(/\\uA789/g, this.KeyValueSeparator)
            .replace(/\\\\/g, '\\')
            .replace(/\\r/g, '\r')
            .replace(/\\n/g, '\n')
            .replace(/\\t/g, '\t')
            .replace(/\\'/g, "'")
            .replace(/\\"/g, '"')
            .replace(/\\x([0-9A-Fa-f]{2})/g, (match, p1) => String.fromCharCode(parseInt(p1, 16)))
            .replace(/\\u([0-9A-Fa-f]{4})/g, (match, p1) => String.fromCharCode(parseInt(p1, 16)));
    },

    serialize: function (data) {
        let result = this.serializeDictionary(data);
        return result;
    },

    serializeDictionary: function (dict) {
        let result = 'D' + this.RecordStart; // Type letter prefix for dictionary
        const entries = Object.entries(dict);
        for (let i = 0; i < entries.length; i++) {
            const [key, value] = entries[i];
            result += `${this.escapeString(key)}${this.KeyValueSeparator}${this.serializeValue(value)}`;
            if (i < entries.length - 1) {
                result += this.FieldDelimiter;
            }
        }
        result += this.RecordEnd;
        return result;
    },

    serializeValue: function (value) {
        if (typeof value === 'string') {
            return 's' + this.escapeString(value); // Type letter prefix for string
        } else if (typeof value === 'number') {
            if (Number.isInteger(value)) {
                return 'i' + value;
            } else {
                return 'f' + value;
            }
        } else if (typeof value === 'boolean') {
            return 'b' + (value ? 'true' : 'false');
        } else if (value instanceof Date) {
            return 'd' + value.toISOString(); // ISO 8601 format
        } else if (Array.isArray(value)) {
            return this.serializeList(value);
        } else if (typeof value === 'object') {
            return this.serializeDictionary(value);
        } else {
            throw new Error(`Unsupported type: ${typeof value}`);
        }
    },

    serializeList: function (list) {
        let result = 'L' + this.RecordStart; // Type letter prefix for list
        for (let item of list) {
            result += `${this.serializeValue(item)}${this.FieldDelimiter}`;
        }
        result += this.RecordEnd;
        return result;
    },

    deserialize: function (input) {
        debugger;
        let index = 0;
        let result = this.deserializeValue(input, index);
        //let index = result.newIndex; -- we don't need to update index at the root data level 
        return result.value;
    },

    deserializeValue: function (input, index) {
        let result, str; // Define result and str once outside the switch statement
        switch (input[index]) {
            // Primatives...
            case 's':
                index++; // Skip type letter prefix
                result = this.readUntil(input, index, this.FieldDelimiter, this.RecordEnd);
                str = result.value;
                index = result.newIndex;
                return { value: this.unescapeString(str), newIndex: index };
            case 'i':
                index++; // Skip type letter prefix
                result = this.readUntil(input, index, this.FieldDelimiter, this.RecordEnd);
                str = result.value;
                index = result.newIndex;
                return { value: parseInt(str), newIndex: index };
            case 'f':
                index++; // Skip type letter prefix
                result = this.readUntil(input, index, this.FieldDelimiter, this.RecordEnd);
                str = result.value;
                index = result.newIndex;
                return { value: parseFloat(str), newIndex: index };
            case 'b':
                index++; // Skip type letter prefix
                result = this.readUntil(input, index, this.FieldDelimiter, this.RecordEnd);
                str = result.value;
                index = result.newIndex;
                return { value: str === "true", newIndex: index };
            case 'd':
                index++; // Skip type letter prefix
                result = this.readUntil(input, index, this.FieldDelimiter, this.RecordEnd);
                str = result.value;
                index = result.newIndex;
                return { value: new Date(str), newIndex: index };
            // Objects...
            case 'D':
                return this.deserializeDictionary(input, index); // Increment index for dictionary
            case 'L':
                return this.deserializeList(input, index); // Increment index for list
            default:
                throw new Error("Invalid format: Unknown type letter prefix");
        }
    },

    deserializeDictionary: function (input, index) {
        if (input[index] !== 'D') // Check type letter prefix
            throw new Error("Invalid format: Type letter prefix for dictionary expected");

        index++; // point to RecordStart char
        let result = {};
        if (input[index] !== this.RecordStart)
            throw new Error("Invalid format: RecordStart expected");

        index++;  // point to first data char (or RecordEnd)
        while (index < input.length && input[index] !== this.RecordEnd) {
            // Get key
            let { value: key, newIndex: nextIndex } = this.readUntil(input, index, this.KeyValueSeparator);
            index = nextIndex + 1;
            // Get value
            let { value, newIndex } = this.deserializeValue(input, index);
            result[key] = value;
            index = newIndex; // Update index to newIndex after deserialization
            // Next field (if present)
            if (input[index] === this.FieldDelimiter)
                index++;
        }
        if (index >= input.length)
            throw new Error("Invalid format: RecordEnd expected");

        index++;  // point past RecordEnd
        return { value: result, newIndex: index };
    },

    deserializeList: function (input, index) {
        if (input[index] !== 'L') // Check type letter prefix
            throw new Error("Invalid format: Type letter prefix for list expected");

        index++; // point to RecordStart char
        let result = [];
        if (input[index] !== this.RecordStart)
            throw new Error("Invalid format: RecordStart expected");

        index++;
        while (index < input.length && input[index] !== this.RecordEnd) {
            let { value, newIndex } = this.deserializeValue(input, index);
            result.push(value);
            index = newIndex;
            if (input[index] === this.FieldDelimiter)
                index++;
        }
        if (index >= input.length)
            throw new Error("Invalid format: RecordEnd expected");

        index++;  // point past RecordEnd
        return { value: result, newIndex: index };
    },

    readUntil: function (input, index, ...delimiters) {
        let start = index;
        while (index < input.length && !delimiters.includes(input[index])) {
            index++;
        }
        return { value: input.substring(start, index), newIndex: index };
    }
};

// Calculate specialChars and lxdSpecialCharsPattern once and store them
window.LXD.specialChars = [window.LXD.MetaEnd, window.LXD.RecordStart, window.LXD.RecordEnd, window.LXD.FieldDelimiter, window.LXD.KeyValueSeparator].join('');
window.LXD.lxdSpecialCharsPattern = new RegExp(`[${window.LXD.specialChars}]`, 'g');

// Export the LXD module for Node.js or CommonJS environments
//if (typeof module !== 'undefined' && module.exports) {
//    module.exports = LXD;
//}

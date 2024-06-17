## LXD (Lightweight Exchange of Data) Format Specification
LXD ('lexed') -- an alternative data transport format to JSON -- includes code.
  
### Introduction

LXD (Lightweight Exchange of Data) is a compact and hierarchical serialization format designed for the efficient exchange of structured data in UTF-8 text format. Unlike JSON, LXD is not intended for human readability but serves as a streamlined communication protocol between clients and servers. It utilizes uncommon Unicode characters for delimiters and incorporates type prefixes to clearly specify data types.
  
### Data Types

LXD supports a variety of data types, each with a specific prefix:

Type | Prefix | Example | Note
---- | ------ | ------- | ----
**String** | s | sHello World | 
**Integer** | i | i123 | 
**Float** | f | f3.14 | 
**Boolean** | b | btrue | (values `true` or `false`)
**Date** | d | d2024-06-16T00:00:00Z | in ISO 8601 format
**Dictionary** | D | D╾key꞉svalue╼ | with key-value pairs enclosed in record delimiters
**List** | L | L╾selement1╽selement2╼ | with elements enclosed in record delimiters

Keys in dictionaries are always untyped strings.

### Special Delimiters

LXD uses specific Unicode characters as delimiters to structure the data:

- **Record Start**: `╾` (U+257E)
- **Record End**: `╼` (U+257C)
- **Field Delimiter**: `╽` (U+257D)
- **Key/Value Separator**: `꞉` (U+A789)

### Escape Sequences

To ensure data integrity, all delimiter characters encountered during encoding are escaped using the `\uXXXX` format. These escape sequences are reversed during the final decoding of fields.

### Optional Meta Structure

LXD supports an optional extensible meta structure for defining types. This structure can be sent as the initial data set to establish type definitions.
  
#### Meta Example

LXD Representation:
```text
  D╾LXD꞉D╾type꞉D╾String꞉ss╽Int꞉si╽Float꞉sf╽Boolean꞉sb╽Date꞉sd╽Dictionary꞉sD╽List꞉sL╼╼╼
```
  JSON Equivalent:
```json
  {
      "LXD": {
          "type": {
              "String": "s",
              "Int": "i",
              "Float": "f",
              "Boolean": "b",
              "Date": "d",
              "Dictionary" : "D",
              "List" : "L"
          }
      }
  }
```
#### Data Examples

LXD Representation:
```text
  D\u257Edata\uA789D\u257Ekey\uA789svalue\u257C\u257C
```
JSON Equivalent:
```json
  {
      "data": {
          "key": "value"
      }
  }
```
LXD Representation:
```text
  sThis is just some text.
```
  JSON Equivalent: 
```json
  "This is just some text."
```

### Comparison with JSON

Feature |	JSON |	LXD
------- | ---- | ----
Readability | Human-readable | Not human-readable
Type Information | Inferred or requires additional context | Explicitly included with type prefixes
Delimiters | Common characters (e.g., {, }, [, ], :, ,) | Uncommon Unicode characters (e.g., ╾, ╼, ╽, ꞉)
Escape Sequences | Requires escaping of quotes, backslashes, and control characters | Uses \uXXXX format for escaping delimiters
Structure | Arrays and objects | Lists and dictionaries
Date Format | ISO 8601 (string) | ISO 8601 with prefix d
Compactness | More verbose due to brackets and commas | More compact due to specialized delimiters

### Advantages of LXD over JSON

- **Explicit Typing**: LXD includes type prefixes, reducing ambiguity and the need for additional type information or context.
- **Compact Format**: By using specialized delimiters and omitting common JSON punctuation, LXD can achieve a more compact representation.
- **Efficient Parsing**: The use of uncommon Unicode characters as delimiters reduces the likelihood of conflicts with data content, simplifying parsing.
- **Flexible Meta Structure**: LXD's extensible meta structure allows for the definition of custom types and schemas, enhancing flexibility.

### Best Practices

#### Consistent Handling
Ensure consistent handling of escape sequences during serialization and deserialization to maintain data integrity.

### Type Definitions
Use the optional meta structure to define types at the beginning of communication for clarity and consistency.

Conclusion
LXD offers a streamlined, text-oriented alternative to JSON for structured data exchange. Its use of uncommon Unicode characters and explicit type prefixes makes it a robust choice for efficient client-server communication. By following best practices for handling escape sequences and defining types, LXD can be an effective tool for structured data serialization.

--
  Author: Andrew Kingdom 2024
  License: MIT

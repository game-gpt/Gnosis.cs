Wasm Interface Object Notation
==============================

WION is a data interchange format for WebAssembly. It is designed to be a simple, human-readable, and easy-to-write
format that can be used to describe the data types and interfaces of WebAssembly modules.

| Type     | Values                           |
|----------|----------------------------------|
| Bools    | `true`, `false`                  |
| Number   | `42`, `-0`, `3.14` , `0xBeef`    |
| Strings  | `"abc\t123"`, `'x'`, `'\u{0}'`   |
| Sequence | `("abc", 123)`, `[1, 2, 3]`      |
| Records  | `{field-a: 1, field-b: "b"}`     |
| Options  | `T`, `some(T)`, `none`           |
| Results  | `T`, `success(T)`, `failure(E)`  |
| Variants | `tag, tag(data)`, `tag { data }` |
| Flags    | `+[read, write]`, `-[execute]`   |

## Details

### Number

- integer: `123`, `-9`
- decimal: `3.14`, `6.022e+23`
- byte: `0xBeef`, `0b1010_1010`


### String 

- escaped: `\n`, `\u{0}`
- raw: `r"abc\t123"`
- single: `'x'`
- multi: `'''abc\n123'''`


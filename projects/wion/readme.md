Wasm Interface Object Notation
==============================

WION is a data interchange format for WebAssembly. It is designed to be a simple, human-readable, and easy-to-write
format that can be used to describe the data types and interfaces of WebAssembly modules.

| Type     | Values                           |
|----------|----------------------------------|
| Bools    | `true`, `false`                  |
| Number   | `42`, `-0`, `3.14` , `0xBeef`    |
| Strings  | `"abc\t123"`, `'x'`, `'\u{0}'`   |
| Sequence | `[1, 2, 3]`                      |
| Records  | `{field-a: 1, field-b: "b"}`     |
| Options  | `T`, `some(T)`, `none`           |
| Results  | `T`, `success(T)`, `failure(E)`  |
| Variants | `tag, tag(data)`, `tag { data }` |
| Flags    | `+[read, write]`, `-[execute]`   |

```wion
{
    namespace:package/module_function@2024.2.4-semver = "function-name",
    primitive = [
        true, false, 0, 0xBeef, 0b1010_1010, 3.14, 6.022e+23,
        "string", 'multi\nline', 
    ],
    options = [0, some(0), none]
    results = [0, fine(0), fail(0)]
    records = {
        field: true
    }
    variant1 = variant()
    variant2 = variant(true)
    variant3 = variant(namespace:package/anonymous {
        field: true
    })
    // nothing + read + write
    flags1 = +[read, write]
    // everything - execute
    flags2 = -[execute]
}
```

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


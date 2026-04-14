use std::sync::Arc;

mod convert;
#[cfg(feature = "serde")]
mod der;
mod display;
#[cfg(feature = "serde")]
mod ser;

/// https://github.com/WebAssembly/component-model/blob/main/design/mvp/WIT.md
pub enum WasiValue {
    /// The boolean value `true` or `false`
    Boolean(bool),
    /// The 8-bit unsigned integer from `0` to `255`
    Unsigned8(u8),
    /// The 16-bit unsigned integer from `0` to `65535`
    Unsigned16(u16),
    /// The 32-bit unsigned integer from `0` to `4294967295`
    Unsigned32(u32),
    /// The 64-bit unsigned integer from `0` to `18446744073709551615`
    Unsigned64(u64),
    /// The 8-bit signed integer from `-128` to `127`
    Integer8(i8),
    /// The 16-bit signed integer from `-32768` to `32767`
    Integer16(i16),
    /// The 32-bit signed integer from `-2147483648` to `2147483647`
    Integer32(i32),
    /// The 64-bit signed integer from `-9223372036854775808` to `9223372036854775807`
    Integer64(i64),
    /// The 32-bit floating point number
    Float32(f32),
    /// The 64-bit floating point number
    Float64(f64),
    /// The UTF-8 character
    Unicode(char),
    /// The UTF-8 string
    UTF8(Arc<str>),
    /// Compact binary data
    Buffer(Arc<[u8]>),
    /// Record value
    Object(Arc<WasiObject>),
}

/// ```wion
/// record {a: value, b: value}
/// (a: value, b: value)
/// variant(value)
/// [0: value, 1: value]
/// ```
#[derive(Debug)]
pub struct WasiObject {
    pub kind: WasiObjectKind,
    pub terms: Vec<WasiValue>,
}

#[derive(Clone, Debug)]
pub enum WasiObjectKind {
    /// `{a: value, b: value}`
    Record { typing: Arc<str> },
    /// `-[read, "write"]`
    /// `+[read, "write"]`
    Flags {
        /// the flag typing
        typing: Arc<str>
    },
    /// `[0: value, 1: value]`
    List,
    /// `variant(value)`
    Variant {
        /// The name of the variant
        typing: Arc<str>,
    },
    /// `(a: value, b: value)`
    Tuple,
}


#[derive(Debug)]
pub struct WasiPair {
    /// The key of the wasi pair, can be a string or a number or nothing
    pub key: Arc<str>,
    /// The value of the wasi pair
    pub value: WasiValue,
}

impl From<WasiObject> for WasiValue {
    fn from(value: WasiObject) -> Self {
        Self::Object(Arc::new(value))
    }
}

impl WasiObject {
    /// Create a new record
    pub fn none() -> Self {
        Self { kind: WasiObjectKind::Variant { typing: Arc::from("none") }, terms: vec![] }
    }
    /// Create a new record
    pub fn unit() -> Self {
        Self { kind: WasiObjectKind::Tuple {}, terms: vec![] }
    }
    pub fn variant<N, I>(name: N, values: I) -> Self
    where
        N: Into<Arc<str>>,
        I: IntoIterator<Item = WasiValue>,
    {
        Self { kind: WasiObjectKind::Variant { typing: name.into() }, terms: values.into_iter().collect() }
    }
}

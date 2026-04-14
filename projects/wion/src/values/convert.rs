use super::*;

impl From<u8> for WasiValue {
    fn from(value: u8) -> Self {
        Self::Unsigned8(value)
    }
}

impl From<u16> for WasiValue {
    fn from(value: u16) -> Self {
        Self::Unsigned16(value)
    }
}

impl From<u32> for WasiValue {
    fn from(value: u32) -> Self {
        Self::Unsigned32(value)
    }
}

impl From<u64> for WasiValue {
    fn from(value: u64) -> Self {
        Self::Unsigned64(value)
    }
}

impl From<i8> for WasiValue {
    fn from(value: i8) -> Self {
        Self::Integer8(value)
    }
}

impl From<i16> for WasiValue {
    fn from(value: i16) -> Self {
        Self::Integer16(value)
    }
}

impl From<i32> for WasiValue {
    fn from(value: i32) -> Self {
        Self::Integer32(value)
    }
}

impl From<i64> for WasiValue {
    fn from(value: i64) -> Self {
        Self::Integer64(value)
    }
}

impl From<f32> for WasiValue {
    fn from(value: f32) -> Self {
        Self::Float32(value)
    }
}

impl From<f64> for WasiValue {
    fn from(value: f64) -> Self {
        Self::Float64(value)
    }
}

impl WasiValue {
    /// Check if the value is a boolean
    pub fn is_bool(&self) -> bool {
        self.as_bool().is_some()
    }
    /// Check if the value is an 8-bit unsigned integer
    pub fn is_u8(&self) -> bool {
        self.as_u8().is_some()
    }
    /// Check if the value is a 16-bit unsigned integer
    pub fn is_u16(&self) -> bool {
        self.as_u16().is_some()
    }
    /// Check if the value is a 32-bit unsigned integer
    pub fn is_u32(&self) -> bool {
        self.as_u32().is_some()
    }
    /// Check if the value is a 64-bit unsigned integer
    pub fn is_u64(&self) -> bool {
        self.as_u64().is_some()
    }
    /// Check if the value is an 8-bit signed integer
    pub fn is_i8(&self) -> bool {
        self.as_i8().is_some()
    }
    /// Check if the value is a 16-bit signed integer
    pub fn is_i16(&self) -> bool {
        self.as_i16().is_some()
    }
    /// Check if the value is a 32-bit signed integer
    pub fn is_i32(&self) -> bool {
        self.as_i32().is_some()
    }
    /// Check if the value is a 64-bit signed integer
    pub fn is_i64(&self) -> bool {
        self.as_i64().is_some()
    }
    /// Check if the value is a 32-bit floating point number
    pub fn is_f32(&self) -> bool {
        self.as_f32().is_some()
    }
    /// Check if the value is a 64-bit floating point number
    pub fn is_f64(&self) -> bool {
        self.as_f64().is_some()
    }
    /// Check if the value is a unicode character
    pub fn is_unicode(&self) -> bool {
        self.as_unicode().is_some()
    }
    /// Check if the value is a UTF-8 string
    pub fn is_utf8(&self) -> bool {
        self.as_utf8().is_some()
    }
    /// Check if the value is a record
    pub fn is_structure(&self) -> bool {
        self.as_structure().is_some()
    }
}
impl WasiValue {
    /// Try to convert the value to a boolean
    pub fn as_bool(&self) -> Option<bool> {
        match self {
            Self::Boolean(v) => Some(*v),
            _ => None,
        }
    }
    /// Try to convert the value to an 8-bit unsigned integer
    pub fn as_u8(&self) -> Option<u8> {
        match self {
            Self::Unsigned8(v) => Some(*v),
            _ => None,
        }
    }
    /// Try to convert the value to a 16-bit unsigned integer
    pub fn as_u16(&self) -> Option<u16> {
        match self {
            Self::Unsigned16(v) => Some(*v),
            _ => None,
        }
    }
    /// Try to convert the value to a 32-bit unsigned integer
    pub fn as_u32(&self) -> Option<u32> {
        match self {
            Self::Unsigned32(v) => Some(*v),
            _ => None,
        }
    }
    /// Try to convert the value to a 64-bit unsigned integer
    pub fn as_u64(&self) -> Option<u64> {
        match self {
            Self::Unsigned64(v) => Some(*v),
            _ => None,
        }
    }
    /// Try to convert the value to an 8-bit signed integer
    pub fn as_i8(&self) -> Option<i8> {
        match self {
            Self::Integer8(v) => Some(*v),
            _ => None,
        }
    }
    /// Try to convert the value to a 16-bit signed integer
    pub fn as_i16(&self) -> Option<i16> {
        match self {
            Self::Integer16(v) => Some(*v),
            _ => None,
        }
    }
    /// Try to convert the value to a 32-bit signed integer
    pub fn as_i32(&self) -> Option<i32> {
        match self {
            Self::Integer32(v) => Some(*v),
            _ => None,
        }
    }
    /// Try to convert the value to a 64-bit signed integer
    pub fn as_i64(&self) -> Option<i64> {
        match self {
            Self::Integer64(v) => Some(*v),
            _ => None,
        }
    }
    /// Try to convert the value to a 32-bit floating point number
    pub fn as_f32(&self) -> Option<f32> {
        match self {
            Self::Float32(v) => Some(*v),
            _ => None,
        }
    }
    /// Try to convert the value to a 64-bit floating point number
    pub fn as_f64(&self) -> Option<f64> {
        match self {
            Self::Float64(v) => Some(*v),
            _ => None,
        }
    }
    /// Try to convert the value to a unicode character
    pub fn as_unicode(&self) -> Option<char> {
        match self {
            Self::Unicode(v) => Some(*v),
            _ => None,
        }
    }
    /// Try to convert the value to a UTF-8 string
    pub fn as_utf8(&self) -> Option<&str> {
        match self {
            Self::UTF8(v) => Some(v),
            _ => None,
        }
    }
    /// Try to convert the value to a record
    pub fn as_structure(&self) -> Option<&WasiObject> {
        match self {
            Self::Object(v) => Some(v),
            _ => None,
        }
    }
}

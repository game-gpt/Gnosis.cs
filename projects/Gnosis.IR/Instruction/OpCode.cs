namespace Gnosis.IR.Instruction;

public enum OpCode : byte
{
    Halt = 0x00,
    Nop = 0x01,

    PushInt8 = 0x10,
    PushInt16 = 0x11,
    PushInt32 = 0x12,
    PushInt64 = 0x13,
    PushFloat32 = 0x14,
    PushFloat64 = 0x15,

    // 布尔与空值常量
    PushTrue = 0x16,
    PushFalse = 0x17,
    PushNull = 0x18,

    Pop = 0x20,
    Dup = 0x21,

    AddInt = 0x30,
    SubInt = 0x31,
    MulInt = 0x32,
    DivInt = 0x33,
    AddFloat = 0x34,
    SubFloat = 0x35,
    MulFloat = 0x36,
    DivFloat = 0x37,
    NegInt = 0x38,
    NegFloat = 0x39,

    // 整数比较
    EqualInt = 0x3A,
    NotEqualInt = 0x3B,
    LessInt = 0x3C,
    GreaterInt = 0x3D,
    LessEqualInt = 0x3E,
    GreaterEqualInt = 0x3F,

    Jump = 0x40,
    JumpIfTrue = 0x41,
    JumpIfFalse = 0x42,

    // 浮点比较
    EqualFloat = 0x43,
    NotEqualFloat = 0x44,
    LessFloat = 0x45,
    GreaterFloat = 0x46,
    LessEqualFloat = 0x47,
    GreaterEqualFloat = 0x48,

    // 逻辑运算
    And = 0x49,
    Or = 0x4A,
    Not = 0x4B,

    Call = 0x50,
    CallNative = 0x51,
    Return = 0x52,
    CallModule = 0x53,

    LoadLocal = 0x60,
    StoreLocal = 0x61,
    LoadGlobal = 0x62,
    StoreGlobal = 0x63,
    LoadField = 0x64,
    StoreField = 0x65,

    NewObject = 0x70,
    GetField = 0x71,
    SetField = 0x72,

    SpawnEntity = 0x80,
    DestroyEntity = 0x81,
    AddComponent = 0x82,
    GetComponent = 0x83,
    RemoveComponent = 0x84,
    QueryAll = 0x85,
    QueryAny = 0x86,
    DefineComponent = 0x87,
    DefineSystem = 0x88,
    SetComponent = 0x89,
    HasComponent = 0x8A,
    QueryWith = 0x8B,
    QueryWithout = 0x8C,
    SystemSchedule = 0x8D,
    WorldUpdate = 0x8E,

    // 字符串操作
    PushString = 0x90,
    ConcatString = 0x91,
    StringLength = 0x92,
    StringGetChar = 0x93,

    // 数组操作
    NewArray = 0xA0,
    ArrayGet = 0xA1,
    ArraySet = 0xA2,
    ArrayLength = 0xA3,

    // 闭包操作
    MakeClosure = 0xB0,
    GetUpvalue = 0xB1,
    SetUpvalue = 0xB2,

    // 类型检查
    IsNull = 0xC0,
    IsType = 0xC1,
    TypeOf = 0xC2,
}

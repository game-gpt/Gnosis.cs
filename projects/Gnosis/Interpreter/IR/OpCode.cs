namespace Gnosis.Interpreter.IR;

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

    Jump = 0x40,
    JumpIfTrue = 0x41,
    JumpIfFalse = 0x42,

    Call = 0x50,
    CallNative = 0x51,
    Return = 0x52,

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
}

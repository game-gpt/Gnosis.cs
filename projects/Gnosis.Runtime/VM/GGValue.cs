namespace Gnosis.Runtime.VM;

public enum GGValueType : byte
{
    Null,
    Int,
    Float,
    Bool,
    String,
    Object,
    Array,
    Struct,
    Closure,
    NativeObject
}

public readonly struct GGValue
{
    public GGValueType Type { get; }
    private readonly long _intValue;
    private readonly double _floatValue;
    private readonly object? _reference;

    private GGValue(GGValueType type, long intValue, double floatValue, object? reference)
    {
        Type = type;
        _intValue = intValue;
        _floatValue = floatValue;
        _reference = reference;
    }

    public static GGValue Null => new(GGValueType.Null, 0, 0, null);
    public static GGValue FromInt(long value) => new(GGValueType.Int, value, 0, null);
    public static GGValue FromFloat(double value) => new(GGValueType.Float, 0, value, null);
    public static GGValue FromBool(bool value) => new(GGValueType.Bool, value ? 1 : 0, 0, null);
    public static GGValue FromString(GGString value) => new(GGValueType.String, 0, 0, value);
    public static GGValue FromObject(GGObject value) => new(GGValueType.Object, 0, 0, value);
    public static GGValue FromArray(GGArray value) => new(GGValueType.Array, 0, 0, value);
    public static GGValue FromStruct(GGStruct value) => new(GGValueType.Struct, 0, 0, value);
    public static GGValue FromClosure(GGClosure value) => new(GGValueType.Closure, 0, 0, value);
    public static GGValue FromNativeObject(object value) => new(GGValueType.NativeObject, 0, 0, value);

    public long IntValue => _intValue;
    public double FloatValue => _floatValue;
    public bool BoolValue => _intValue != 0;
    public GGString? StringValue => _reference as GGString;
    public GGObject? ObjectValue => _reference as GGObject;
    public GGArray? ArrayValue => _reference as GGArray;
    public GGStruct? StructValue => _reference as GGStruct;
    public GGClosure? ClosureValue => _reference as GGClosure;
    public object? NativeObjectValue => _reference;

    public bool IsNull => Type == GGValueType.Null;
    public bool IsReference => Type is GGValueType.Object or GGValueType.Array or GGValueType.String or GGValueType.Struct or GGValueType.Closure or GGValueType.NativeObject;
    public object? Reference => _reference;
}

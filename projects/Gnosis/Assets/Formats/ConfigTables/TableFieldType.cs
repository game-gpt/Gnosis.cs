namespace Gnosis.Assets.Formats.ConfigTables;

public enum TableFieldTypeKind
{
    I32,
    I64,
    F32,
    F64,
    Bool,
    String,
    List,
    FixedList,
    Reference
}

public abstract record TableFieldType
{
    public abstract TableFieldTypeKind Kind { get; }

    public static readonly TableFieldType I32 = new PrimitiveType(TableFieldTypeKind.I32);
    public static readonly TableFieldType I64 = new PrimitiveType(TableFieldTypeKind.I64);
    public static readonly TableFieldType F32 = new PrimitiveType(TableFieldTypeKind.F32);
    public static readonly TableFieldType F64 = new PrimitiveType(TableFieldTypeKind.F64);
    public static readonly TableFieldType Bool = new PrimitiveType(TableFieldTypeKind.Bool);
    public static readonly TableFieldType String = new PrimitiveType(TableFieldTypeKind.String);

    public static TableFieldType List(TableFieldType element)
    {
        return new ListType(element);
    }

    public static TableFieldType FixedList(TableFieldType element, int size)
    {
        return new FixedListType(element, size);
    }

    public static TableFieldType Reference(string targetTable)
    {
        return new ReferenceType(targetTable);
    }

    private sealed record PrimitiveType(TableFieldTypeKind Kind) : TableFieldType;

    private sealed record ListType(TableFieldType Element) : TableFieldType
    {
        public override TableFieldTypeKind Kind => TableFieldTypeKind.List;
    }

    private sealed record FixedListType(TableFieldType Element, int Size) : TableFieldType
    {
        public override TableFieldTypeKind Kind => TableFieldTypeKind.FixedList;
    }

    private sealed record ReferenceType(string TargetTable) : TableFieldType
    {
        public override TableFieldTypeKind Kind => TableFieldTypeKind.Reference;
    }

    public TableFieldType? GetListElementType()
    {
        return this switch
        {
            ListType l => l.Element,
            FixedListType f => f.Element,
            _ => null
        };
    }

    public int? GetFixedListSize()
    {
        return this is FixedListType f ? f.Size : null;
    }

    public string? GetReferenceTarget()
    {
        return this is ReferenceType r ? r.TargetTable : null;
    }
}

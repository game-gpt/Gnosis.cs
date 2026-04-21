namespace Gnosis.Assets.Formats.ConfigTables;

public sealed record ConfigTableMapping
{
    public int CommentRow { get; init; } = 1;
    public int FieldNameRow { get; init; } = 2;
    public int FieldTypeRow { get; init; } = 3;
    public int DataStartRow { get; init; } = 4;
}

using Oak.Csv;

namespace Gnosis.Asset.Format.ConfigTables;

public sealed class TsvTableParser : ConfigTableParserBase
{
    public TsvTableParser(ConfigTableMapping? mapping = null) : base(mapping) { }

    public override async Task<ConfigTableData> ParseAsync(string path, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var tableName = Path.GetFileNameWithoutExtension(path);
        var rows = TsvParser.ParseRows(content);

        return BuildTableData(tableName, rows, path);
    }

    public ConfigTableData ParseFromString(string content, string tableName = "Unknown")
    {
        var rows = TsvParser.ParseRows(content);

        return BuildTableData(tableName, rows, string.Empty);
    }
}

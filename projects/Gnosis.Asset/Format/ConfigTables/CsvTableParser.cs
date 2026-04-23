using Oak.Csv;

namespace Gnosis.Asset.Format.ConfigTables;

public sealed class CsvTableParser : ConfigTableParserBase
{
    public CsvTableParser(ConfigTableMapping? mapping = null) : base(mapping) { }

    public override async Task<ConfigTableData> ParseAsync(string path, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var tableName = Path.GetFileNameWithoutExtension(path);
        var rows = CsvParser.ParseRows(content);

        return BuildTableData(tableName, rows, path);
    }

    public ConfigTableData ParseFromString(string content, string tableName = "Unknown")
    {
        var rows = CsvParser.ParseRows(content);

        return BuildTableData(tableName, rows, string.Empty);
    }
}

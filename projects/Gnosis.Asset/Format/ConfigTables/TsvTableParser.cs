namespace Gnosis.Asset.Format.ConfigTables;

public sealed class TsvTableParser : ConfigTableParserBase
{
    public TsvTableParser(ConfigTableMapping? mapping = null) : base(mapping) { }

    public override async Task<ConfigTableData> ParseAsync(string path, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var tableName = Path.GetFileNameWithoutExtension(path);
        var rows = ParseTsvRows(content);

        return BuildTableData(tableName, rows, path);
    }

    public ConfigTableData ParseFromString(string content, string tableName = "Unknown")
    {
        var rows = ParseTsvRows(content);

        return BuildTableData(tableName, rows, string.Empty);
    }

    private static List<IReadOnlyList<string>> ParseTsvRows(string content)
    {
        var rows = new List<IReadOnlyList<string>>();
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim('\r', '\n');

            if (string.IsNullOrEmpty(trimmedLine))
            {
                continue;
            }

            rows.Add(trimmedLine.Split('\t'));
        }

        return rows;
    }
}

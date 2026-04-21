namespace Gnosis.Assets.Formats.ConfigTables;

public sealed class CsvTableParser : ConfigTableParserBase
{
    public CsvTableParser(ConfigTableMapping? mapping = null) : base(mapping) { }

    public override async Task<ConfigTableData> ParseAsync(string path, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var tableName = Path.GetFileNameWithoutExtension(path);
        var rows = ParseCsvRows(content);

        return BuildTableData(tableName, rows, path);
    }

    public ConfigTableData ParseFromString(string content, string tableName = "Unknown")
    {
        var rows = ParseCsvRows(content);

        return BuildTableData(tableName, rows, string.Empty);
    }

    private static List<IReadOnlyList<string>> ParseCsvRows(string content)
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

            rows.Add(ParseCsvLine(trimmedLine));
        }

        return rows;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        fields.Add(sb.ToString());

        return fields;
    }
}

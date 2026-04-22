namespace Gnosis.Asset.Format.ConfigTables;

public sealed class ConfigTableFormatHandler : FormatHandlerBase, IConfigTableFormat
{
    private readonly ConfigTableExporter _exporter = new();
    private readonly ConfigTableMapping? _mapping;

    public ConfigTableFormatHandler(ConfigTableMapping? mapping = null)
    {
        _mapping = mapping;
    }

    public override FormatType SupportedFormat => FormatType.ConfigTable;

    public async Task<ConfigTableData> LoadTableAsync(string path, CancellationToken cancellationToken = default)
    {
        var parser = CreateParser(path);

        return await parser.ParseAsync(path, cancellationToken);
    }

    public async Task ExportTableAsync(ConfigTableData tableData, string outputDir, CancellationToken cancellationToken = default)
    {
        await _exporter.ExportAsync(tableData, outputDir, cancellationToken);
    }

    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".csv", ".tsv", ".xlsx", ".xls" };
    }

    private ConfigTableParserBase CreateParser(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        return extension switch
        {
            ".csv" => new CsvTableParser(_mapping),
            ".tsv" => new TsvTableParser(_mapping),
            ".xlsx" or ".xls" => new ExcelTableParser(_mapping),
            _ => new CsvTableParser(_mapping)
        };
    }
}

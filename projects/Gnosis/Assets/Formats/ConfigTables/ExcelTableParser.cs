namespace Gnosis.Assets.Formats.ConfigTables;

public sealed class ExcelTableParser : ConfigTableParserBase
{
    public ExcelTableParser(ConfigTableMapping? mapping = null) : base(mapping) { }

    public override Task<ConfigTableData> ParseAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Excel 解析器尚未实现，请先将 Excel 文件转换为 CSV 或 TSV 格式");
    }
}

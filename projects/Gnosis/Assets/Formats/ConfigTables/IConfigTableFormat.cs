namespace Gnosis.Assets.Formats.ConfigTables;

public interface IConfigTableFormat : IFormatHandler
{
    Task<ConfigTableData> LoadTableAsync(string path, CancellationToken cancellationToken = default);
    Task ExportTableAsync(ConfigTableData tableData, string outputDir, CancellationToken cancellationToken = default);
}

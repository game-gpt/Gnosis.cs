using Gnosis.Asset.Format;

namespace Gnosis.Asset.Import;

public abstract class AssetImporterBase : IAssetImporter
{
    public abstract bool CanImport(string assetPath, FormatType formatType);

    public virtual IReadOnlyList<string> GetDependencies(string assetPath)
    {
        return Array.Empty<string>();
    }

    public async Task<ImportResult> ImportAsync(
        ImportContext context,
        string assetPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validationResult = ValidatePath(assetPath);
        if (!validationResult.Success)
        {
            return validationResult;
        }

        try
        {
            var result = await OnImportAsync(context, assetPath, cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FileNotFoundException ex)
        {
            context.Logger?.LogError($"资产文件未找到：{assetPath}，{ex.Message}");
            return ImportResult.Failed(assetPath, $"资产文件未找到：{ex.Message}");
        }
        catch (IOException ex)
        {
            context.Logger?.LogError($"资产读取失败：{assetPath}，{ex.Message}");
            return ImportResult.Failed(assetPath, $"资产读取失败：{ex.Message}");
        }
        catch (SystemException ex)
        {
            context.Logger?.LogError($"资产导入失败：{assetPath}，{ex.Message}");
            return ImportResult.Failed(assetPath, $"资产导入失败：{ex.Message}");
        }
    }

    protected abstract Task<ImportResult> OnImportAsync(
        ImportContext context,
        string assetPath,
        CancellationToken cancellationToken);

    protected static ImportResult ValidatePath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return ImportResult.Failed(assetPath, "资产路径不能为空");
        }

        var normalized = assetPath.Replace('\\', '/');
        if (normalized.Contains(".."))
        {
            return ImportResult.Failed(assetPath, "资产路径不能包含父目录引用（..）");
        }

        return ImportResult.Succeeded(assetPath, string.Empty);
    }

    protected async Task<byte[]> ReadAssetAsync(
        ImportContext context,
        string assetPath,
        CancellationToken cancellationToken)
    {
        var stream = context.VFS.OpenRead(assetPath);
        if (stream is null)
        {
            throw new FileNotFoundException($"资产文件未找到：{assetPath}", assetPath);
        }

        await using (stream.ConfigureAwait(false))
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            return memoryStream.ToArray();
        }
    }
}

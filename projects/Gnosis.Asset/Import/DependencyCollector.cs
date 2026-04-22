namespace Gnosis.Asset.Import;

public sealed class DependencyCollector
{
    private readonly ImportContext _context;

    public DependencyCollector(ImportContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IReadOnlyList<string> CollectDependencies(string assetPath, IAssetImporter importer)
    {
        var dependencies = importer.GetDependencies(assetPath);
        var validDependencies = new List<string>(dependencies.Count);

        foreach (var dep in dependencies)
        {
            if (_context.VFS.FileExists(dep))
            {
                validDependencies.Add(dep);
            }
            else
            {
                _context.Logger?.LogWarning($"依赖资产未找到：{dep}（被 {assetPath} 引用）");
            }
        }

        return validDependencies;
    }

    public Dictionary<string, IReadOnlyList<string>> CollectAllDependencies(
        IEnumerable<string> assetPaths,
        IAssetImporter importer)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>();

        foreach (var assetPath in assetPaths)
        {
            var deps = CollectDependencies(assetPath, importer);
            result[assetPath] = deps;
        }

        return result;
    }
}

using Gnosis.Asset.Bundle;
using Gnosis.Asset.Format;

namespace Gnosis.Toolchain.Cooker.Module;

public sealed record ModulePackEntry
{
    public string ModuleName { get; init; } = string.Empty;
    public string ModulePath { get; init; } = string.Empty;
    public IReadOnlyList<string> Dependencies { get; init; } = [];
    public int BytecodeSize { get; init; }
}

public sealed record ModulePackResult
{
    public bool Success { get; init; }
    public string BundleName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public int ModuleCount { get; init; }
    public long TotalSize { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

public sealed class ModulePackager
{
    private const uint ModuleBundleMagic = 0x47474D42;
    private const ushort CurrentVersion = 1;

    private readonly List<ModulePackEntry> _entries = new();

    public int ModuleCount => _entries.Count;

    public void AddModule(string moduleName, string modulePath, IReadOnlyList<string>? dependencies = null)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            throw new ArgumentException("模块名称不能为空", nameof(moduleName));
        }

        if (string.IsNullOrWhiteSpace(modulePath))
        {
            throw new ArgumentException("模块路径不能为空", nameof(modulePath));
        }

        var bytecodeSize = 0;
        if (File.Exists(modulePath))
        {
            var fileInfo = new FileInfo(modulePath);
            bytecodeSize = (int)fileInfo.Length;
        }

        var entry = new ModulePackEntry
        {
            ModuleName = moduleName,
            ModulePath = modulePath,
            Dependencies = dependencies ?? [],
            BytecodeSize = bytecodeSize
        };

        _entries.Add(entry);
    }

    public void AddModulesFromDirectory(string directory, string pattern = "*.gnosis")
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"模块目录不存在：{directory}");
        }

        var files = Directory.GetFiles(directory, pattern);

        foreach (var file in files)
        {
            var moduleName = Path.GetFileNameWithoutExtension(file);
            AddModule(moduleName, file);
        }
    }

    public bool RemoveModule(string moduleName)
    {
        return _entries.RemoveAll(e => string.Equals(e.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase)) > 0;
    }

    public IReadOnlyList<ModulePackEntry> GetEntries()
    {
        return _entries.ToList();
    }

    public IReadOnlyList<string> ValidateModules()
    {
        var errors = new List<string>();

        foreach (var entry in _entries)
        {
            if (!File.Exists(entry.ModulePath))
            {
                errors.Add($"模块文件不存在：{entry.ModuleName} ({entry.ModulePath})");
                continue;
            }

            if (!ValidateModuleFormat(entry.ModulePath))
            {
                errors.Add($"模块格式无效：{entry.ModuleName} ({entry.ModulePath})");
            }
        }

        var moduleNames = new HashSet<string>(_entries.Select(e => e.ModuleName), StringComparer.OrdinalIgnoreCase);

        foreach (var entry in _entries)
        {
            foreach (var dep in entry.Dependencies)
            {
                if (!moduleNames.Contains(dep))
                {
                    errors.Add($"模块 {entry.ModuleName} 依赖的模块 {dep} 不存在于包中");
                }
            }
        }

        return errors;
    }

    public async Task<ModulePackResult> PackAsync(string outputDirectory, string bundleName, CompressionType compression = CompressionType.LZ4, byte[]? encryptionKey = null, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateModules();
        if (validationErrors.Count > 0)
        {
            return new ModulePackResult
            {
                Success = false,
                Errors = validationErrors
            };
        }

        try
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var bundler = new AssetBundler(outputDirectory);

            foreach (var entry in _entries)
            {
                var virtualPath = $"modules/{entry.ModuleName}.gnosis";
                bundler.AddAsset(entry.ModulePath, virtualPath, compression);
            }

            var manifest = BuildManifest();
            var manifestPath = Path.Combine(outputDirectory, "module_manifest.gon");
            await File.WriteAllTextAsync(manifestPath, manifest, cancellationToken);
            bundler.AddAsset(manifestPath, "module_manifest.gon", CompressionType.None);

            var result = bundler.Pack(bundleName, encryptionKey);

            if (File.Exists(manifestPath))
            {
                File.Delete(manifestPath);
            }

            return new ModulePackResult
            {
                Success = true,
                BundleName = result.BundleName,
                FilePath = result.FilePath,
                ModuleCount = _entries.Count,
                TotalSize = result.TotalSize
            };
        }
        catch (IOException ex)
        {
            return new ModulePackResult
            {
                Success = false,
                Errors = [$"模块打包 IO 错误：{ex.Message}"]
            };
        }
    }

    public void Clear()
    {
        _entries.Clear();
    }

    private static bool ValidateModuleFormat(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            if (stream.Length < 4)
            {
                return false;
            }

            var magic = reader.ReadUInt32();
            return magic == 0x474E4F53;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private string BuildManifest()
    {
        var lines = new List<string>
        {
            "{",
            $"  \"version\": {CurrentVersion},",
            $"  \"moduleCount\": {_entries.Count},",
            "  \"modules\": ["
        };

        for (var i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            var deps = string.Join(", ", entry.Dependencies.Select(d => $"\"{d}\""));
            var comma = i < _entries.Count - 1 ? "," : "";

            lines.Add("    {");
            lines.Add($"      \"name\": \"{entry.ModuleName}\",");
            lines.Add($"      \"bytecodeSize\": {entry.BytecodeSize},");
            lines.Add($"      \"dependencies\": [{deps}]");
            lines.Add($"    }}{comma}");
        }

        lines.Add("  ]");
        lines.Add("}");

        return string.Join("\n", lines);
    }
}

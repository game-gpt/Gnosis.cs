using System.Collections.Concurrent;
using System.Text.Json;
using Gnosis.Asset.Format;
using Gnosis.Asset.Format.Compression;
using Gnosis.Asset.Meta;

namespace Gnosis.Asset.Bundle;

public sealed class AssetBundler : IDisposable
{
    private readonly ConcurrentDictionary<string, AssetBundleEntry> _entries = new();
    private readonly string _outputDirectory;
    private bool _disposed;

    public int EntryCount => _entries.Count;
    public string OutputDirectory => _outputDirectory;

    public AssetBundler(string outputDirectory)
    {
        _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));

        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    #region 添加资产

    /// <summary>
    /// 添加资产到打包列表
    /// </summary>
    public void AddAsset(string assetPath, string virtualPath, CompressionType compression = CompressionType.None, EncryptionType encryption = EncryptionType.None)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(assetPath))
        {
            throw new ArgumentNullException(nameof(assetPath));
        }

        if (string.IsNullOrEmpty(virtualPath))
        {
            throw new ArgumentNullException(nameof(virtualPath));
        }

        var normalizedVirtualPath = NormalizePath(virtualPath);
        var normalizedAssetPath = NormalizePath(assetPath);

        if (!File.Exists(normalizedAssetPath))
        {
            throw new FileNotFoundException($"未找到资产文件：{normalizedAssetPath}");
        }

        var data = File.ReadAllBytes(normalizedAssetPath);
        var hash = AssetHashCalculator.ComputeHash(data);

        var entry = new AssetBundleEntry
        {
            VirtualPath = normalizedVirtualPath,
            SourcePath = normalizedAssetPath,
            Data = data,
            Compression = compression,
            Encryption = encryption,
            Hash = hash,
            Size = data.Length
        };

        _entries[normalizedVirtualPath] = entry;
    }

    /// <summary>
    /// 添加内存中的资产数据到打包列表
    /// </summary>
    public void AddAssetFromMemory(string virtualPath, byte[] data, CompressionType compression = CompressionType.None, EncryptionType encryption = EncryptionType.None)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(virtualPath))
        {
            throw new ArgumentNullException(nameof(virtualPath));
        }

        ArgumentNullException.ThrowIfNull(data);

        var normalizedVirtualPath = NormalizePath(virtualPath);
        var hash = AssetHashCalculator.ComputeHash(data);

        var entry = new AssetBundleEntry
        {
            VirtualPath = normalizedVirtualPath,
            SourcePath = string.Empty,
            Data = data,
            Compression = compression,
            Encryption = encryption,
            Hash = hash,
            Size = data.Length
        };

        _entries[normalizedVirtualPath] = entry;
    }

    /// <summary>
    /// 从目录批量添加资产
    /// </summary>
    public int AddDirectory(string directoryPath, string virtualPrefix, CompressionType compression = CompressionType.None, EncryptionType encryption = EncryptionType.None, string searchPattern = "*")
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"未找到目录：{directoryPath}");
        }

        var files = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);
        var normalizedPrefix = NormalizePath(virtualPrefix);
        var added = 0;

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(directoryPath, file);
            var virtualPath = string.IsNullOrEmpty(normalizedPrefix)
                ? NormalizePath(relativePath)
                : $"{normalizedPrefix}/{NormalizePath(relativePath)}";

            try
            {
                AddAsset(file, virtualPath, compression, encryption);
                added++;
            }
            catch (IOException)
            {
            }
        }

        return added;
    }

    /// <summary>
    /// 移除指定虚拟路径的资产
    /// </summary>
    public bool RemoveAsset(string virtualPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedPath = NormalizePath(virtualPath);
        return _entries.TryRemove(normalizedPath, out _);
    }

    /// <summary>
    /// 清空所有待打包资产
    /// </summary>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _entries.Clear();
    }

    #endregion

    #region 打包

    /// <summary>
    /// 将所有资产打包为资产包文件
    /// </summary>
    public AssetBundleResult Pack(string bundleName, byte[]? encryptionKey = null, IEncryptionProvider? encryptionProvider = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(bundleName))
        {
            throw new ArgumentNullException(nameof(bundleName));
        }

        if (_entries.IsEmpty)
        {
            throw new InvalidOperationException("没有可打包的资产");
        }

        ValidateEncryptionConfig(encryptionKey, encryptionProvider);

        cancellationToken.ThrowIfCancellationRequested();

        var sortedEntries = _entries.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase).ToList();
        var indexEntries = new List<AssetBundleIndexEntry>();
        var effectiveProvider = ResolveEncryptionProvider(encryptionProvider);

        using var dataStream = new MemoryStream();

        foreach (var (virtualPath, entry) in sortedEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var offset = dataStream.Position;
            byte[] dataToWrite = entry.Data;

            if (entry.Compression != CompressionType.None)
            {
                dataToWrite = CompressionService.Compress(entry.Data, entry.Compression);
            }

            byte[]? iv = null;

            if (entry.Encryption != EncryptionType.None)
            {
                var encrypted = effectiveProvider.Encrypt(dataToWrite, encryptionKey!);
                dataToWrite = encrypted.Ciphertext;
                iv = encrypted.Iv;
            }

            dataStream.Write(dataToWrite, 0, dataToWrite.Length);

            indexEntries.Add(new AssetBundleIndexEntry
            {
                VirtualPath = virtualPath,
                Offset = offset,
                CompressedSize = dataToWrite.Length,
                OriginalSize = entry.Size,
                Compression = entry.Compression,
                Encryption = entry.Encryption,
                Iv = iv ?? [],
                Hash = entry.Hash
            });
        }

        var indexData = BuildIndexData(indexEntries, bundleName);
        dataStream.Write(indexData, 0, indexData.Length);

        var totalSize = (int)dataStream.Position;
        var bundleData = dataStream.ToArray();

        var bundleFilePath = Path.Combine(_outputDirectory, $"{bundleName}.gnosis-bundle");
        File.WriteAllBytes(bundleFilePath, bundleData);

        return new AssetBundleResult
        {
            BundleName = bundleName,
            FilePath = bundleFilePath,
            TotalSize = totalSize,
            AssetCount = indexEntries.Count,
            Entries = indexEntries
        };
    }

    /// <summary>
    /// 异步打包所有资产
    /// </summary>
    public async Task<AssetBundleResult> PackAsync(string bundleName, byte[]? encryptionKey = null, IEncryptionProvider? encryptionProvider = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(bundleName))
        {
            throw new ArgumentNullException(nameof(bundleName));
        }

        if (_entries.IsEmpty)
        {
            throw new InvalidOperationException("没有可打包的资产");
        }

        ValidateEncryptionConfig(encryptionKey, encryptionProvider);

        cancellationToken.ThrowIfCancellationRequested();

        var sortedEntries = _entries.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase).ToList();
        var indexEntries = new List<AssetBundleIndexEntry>();
        var effectiveProvider = ResolveEncryptionProvider(encryptionProvider);

        using var dataStream = new MemoryStream();

        foreach (var (virtualPath, entry) in sortedEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var offset = dataStream.Position;
            byte[] dataToWrite = entry.Data;

            if (entry.Compression != CompressionType.None)
            {
                dataToWrite = await Task.Run(() => CompressionService.Compress(entry.Data, entry.Compression), cancellationToken);
            }

            byte[]? iv = null;

            if (entry.Encryption != EncryptionType.None)
            {
                var encrypted = effectiveProvider.Encrypt(dataToWrite, encryptionKey!);
                dataToWrite = encrypted.Ciphertext;
                iv = encrypted.Iv;
            }

            await dataStream.WriteAsync(dataToWrite, cancellationToken);

            indexEntries.Add(new AssetBundleIndexEntry
            {
                VirtualPath = virtualPath,
                Offset = offset,
                CompressedSize = dataToWrite.Length,
                OriginalSize = entry.Size,
                Compression = entry.Compression,
                Encryption = entry.Encryption,
                Iv = iv ?? [],
                Hash = entry.Hash
            });
        }

        var indexData = BuildIndexData(indexEntries, bundleName);
        await dataStream.WriteAsync(indexData, cancellationToken);

        var totalSize = (int)dataStream.Position;
        var bundleData = dataStream.ToArray();

        var bundleFilePath = Path.Combine(_outputDirectory, $"{bundleName}.gnosis-bundle");
        await File.WriteAllBytesAsync(bundleFilePath, bundleData, cancellationToken);

        return new AssetBundleResult
        {
            BundleName = bundleName,
            FilePath = bundleFilePath,
            TotalSize = totalSize,
            AssetCount = indexEntries.Count,
            Entries = indexEntries
        };
    }

    #endregion

    #region 加载资产包

    /// <summary>
    /// 从文件加载资产包索引
    /// </summary>
    public static AssetBundleIndex LoadIndex(string bundleFilePath)
    {
        if (!File.Exists(bundleFilePath))
        {
            throw new FileNotFoundException($"未找到资产包文件：{bundleFilePath}");
        }

        var data = File.ReadAllBytes(bundleFilePath);
        return ParseIndex(data);
    }

    /// <summary>
    /// 从资产包中读取指定资产
    /// </summary>
    public static byte[] ReadAsset(string bundleFilePath, string virtualPath, byte[]? encryptionKey = null, IEncryptionProvider? encryptionProvider = null)
    {
        var index = LoadIndex(bundleFilePath);
        var normalizedPath = NormalizePath(virtualPath);

        var entry = index.Entries.FirstOrDefault(e =>
            string.Equals(e.VirtualPath, normalizedPath, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            throw new KeyNotFoundException($"资产包中未找到资产：{virtualPath}");
        }

        return ReadAssetFromBundle(bundleFilePath, entry, encryptionKey, encryptionProvider);
    }

    /// <summary>
    /// 从资产包中流式读取指定资产
    /// </summary>
    public static Stream ReadAssetStream(string bundleFilePath, string virtualPath, byte[]? encryptionKey = null, IEncryptionProvider? encryptionProvider = null)
    {
        var index = LoadIndex(bundleFilePath);
        var normalizedPath = NormalizePath(virtualPath);

        var entry = index.Entries.FirstOrDefault(e =>
            string.Equals(e.VirtualPath, normalizedPath, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            throw new KeyNotFoundException($"资产包中未找到资产：{virtualPath}");
        }

        var data = ReadAssetFromBundle(bundleFilePath, entry, encryptionKey, encryptionProvider);
        return new MemoryStream(data);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _entries.Clear();
        _disposed = true;
    }

    #endregion

    #region 私有方法

    private void ValidateEncryptionConfig(byte[]? encryptionKey, IEncryptionProvider? encryptionProvider)
    {
        var hasEncryptedEntries = _entries.Values.Any(e => e.Encryption != EncryptionType.None);

        if (hasEncryptedEntries && encryptionKey == null)
        {
            throw new ArgumentException("存在加密资产但未提供加密密钥", nameof(encryptionKey));
        }

        if (encryptionKey != null && encryptionProvider != null && encryptionProvider.EncryptionType != EncryptionType.Aes256Cbc)
        {
            throw new ArgumentException($"不支持的加密类型：{encryptionProvider.EncryptionType}", nameof(encryptionProvider));
        }
    }

    private static IEncryptionProvider ResolveEncryptionProvider(IEncryptionProvider? encryptionProvider)
    {
        return encryptionProvider ?? new AesEncryptionProvider();
    }

    private static byte[] BuildIndexData(List<AssetBundleIndexEntry> entries, string bundleName)
    {
        var index = new AssetBundleIndex
        {
            Magic = 0x474E425A,
            Version = 2,
            BundleName = bundleName,
            EntryCount = entries.Count,
            Entries = entries
        };

        var indexJson = JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = false });
        var indexBytes = System.Text.Encoding.UTF8.GetBytes(indexJson);

        using var outputStream = new MemoryStream();
        using var writer = new BinaryWriter(outputStream);

        writer.Write(index.Magic);
        writer.Write(index.Version);
        writer.Write(index.EntryCount);
        writer.Write(indexBytes.Length);
        writer.Write(indexBytes);

        return outputStream.ToArray();
    }

    private static AssetBundleIndex ParseIndex(byte[] bundleData)
    {
        var footerSize = 4 + 4 + 4 + 4;
        var footerOffset = bundleData.Length - footerSize;

        if (footerOffset < 0)
        {
            throw new InvalidDataException("资产包数据过小");
        }

        using var stream = new MemoryStream(bundleData);
        using var reader = new BinaryReader(stream);

        stream.Position = footerOffset;
        var magic = reader.ReadUInt32();

        if (magic != 0x474E425A)
        {
            throw new InvalidDataException("资产包索引魔数无效");
        }

        var version = reader.ReadInt32();

        if (version < 1 || version > 2)
        {
            throw new InvalidDataException($"不支持的资产包版本：{version}");
        }

        var entryCount = reader.ReadInt32();
        var indexJsonSize = reader.ReadInt32();

        var indexOffset = footerOffset - indexJsonSize;

        if (indexOffset < 0)
        {
            throw new InvalidDataException("资产包索引偏移无效");
        }

        stream.Position = indexOffset;
        var indexJsonBytes = reader.ReadBytes(indexJsonSize);
        var indexJson = System.Text.Encoding.UTF8.GetString(indexJsonBytes);

        var index = JsonSerializer.Deserialize<AssetBundleIndex>(indexJson)
            ?? throw new InvalidDataException("资产包索引反序列化失败");

        return index;
    }

    private static byte[] ReadAssetFromBundle(string bundleFilePath, AssetBundleIndexEntry entry, byte[]? encryptionKey = null, IEncryptionProvider? encryptionProvider = null)
    {
        using var stream = new FileStream(bundleFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        stream.Position = entry.Offset;

        var compressedData = new byte[entry.CompressedSize];
        int totalRead = 0;

        while (totalRead < entry.CompressedSize)
        {
            int bytesRead = stream.Read(compressedData, totalRead, entry.CompressedSize - totalRead);
            if (bytesRead == 0) break;
            totalRead += bytesRead;
        }

        byte[] data = compressedData;

        if (entry.Encryption != EncryptionType.None)
        {
            if (encryptionKey == null)
            {
                throw new InvalidOperationException($"资产 {entry.VirtualPath} 已加密，需要提供加密密钥");
            }

            var provider = ResolveEncryptionProvider(encryptionProvider);
            var encryptedData = new EncryptedData
            {
                Ciphertext = compressedData,
                Iv = entry.Iv,
                EncryptionType = entry.Encryption
            };
            data = provider.Decrypt(encryptedData, encryptionKey);
        }

        if (entry.Compression != CompressionType.None)
        {
            return CompressionService.Decompress(data, entry.Compression);
        }

        return data;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    #endregion
}

public sealed class AssetBundleEntry
{
    public string VirtualPath { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public byte[] Data { get; init; } = [];
    public CompressionType Compression { get; init; }
    public EncryptionType Encryption { get; init; }
    public AssetHash Hash { get; init; } = AssetHash.Empty;
    public int Size { get; init; }
}

public sealed class AssetBundleResult
{
    public string BundleName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public int TotalSize { get; init; }
    public int AssetCount { get; init; }
    public IReadOnlyList<AssetBundleIndexEntry> Entries { get; init; } = [];
}

public sealed class AssetBundleIndex
{
    public uint Magic { get; init; }
    public int Version { get; init; }
    public string BundleName { get; init; } = string.Empty;
    public int EntryCount { get; init; }
    public IReadOnlyList<AssetBundleIndexEntry> Entries { get; init; } = [];
}

public sealed class AssetBundleIndexEntry
{
    public string VirtualPath { get; init; } = string.Empty;
    public long Offset { get; init; }
    public int CompressedSize { get; init; }
    public int OriginalSize { get; init; }
    public CompressionType Compression { get; init; }
    public EncryptionType Encryption { get; init; }
    public byte[] Iv { get; init; } = [];
    public AssetHash Hash { get; init; } = AssetHash.Empty;
}

using System.Collections.Concurrent;

namespace Gnosis.Widget;

/// <summary>
/// 热重载服务，监控 .script 文件变更并触发增量编译与模块替换
/// </summary>
public sealed class HotReloadService : IDisposable
{
    #region 字段

    private readonly ConcurrentDictionary<string, DateTime> _fileTimestamps = new();
    private readonly ConcurrentQueue<FileChangeEvent> _pendingChanges = new();
    private readonly List<string> _watchDirectories = new();
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly HashSet<string> _supportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".script",
        ".ggs",
        ".ggw",
        ".ggon"
    };

    private bool _isRunning;
    private bool _isProcessing;
    private int _debounceMs = 200;

    #endregion

    #region 属性

    /// <summary>
    /// 服务是否正在运行
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// 是否正在处理变更
    /// </summary>
    public bool IsProcessing => _isProcessing;

    /// <summary>
    /// 防抖间隔（毫秒），避免频繁触发编译
    /// </summary>
    public int DebounceMs
    {
        get => _debounceMs;
        set => _debounceMs = Math.Max(50, value);
    }

    /// <summary>
    /// 已监控的目录数量
    /// </summary>
    public int WatchDirectoryCount => _watchDirectories.Count;

    /// <summary>
    /// 已监控的文件数量
    /// </summary>
    public int TrackedFileCount => _fileTimestamps.Count;

    #endregion

    #region 事件

    /// <summary>
    /// 文件变更检测到时触发
    /// </summary>
    public event EventHandler<FileChangeEventArgs>? FileChanged;

    /// <summary>
    /// 增量编译开始时触发
    /// </summary>
    public event EventHandler<HotReloadEventArgs>? CompilationStarted;

    /// <summary>
    /// 增量编译完成时触发
    /// </summary>
    public event EventHandler<HotReloadResultEventArgs>? CompilationCompleted;

    /// <summary>
    /// 模块替换完成时触发
    /// </summary>
    public event EventHandler<HotReloadResultEventArgs>? ModuleReloaded;

    /// <summary>
    /// 热重载出错时触发
    /// </summary>
    public event EventHandler<HotReloadErrorEventArgs>? ErrorOccurred;

    #endregion

    #region 公开方法

    /// <summary>
    /// 添加监控目录
    /// </summary>
    public void AddWatchDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            OnError($"目录不存在：{path}");
            return;
        }

        var fullPath = Path.GetFullPath(path);

        if (_watchDirectories.Contains(fullPath))
        {
            return;
        }

        _watchDirectories.Add(fullPath);
        ScanExistingFiles(fullPath);

        if (_isRunning)
        {
            CreateWatcher(fullPath);
        }
    }

    /// <summary>
    /// 移除监控目录
    /// </summary>
    public void RemoveWatchDirectory(string path)
    {
        var fullPath = Path.GetFullPath(path);

        if (!_watchDirectories.Remove(fullPath))
        {
            return;
        }

        var watcher = _watchers.FirstOrDefault(w =>
            Path.GetFullPath(w.Path) == fullPath);

        if (watcher != null)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            _watchers.Remove(watcher);
        }

        foreach (var key in _fileTimestamps.Keys.ToList())
        {
            if (key.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase))
            {
                _fileTimestamps.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// 启动文件监控
    /// </summary>
    public void Start()
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;

        foreach (var dir in _watchDirectories)
        {
            CreateWatcher(dir);
        }
    }

    /// <summary>
    /// 停止文件监控
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;

        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        _watchers.Clear();
    }

    /// <summary>
    /// 处理挂起的文件变更，触发增量编译
    /// </summary>
    /// <param name="compileAction">编译回调，接收变更文件路径列表，返回编译结果</param>
    public void ProcessPendingChanges(Func<IReadOnlyList<string>, HotReloadCompileResult> compileAction)
    {
        if (_isProcessing || _pendingChanges.IsEmpty)
        {
            return;
        }

        _isProcessing = true;

        try
        {
            var changes = CollectPendingChanges();

            if (changes.Count == 0)
            {
                return;
            }

            var changedFiles = changes.Select(c => c.FilePath).Distinct().ToList();

            OnCompilationStarted(changedFiles);

            var result = compileAction(changedFiles);

            var eventArgs = new HotReloadResultEventArgs(
                changedFiles,
                result.Success,
                result.CompiledModules,
                result.ErrorMessage,
                result.ElapsedMs
            );

            OnCompilationCompleted(eventArgs);

            if (result.Success)
            {
                OnModuleReloaded(eventArgs);
            }
        }
        catch (Exception ex)
        {
            OnError($"热重载处理异常：{ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <summary>
    /// 手动触发指定文件的热重载
    /// </summary>
    public void TriggerReload(string filePath, Func<string, HotReloadCompileResult> compileAction)
    {
        if (!File.Exists(filePath))
        {
            OnError($"文件不存在：{filePath}");
            return;
        }

        var fullPath = Path.GetFullPath(filePath);

        OnCompilationStarted(new List<string> { fullPath });

        try
        {
            var result = compileAction(fullPath);

            var eventArgs = new HotReloadResultEventArgs(
                new List<string> { fullPath },
                result.Success,
                result.CompiledModules,
                result.ErrorMessage,
                result.ElapsedMs
            );

            OnCompilationCompleted(eventArgs);

            if (result.Success)
            {
                OnModuleReloaded(eventArgs);
            }
        }
        catch (Exception ex)
        {
            OnError($"手动热重载异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 添加支持的文件扩展名
    /// </summary>
    public void AddSupportedExtension(string extension)
    {
        if (!extension.StartsWith('.'))
        {
            extension = $".{extension}";
        }

        _supportedExtensions.Add(extension);
    }

    /// <summary>
    /// 获取所有已跟踪的文件路径
    /// </summary>
    public IReadOnlyList<string> GetTrackedFiles()
    {
        return _fileTimestamps.Keys.ToList().AsReadOnly();
    }

    #endregion

    #region 文件监控

    private void CreateWatcher(string path)
    {
        var watcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileCreated;
        watcher.Deleted += OnFileDeleted;
        watcher.Renamed += OnFileRenamed;

        _watchers.Add(watcher);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!IsSupportedFile(e.FullPath))
        {
            return;
        }

        EnqueueChange(e.FullPath, FileChangeKind.Modified);
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (!IsSupportedFile(e.FullPath))
        {
            return;
        }

        _fileTimestamps[e.FullPath] = File.GetLastWriteTimeUtc(e.FullPath);
        EnqueueChange(e.FullPath, FileChangeKind.Created);
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (!IsSupportedFile(e.FullPath))
        {
            return;
        }

        _fileTimestamps.TryRemove(e.FullPath, out _);
        EnqueueChange(e.FullPath, FileChangeKind.Deleted);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (IsSupportedFile(e.OldFullPath))
        {
            _fileTimestamps.TryRemove(e.OldFullPath, out _);
        }

        if (IsSupportedFile(e.FullPath))
        {
            _fileTimestamps[e.FullPath] = File.GetLastWriteTimeUtc(e.FullPath);
            EnqueueChange(e.FullPath, FileChangeKind.Created);
        }
    }

    private void EnqueueChange(string filePath, FileChangeKind kind)
    {
        var change = new FileChangeEvent(filePath, kind, DateTime.UtcNow);
        _pendingChanges.Enqueue(change);

        OnFileChanged(filePath, kind);
    }

    private bool IsSupportedFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return _supportedExtensions.Contains(ext);
    }

    #endregion

    #region 文件扫描

    private void ScanExistingFiles(string directory)
    {
        try
        {
            foreach (var ext in _supportedExtensions)
            {
                var files = Directory.GetFiles(directory, $"*{ext}", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var fullPath = Path.GetFullPath(file);
                    _fileTimestamps[fullPath] = File.GetLastWriteTimeUtc(fullPath);
                }
            }
        }
        catch (DirectoryNotFoundException)
        {
            OnError($"扫描目录时目录不存在：{directory}");
        }
        catch (UnauthorizedAccessException)
        {
            OnError($"扫描目录时权限不足：{directory}");
        }
    }

    #endregion

    #region 变更收集

    private List<FileChangeEvent> CollectPendingChanges()
    {
        var changes = new List<FileChangeEvent>();
        var cutoff = DateTime.UtcNow.AddMilliseconds(-_debounceMs);

        while (_pendingChanges.TryDequeue(out var change))
        {
            if (change.Timestamp >= cutoff)
            {
                var existing = changes.FirstOrDefault(c => c.FilePath == change.FilePath);

                if (existing != null)
                {
                    if (change.Kind == FileChangeKind.Deleted)
                    {
                        changes.Remove(existing);
                        changes.Add(change);
                    }
                }
                else
                {
                    changes.Add(change);
                }
            }
        }

        return changes;
    }

    #endregion

    #region 事件触发

    private void OnFileChanged(string filePath, FileChangeKind kind)
    {
        FileChanged?.Invoke(this, new FileChangeEventArgs(filePath, kind));
    }

    private void OnCompilationStarted(IReadOnlyList<string> files)
    {
        CompilationStarted?.Invoke(this, new HotReloadEventArgs(files));
    }

    private void OnCompilationCompleted(HotReloadResultEventArgs args)
    {
        CompilationCompleted?.Invoke(this, args);
    }

    private void OnModuleReloaded(HotReloadResultEventArgs args)
    {
        ModuleReloaded?.Invoke(this, args);
    }

    private void OnError(string message)
    {
        ErrorOccurred?.Invoke(this, new HotReloadErrorEventArgs(message));
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Stop();
        _fileTimestamps.Clear();
        _pendingChanges.Clear();
    }

    #endregion
}

#region 事件参数

/// <summary>
/// 文件变更事件参数
/// </summary>
public sealed class FileChangeEventArgs : EventArgs
{
    public string FilePath { get; }
    public FileChangeKind ChangeKind { get; }
    public DateTime Timestamp { get; }

    public FileChangeEventArgs(string filePath, FileChangeKind changeKind)
    {
        FilePath = filePath;
        ChangeKind = changeKind;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// 热重载事件参数
/// </summary>
public sealed class HotReloadEventArgs : EventArgs
{
    public IReadOnlyList<string> AffectedFiles { get; }

    public HotReloadEventArgs(IReadOnlyList<string> affectedFiles)
    {
        AffectedFiles = affectedFiles;
    }
}

/// <summary>
/// 热重载结果事件参数
/// </summary>
public sealed class HotReloadResultEventArgs : EventArgs
{
    public IReadOnlyList<string> AffectedFiles { get; }
    public bool Success { get; }
    public IReadOnlyList<string> CompiledModules { get; }
    public string? ErrorMessage { get; }
    public double ElapsedMs { get; }

    public HotReloadResultEventArgs(
        IReadOnlyList<string> affectedFiles,
        bool success,
        IReadOnlyList<string> compiledModules,
        string? errorMessage,
        double elapsedMs)
    {
        AffectedFiles = affectedFiles;
        Success = success;
        CompiledModules = compiledModules;
        ErrorMessage = errorMessage;
        ElapsedMs = elapsedMs;
    }
}

/// <summary>
/// 热重载错误事件参数
/// </summary>
public sealed class HotReloadErrorEventArgs : EventArgs
{
    public string Message { get; }
    public DateTime Timestamp { get; }

    public HotReloadErrorEventArgs(string message)
    {
        Message = message;
        Timestamp = DateTime.UtcNow;
    }
}

#endregion

#region 数据类型

/// <summary>
/// 文件变更类型
/// </summary>
public enum FileChangeKind
{
    Created,
    Modified,
    Deleted
}

/// <summary>
/// 文件变更事件
/// </summary>
public sealed record FileChangeEvent(string FilePath, FileChangeKind Kind, DateTime Timestamp);

/// <summary>
/// 增量编译结果
/// </summary>
public sealed class HotReloadCompileResult
{
    public bool Success { get; init; }
    public IReadOnlyList<string> CompiledModules { get; init; } = Array.Empty<string>();
    public string? ErrorMessage { get; init; }
    public double ElapsedMs { get; init; }

    public static HotReloadCompileResult Succeeded(IReadOnlyList<string> modules, double elapsedMs)
    {
        return new HotReloadCompileResult
        {
            Success = true,
            CompiledModules = modules,
            ElapsedMs = elapsedMs
        };
    }

    public static HotReloadCompileResult Failed(string errorMessage, double elapsedMs)
    {
        return new HotReloadCompileResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ElapsedMs = elapsedMs
        };
    }
}

#endregion

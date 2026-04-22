namespace Gnosis.Database.Core;

public sealed class DatabaseOptions
{
    public string Path { get; set; } = ".genesis/db";

    public int BTreeOrder { get; set; } = 128;

    public int PageSize { get; set; } = 4096;

    public bool ReadOnly { get; set; }

    public bool AutoCheckpoint { get; set; } = true;

    public int CheckpointIntervalMs { get; set; } = 30000;

    public int PageCacheSize { get; set; } = 1024;

    public static DatabaseOptions Default => new();

    public SolidDB.Core.SolidOptions ToSolidOptions() => new()
    {
        Path = Path,
        BTreeOrder = BTreeOrder,
        PageSize = PageSize,
        ReadOnly = ReadOnly,
        AutoCheckpoint = AutoCheckpoint,
        CheckpointIntervalMs = CheckpointIntervalMs,
        PageCacheSize = PageCacheSize
    };
}

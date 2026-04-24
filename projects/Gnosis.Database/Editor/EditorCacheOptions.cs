namespace Gnosis.Database.Editor;

public sealed class EditorCacheOptions
{
    public TimeSpan HotKeyTtl { get; set; } = TimeSpan.FromMinutes(5);

    public int MaxHotEntries { get; set; } = 10000;

    public int MaxWarmEntries { get; set; } = 100000;

    public bool EnablePreload { get; set; } = true;

    public string[] PreloadPrefixes { get; set; } = ["config:", "meta:", "history:"];

    public static EditorCacheOptions Default => new();
}

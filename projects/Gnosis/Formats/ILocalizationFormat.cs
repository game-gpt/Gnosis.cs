namespace Gnosis.Formats;

public interface ILocalizationFormat : IFormatHandler
{
    Task<LocalizationData> LoadLocalizationAsync(string path, CancellationToken cancellationToken = default);
    Task SaveLocalizationAsync(string path, LocalizationData localization, CancellationToken cancellationToken = default);
    Task<LocalizationData> MergeAsync(LocalizationData baseData, LocalizationData overrideData, CancellationToken cancellationToken = default);
}

public record LocalizationData
{
    public string LocaleCode { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsRightToLeft { get; init; }
    public IReadOnlyDictionary<string, string> Entries { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, PluralForm> Plurals { get; init; } = new Dictionary<string, PluralForm>();
}

public record PluralForm
{
    public string Key { get; init; } = string.Empty;
    public IReadOnlyDictionary<PluralCategory, string> Forms { get; init; } = new Dictionary<PluralCategory, string>();
}

public enum PluralCategory
{
    Zero = 0,
    One = 1,
    Two = 2,
    Few = 3,
    Many = 4,
    Other = 5
}

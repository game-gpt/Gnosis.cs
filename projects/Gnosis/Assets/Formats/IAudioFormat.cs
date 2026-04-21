namespace Gnosis.Assets.Formats;

public interface IAudioFormat : IFormatHandler
{
    Task<AudioData> LoadAudioAsync(string path, CancellationToken cancellationToken = default);
    Task SaveAudioAsync(string path, AudioData audio, CancellationToken cancellationToken = default);
    Task<AudioData> ConvertAsync(AudioData audio, AudioEncoding targetEncoding, CancellationToken cancellationToken = default);
}

public record AudioData
{
    public string Name { get; init; } = string.Empty;
    public int Channels { get; init; }
    public int SampleRate { get; init; }
    public int BitsPerSample { get; init; }
    public float Duration { get; init; }
    public AudioEncoding Encoding { get; init; }
    public AudioCompression Compression { get; init; }
    public byte[] RawData { get; init; } = Array.Empty<byte>();
    public IReadOnlyList<AudioMarker> Markers { get; init; } = new List<AudioMarker>();
}

public record AudioMarker
{
    public float Time { get; init; }
    public string Name { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public enum AudioEncoding
{
    Unknown = 0,
    PCM = 1,
    ADPCM = 2,
    Vorbis = 3,
    Opus = 4,
    AAC = 5,
    MP3 = 6,
    FLAC = 7
}

public enum AudioCompression
{
    None = 0,
    Lossy = 1,
    Lossless = 2,
    Adaptive = 3
}

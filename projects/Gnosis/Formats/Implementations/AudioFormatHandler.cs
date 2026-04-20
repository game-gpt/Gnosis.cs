using System.Text.Json;
using Gnosis.Formats.Enums;
using Gnosis.Formats.Interfaces;

namespace Gnosis.Formats.Implementations;

public class AudioFormatHandler : FormatHandlerBase, IAudioFormat
{
    public override FormatType SupportedFormat => FormatType.Audio;
    
    public async Task<AudioData> LoadAudioAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<AudioData>(data) 
            ?? throw new InvalidOperationException($"Failed to deserialize audio: {path}");
    }
    
    public async Task SaveAudioAsync(string path, AudioData audio, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(audio, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await WriteAsync(path, data, null, cancellationToken);
    }
    
    public Task<AudioData> ConvertAsync(AudioData audio, AudioEncoding targetEncoding, CancellationToken cancellationToken = default)
    {
        var convertedAudio = audio with { Encoding = targetEncoding };
        
        return Task.FromResult(convertedAudio);
    }
    
    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".wav", ".mp3", ".ogg", ".aac", ".flac", ".ggaudio" };
    }
}

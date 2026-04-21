using System.Text.Json;
using NAudio.Wave;

namespace Gnosis.Assets.Formats;

/// <summary>
/// 音频格式处理器，支持 WAV、MP3 及引擎格式的音频加载、保存和转换
/// </summary>
public class AudioFormatHandler : FormatHandlerBase, IAudioFormat
{
    #region 常量

    private static readonly byte[] WavMagic = [0x52, 0x49, 0x46, 0x46];
    private static readonly byte[] Mp3Id3Magic = [0x49, 0x44, 0x33];
    private static readonly byte[] OggMagic = [0x4F, 0x67, 0x67, 0x53];
    private static readonly byte[] FlacMagic = [0x66, 0x4C, 0x61, 0x43];

    private const int MagicHeaderSize = 4;

    #endregion

    #region 公共方法

    public override FormatType SupportedFormat => FormatType.Audio;

    /// <summary>
    /// 异步加载音频文件，支持 WAV、MP3 和引擎格式
    /// </summary>
    public async Task<AudioData> LoadAudioAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!FileIO.Exists(path))
        {
            throw new FileNotFoundException($"未找到音频文件：{path}");
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();

        return extension switch
        {
            ".gnosis-audio" or ".scirptaudio" => await LoadEngineFormatAsync(path, cancellationToken),
            ".wav" => await LoadWavAsync(path, cancellationToken),
            ".mp3" => await LoadMp3Async(path, cancellationToken),
            ".ogg" => throw new NotSupportedException("OGG/Vorbis 格式需要 NAudio.Vorbis NuGet 包支持"),
            ".flac" => throw new NotSupportedException("FLAC 格式需要专用解码库支持"),
            ".aac" => throw new NotSupportedException("AAC 格式暂不支持"),
            _ => throw new NotSupportedException($"不支持的音频格式：{extension}")
        };
    }

    /// <summary>
    /// 异步保存音频数据到文件，支持引擎格式和 WAV 格式
    /// </summary>
    public async Task SaveAudioAsync(string path, AudioData audio, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        switch (extension)
        {
            case ".gnosis-audio":
            case ".scirptaudio":
                await SaveEngineFormatAsync(path, audio, cancellationToken);
                break;
            case ".wav":
                await SaveWavAsync(path, audio, cancellationToken);
                break;
            default:
                throw new NotSupportedException($"不支持的导出格式：{extension}");
        }
    }

    /// <summary>
    /// 异步转换音频编码格式，支持 PCM ↔ ADPCM 转换
    /// </summary>
    public Task<AudioData> ConvertAsync(AudioData audio, AudioEncoding targetEncoding, CancellationToken cancellationToken = default)
    {
        if (audio.Encoding == targetEncoding)
        {
            return Task.FromResult(audio);
        }

        return targetEncoding switch
        {
            AudioEncoding.PCM => ConvertToPcmAsync(audio, cancellationToken),
            AudioEncoding.ADPCM => ConvertToAdpcmAsync(audio, cancellationToken),
            AudioEncoding.Vorbis => throw new NotSupportedException("Vorbis 编码需要外部编码器库支持"),
            AudioEncoding.MP3 => throw new NotSupportedException("MP3 编码需要 NAudio.Lame NuGet 包支持"),
            AudioEncoding.FLAC => throw new NotSupportedException("FLAC 编码需要专用编码库支持"),
            AudioEncoding.AAC => throw new NotSupportedException("AAC 编码暂不支持"),
            AudioEncoding.Opus => throw new NotSupportedException("Opus 编码暂不支持"),
            _ => throw new NotSupportedException($"不支持的目标编码格式：{targetEncoding}")
        };
    }

    /// <summary>
    /// 验证音频文件格式，检查文件存在性和魔数签名
    /// </summary>
    public override async Task<bool> ValidateAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!FileIO.Exists(path))
        {
            return false;
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension is ".gnosis-audio" or ".scirptaudio")
        {
            return true;
        }

        var data = await ReadAsync(path, cancellationToken);

        if (data.Length < MagicHeaderSize)
        {
            return false;
        }

        return extension switch
        {
            ".wav" => CheckMagic(data, WavMagic),
            ".mp3" => CheckMagic(data, Mp3Id3Magic) || CheckMp3SyncWord(data),
            ".ogg" => CheckMagic(data, OggMagic),
            ".flac" => CheckMagic(data, FlacMagic),
            _ => true
        };
    }

    #endregion

    #region 加载方法

    private async Task<AudioData> LoadWavAsync(string path, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(path, cancellationToken);

        return await Task.Run(() =>
        {
            using var ms = new MemoryStream(data);
            using var reader = new WaveFileReader(ms);
            var pcmData = ReadAsPcm16(reader);
            var duration = (float)reader.TotalTime.TotalSeconds;

            return new AudioData
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Channels = reader.WaveFormat.Channels,
                SampleRate = reader.WaveFormat.SampleRate,
                BitsPerSample = 16,
                Duration = duration,
                Encoding = AudioEncoding.PCM,
                Compression = AudioCompression.None,
                RawData = pcmData
            };
        }, cancellationToken);
    }

    private async Task<AudioData> LoadMp3Async(string path, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(path, cancellationToken);

        return await Task.Run(() =>
        {
            using var ms = new MemoryStream(data);
            using var reader = new Mp3FileReader(ms);
            var pcmData = ReadAsPcm16(reader);
            var duration = (float)reader.TotalTime.TotalSeconds;

            return new AudioData
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Channels = reader.WaveFormat.Channels,
                SampleRate = reader.WaveFormat.SampleRate,
                BitsPerSample = 16,
                Duration = duration,
                Encoding = AudioEncoding.PCM,
                Compression = AudioCompression.Lossy,
                RawData = pcmData
            };
        }, cancellationToken);
    }

    private async Task<AudioData> LoadEngineFormatAsync(string path, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(path, cancellationToken);

        return JsonSerializer.Deserialize<AudioData>(data)
            ?? throw new InvalidOperationException($"音频数据反序列化失败：{path}");
    }

    #endregion

    #region 保存方法

    private async Task SaveEngineFormatAsync(string path, AudioData audio, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(audio, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await WriteAsync(path, data, null, cancellationToken);
    }

    private async Task SaveWavAsync(string path, AudioData audio, CancellationToken cancellationToken)
    {
        var wavData = await Task.Run(() =>
        {
            var format = new WaveFormat(audio.SampleRate, audio.BitsPerSample, audio.Channels);
            using var ms = new MemoryStream();
            using (var writer = new WaveFileWriter(ms, format))
            {
                writer.Write(audio.RawData, 0, audio.RawData.Length);
            }

            return ms.ToArray();
        }, cancellationToken);

        await WriteAsync(path, wavData, null, cancellationToken);
    }

    #endregion

    #region 转换方法

    private Task<AudioData> ConvertToPcmAsync(AudioData audio, CancellationToken cancellationToken)
    {
        if (audio.Encoding == AudioEncoding.PCM)
        {
            return Task.FromResult(audio);
        }

        if (audio.Encoding == AudioEncoding.ADPCM)
        {
            return ConvertAdpcmToPcmAsync(audio, cancellationToken);
        }

        throw new NotSupportedException($"不支持从 {audio.Encoding} 转换为 PCM");
    }

    private Task<AudioData> ConvertToAdpcmAsync(AudioData audio, CancellationToken cancellationToken)
    {
        if (audio.Encoding != AudioEncoding.PCM)
        {
            throw new ArgumentException("只能将 PCM 格式转换为 ADPCM", nameof(audio));
        }

        if (audio.BitsPerSample != 16)
        {
            throw new ArgumentException("ADPCM 转换需要 16 位 PCM 输入", nameof(audio));
        }

        return Task.Run(() =>
        {
            var pcmFormat = new WaveFormat(audio.SampleRate, 16, audio.Channels);
            using var pcmStream = new RawSourceWaveStream(audio.RawData, 0, audio.RawData.Length, pcmFormat);
            var adpcmFormat = new AdpcmWaveFormat(audio.SampleRate, audio.Channels);
            using var conversionStream = new WaveFormatConversionStream(adpcmFormat, pcmStream);
            using var ms = new MemoryStream();
            conversionStream.CopyTo(ms);
            var adpcmData = ms.ToArray();

            return audio with
            {
                Encoding = AudioEncoding.ADPCM,
                Compression = AudioCompression.Lossy,
                BitsPerSample = 4,
                RawData = adpcmData
            };
        }, cancellationToken);
    }

    private Task<AudioData> ConvertAdpcmToPcmAsync(AudioData audio, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var adpcmFormat = new AdpcmWaveFormat(audio.SampleRate, audio.Channels);
            using var adpcmStream = new RawSourceWaveStream(audio.RawData, 0, audio.RawData.Length, adpcmFormat);
            var pcmFormat = new WaveFormat(audio.SampleRate, 16, audio.Channels);
            using var conversionStream = new WaveFormatConversionStream(pcmFormat, adpcmStream);
            using var ms = new MemoryStream();
            conversionStream.CopyTo(ms);
            var pcmData = ms.ToArray();

            return audio with
            {
                Encoding = AudioEncoding.PCM,
                Compression = AudioCompression.None,
                BitsPerSample = 16,
                RawData = pcmData
            };
        }, cancellationToken);
    }

    #endregion

    #region 验证方法

    private static bool CheckMagic(byte[] data, byte[] magic)
    {
        if (data.Length < magic.Length)
        {
            return false;
        }

        for (var i = 0; i < magic.Length; i++)
        {
            if (data[i] != magic[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool CheckMp3SyncWord(byte[] data)
    {
        if (data.Length < 2)
        {
            return false;
        }

        return data[0] == 0xFF && (data[1] & 0xE0) == 0xE0;
    }

    #endregion

    #region 工具方法

    private static byte[] ReadAsPcm16(WaveStream reader)
    {
        if (reader.WaveFormat.Encoding == WaveFormatEncoding.Pcm &&
            reader.WaveFormat.BitsPerSample == 16)
        {
            return ReadAllBytes(reader);
        }

        if (reader.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat &&
            reader.WaveFormat.BitsPerSample == 32)
        {
            return ConvertFloat32ToPcm16(reader);
        }

        return ConvertViaAcm(reader);
    }

    private static byte[] ReadAllBytes(WaveStream reader)
    {
        var data = new byte[reader.Length];
        var offset = 0;

        while (offset < data.Length)
        {
            var read = reader.Read(data, offset, data.Length - offset);

            if (read == 0)
            {
                break;
            }

            offset += read;
        }

        if (offset < data.Length)
        {
            Array.Resize(ref data, offset);
        }

        return data;
    }

    private static byte[] ConvertFloat32ToPcm16(WaveStream reader)
    {
        var floatData = ReadAllBytes(reader);
        var sampleCount = floatData.Length / 4;
        var pcmData = new byte[sampleCount * 2];

        for (var i = 0; i < sampleCount; i++)
        {
            var sample = BitConverter.ToSingle(floatData, i * 4);
            var clamped = float.IsFinite(sample) ? Math.Clamp(sample, -1f, 1f) : 0f;
            var pcmSample = (short)(clamped * short.MaxValue);
            BitConverter.GetBytes(pcmSample).CopyTo(pcmData, i * 2);
        }

        return pcmData;
    }

    private static byte[] ConvertViaAcm(WaveStream reader)
    {
        var targetFormat = new WaveFormat(reader.WaveFormat.SampleRate, 16, reader.WaveFormat.Channels);
        using var conversionStream = new WaveFormatConversionStream(targetFormat, reader);
        using var ms = new MemoryStream();
        conversionStream.CopyTo(ms);

        return ms.ToArray();
    }

    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".gnosis-audio", ".wav", ".mp3", ".ogg", ".aac", ".flac", ".scirptaudio" };
    }

    #endregion
}

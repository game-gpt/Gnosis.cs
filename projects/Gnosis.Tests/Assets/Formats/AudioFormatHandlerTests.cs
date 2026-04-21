using Gnosis.Assets.Formats;
using Gnosis.Testing;
using NAudio.Wave;
using NUnit.Framework;

namespace Gnosis.Tests.Assets.Formats;

[TestFixture]
public class AudioFormatHandlerTests : GnosisTester
{
    #region 测试数据构建

    private AudioFormatHandler _handler = null!;
    private string _tempDir = null!;

    private static AudioData CreateTestAudioData(
        int sampleRate = 44100,
        int channels = 1,
        double durationSeconds = 1.0)
    {
        var sampleCount = (int)(sampleRate * durationSeconds);
        var rawData = new byte[sampleCount * channels * 2];

        for (var i = 0; i < sampleCount * channels; i++)
        {
            var sample = (short)(Math.Sin(2 * Math.PI * 440.0 * i / sampleRate) * short.MaxValue * 0.5);
            var bytes = BitConverter.GetBytes(sample);
            rawData[i * 2] = bytes[0];
            rawData[i * 2 + 1] = bytes[1];
        }

        return new AudioData
        {
            Name = "test",
            Channels = channels,
            SampleRate = sampleRate,
            BitsPerSample = 16,
            Duration = (float)durationSeconds,
            Encoding = AudioEncoding.PCM,
            Compression = AudioCompression.None,
            RawData = rawData
        };
    }

    private string CreateTestWavFile(
        string fileName,
        int sampleRate = 44100,
        int channels = 1,
        double durationSeconds = 1.0)
    {
        var path = Path.Combine(_tempDir, fileName);
        var format = new WaveFormat(sampleRate, 16, channels);
        var sampleCount = (int)(sampleRate * durationSeconds);

        using var ms = new MemoryStream();
        using (var writer = new WaveFileWriter(ms, format))
        {
            for (var i = 0; i < sampleCount * channels; i++)
            {
                var sample = (short)(Math.Sin(2 * Math.PI * 440.0 * i / sampleRate) * short.MaxValue * 0.5);
                var bytes = BitConverter.GetBytes(sample);
                writer.Write(bytes, 0, bytes.Length);
            }
        }

        File.WriteAllBytes(path, ms.ToArray());
        return path;
    }

    #endregion

    #region Setup / Teardown

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _handler = new AudioFormatHandler();
        _tempDir = Path.Combine(Path.GetTempPath(), $"GnosisAudioTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public override void Teardown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }

        base.Teardown();
    }

    #endregion

    #region LoadAudioAsync 测试

    [Test]
    public async Task LoadAudioAsync_Wav文件_返回正确的音频数据()
    {
        var path = CreateTestWavFile("test.wav");
        var audio = await _handler.LoadAudioAsync(path);

        Assert.That(audio, Is.Not.Null, "加载结果不应为 null");
        Assert.That(audio.Channels, Is.EqualTo(1), "声道数应为 1");
        Assert.That(audio.SampleRate, Is.EqualTo(44100), "采样率应为 44100");
        Assert.That(audio.BitsPerSample, Is.EqualTo(16), "位深应为 16");
        Assert.That(audio.Encoding, Is.EqualTo(AudioEncoding.PCM), "编码应为 PCM");
        Assert.That(audio.RawData, Is.Not.Empty, "原始数据不应为空");
        Assert.That(audio.Duration, Is.GreaterThan(0), "时长应大于 0");
    }

    [Test]
    public async Task LoadAudioAsync_Wav立体声_返回正确的声道数()
    {
        var path = CreateTestWavFile("stereo.wav", channels: 2);
        var audio = await _handler.LoadAudioAsync(path);

        Assert.That(audio.Channels, Is.EqualTo(2), "声道数应为 2");
    }

    [Test]
    public async Task LoadAudioAsync_引擎格式_返回正确的音频数据()
    {
        var audio = CreateTestAudioData();
        var path = Path.Combine(_tempDir, "test.gnosis-audio");
        await _handler.SaveAudioAsync(path, audio);

        var loaded = await _handler.LoadAudioAsync(path);

        Assert.That(loaded.Name, Is.EqualTo(audio.Name), "名称应一致");
        Assert.That(loaded.Channels, Is.EqualTo(audio.Channels), "声道数应一致");
        Assert.That(loaded.SampleRate, Is.EqualTo(audio.SampleRate), "采样率应一致");
    }

    [Test]
    public void LoadAudioAsync_Ogg文件_抛出NotSupportedException()
    {
        var path = Path.Combine(_tempDir, "test.ogg");
        File.WriteAllBytes(path, new byte[] { 0x4F, 0x67, 0x67, 0x53, 0x00, 0x00 });

        Assert.ThrowsAsync<NotSupportedException>(() => _handler.LoadAudioAsync(path));
    }

    [Test]
    public void LoadAudioAsync_Flac文件_抛出NotSupportedException()
    {
        var path = Path.Combine(_tempDir, "test.flac");
        File.WriteAllBytes(path, new byte[] { 0x66, 0x4C, 0x61, 0x43, 0x00, 0x00 });

        Assert.ThrowsAsync<NotSupportedException>(() => _handler.LoadAudioAsync(path));
    }

    [Test]
    public void LoadAudioAsync_Aac文件_抛出NotSupportedException()
    {
        var path = Path.Combine(_tempDir, "test.aac");
        File.WriteAllBytes(path, new byte[] { 0xFF, 0xF1, 0x50, 0x00 });

        Assert.ThrowsAsync<NotSupportedException>(() => _handler.LoadAudioAsync(path));
    }

    [Test]
    public void LoadAudioAsync_不存在的文件_抛出FileNotFoundException()
    {
        var path = Path.Combine(_tempDir, "nonexistent.wav");

        Assert.ThrowsAsync<FileNotFoundException>(() => _handler.LoadAudioAsync(path));
    }

    #endregion

    #region SaveAudioAsync 测试

    [Test]
    public async Task SaveAudioAsync_引擎格式_可以重新加载()
    {
        var audio = CreateTestAudioData();
        var path = Path.Combine(_tempDir, "test.gnosis-audio");

        await _handler.SaveAudioAsync(path, audio);
        var loaded = await _handler.LoadAudioAsync(path);

        Assert.That(loaded.Name, Is.EqualTo(audio.Name), "名称应一致");
        Assert.That(loaded.Channels, Is.EqualTo(audio.Channels), "声道数应一致");
        Assert.That(loaded.SampleRate, Is.EqualTo(audio.SampleRate), "采样率应一致");
        Assert.That(loaded.BitsPerSample, Is.EqualTo(audio.BitsPerSample), "位深应一致");
    }

    [Test]
    public async Task SaveAudioAsync_Wav格式_创建有效的Wav文件()
    {
        var audio = CreateTestAudioData();
        var path = Path.Combine(_tempDir, "output.wav");

        await _handler.SaveAudioAsync(path, audio);

        Assert.That(File.Exists(path), Is.True, "WAV 文件应存在");
        var data = File.ReadAllBytes(path);
        Assert.That(data[0], Is.EqualTo(0x52), "首字节应为 'R'");
        Assert.That(data[1], Is.EqualTo(0x49), "第二字节应为 'I'");
        Assert.That(data[2], Is.EqualTo(0x46), "第三字节应为 'F'");
        Assert.That(data[3], Is.EqualTo(0x46), "第四字节应为 'F'");
    }

    [Test]
    public async Task SaveAndLoad_Wav往返_保持音频属性()
    {
        var audio = CreateTestAudioData();
        var savePath = Path.Combine(_tempDir, "roundtrip.wav");

        await _handler.SaveAudioAsync(savePath, audio);
        var loaded = await _handler.LoadAudioAsync(savePath);

        Assert.That(loaded.Channels, Is.EqualTo(audio.Channels), "声道数应一致");
        Assert.That(loaded.SampleRate, Is.EqualTo(audio.SampleRate), "采样率应一致");
        Assert.That(loaded.BitsPerSample, Is.EqualTo(16), "位深应为 16");
        Assert.That(loaded.Encoding, Is.EqualTo(AudioEncoding.PCM), "编码应为 PCM");
        Assert.That(loaded.Duration, Is.GreaterThan(0), "时长应大于 0");
    }

    [Test]
    public void SaveAudioAsync_不支持的格式_抛出NotSupportedException()
    {
        var audio = CreateTestAudioData();
        var path = Path.Combine(_tempDir, "output.mp3");

        Assert.ThrowsAsync<NotSupportedException>(() => _handler.SaveAudioAsync(path, audio));
    }

    #endregion

    #region ConvertAsync 测试

    [Test]
    public async Task ConvertAsync_相同编码_返回相同音频()
    {
        var audio = CreateTestAudioData();
        var result = await _handler.ConvertAsync(audio, AudioEncoding.PCM);

        Assert.That(result.Encoding, Is.EqualTo(AudioEncoding.PCM), "编码应保持 PCM");
        Assert.That(result.RawData, Is.EqualTo(audio.RawData), "原始数据应不变");
    }

    [Test]
    public void ConvertAsync_Vorbis目标_抛出NotSupportedException()
    {
        var audio = CreateTestAudioData();

        Assert.ThrowsAsync<NotSupportedException>(() => _handler.ConvertAsync(audio, AudioEncoding.Vorbis));
    }

    [Test]
    public void ConvertAsync_Mp3目标_抛出NotSupportedException()
    {
        var audio = CreateTestAudioData();

        Assert.ThrowsAsync<NotSupportedException>(() => _handler.ConvertAsync(audio, AudioEncoding.MP3));
    }

    [Test]
    public void ConvertAsync_Flac目标_抛出NotSupportedException()
    {
        var audio = CreateTestAudioData();

        Assert.ThrowsAsync<NotSupportedException>(() => _handler.ConvertAsync(audio, AudioEncoding.FLAC));
    }

    [Test]
    public void ConvertAsync_Opus目标_抛出NotSupportedException()
    {
        var audio = CreateTestAudioData();

        Assert.ThrowsAsync<NotSupportedException>(() => _handler.ConvertAsync(audio, AudioEncoding.Opus));
    }

    [Test]
    public void ConvertAsync_Aac目标_抛出NotSupportedException()
    {
        var audio = CreateTestAudioData();

        Assert.ThrowsAsync<NotSupportedException>(() => _handler.ConvertAsync(audio, AudioEncoding.AAC));
    }

    [Test]
    public async Task ConvertAsync_Pcm转Adpcm_返回Adpcm音频()
    {
        var audio = CreateTestAudioData(sampleRate: 44100, channels: 1, durationSeconds: 1.0);
        var result = await _handler.ConvertAsync(audio, AudioEncoding.ADPCM);

        Assert.That(result.Encoding, Is.EqualTo(AudioEncoding.ADPCM), "编码应为 ADPCM");
        Assert.That(result.BitsPerSample, Is.EqualTo(4), "位深应为 4");
        Assert.That(result.Compression, Is.EqualTo(AudioCompression.Lossy), "压缩类型应为 Lossy");
        Assert.That(result.RawData, Is.Not.Empty, "原始数据不应为空");
        Assert.That(result.RawData.Length, Is.LessThan(audio.RawData.Length), "ADPCM 数据应小于 PCM 数据");
    }

    [Test]
    public void ConvertAsync_非Pcm转Adpcm_抛出ArgumentException()
    {
        var audio = CreateTestAudioData() with { Encoding = AudioEncoding.Vorbis };

        Assert.ThrowsAsync<ArgumentException>(() => _handler.ConvertAsync(audio, AudioEncoding.ADPCM));
    }

    [Test]
    public async Task ConvertAsync_Adpcm转Pcm_返回Pcm音频()
    {
        var pcmAudio = CreateTestAudioData(sampleRate: 44100, channels: 1, durationSeconds: 1.0);
        var adpcmAudio = await _handler.ConvertAsync(pcmAudio, AudioEncoding.ADPCM);
        var result = await _handler.ConvertAsync(adpcmAudio, AudioEncoding.PCM);

        Assert.That(result.Encoding, Is.EqualTo(AudioEncoding.PCM), "编码应为 PCM");
        Assert.That(result.BitsPerSample, Is.EqualTo(16), "位深应为 16");
        Assert.That(result.Compression, Is.EqualTo(AudioCompression.None), "压缩类型应为 None");
        Assert.That(result.RawData, Is.Not.Empty, "原始数据不应为空");
    }

    #endregion

    #region ValidateAsync 测试

    [Test]
    public async Task ValidateAsync_有效的Wav文件_返回True()
    {
        var path = CreateTestWavFile("valid.wav");
        var result = await _handler.ValidateAsync(path);

        Assert.That(result, Is.True, "有效的 WAV 文件应通过验证");
    }

    [Test]
    public async Task ValidateAsync_无效的Wav文件_返回False()
    {
        var path = Path.Combine(_tempDir, "invalid.wav");
        File.WriteAllBytes(path, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

        var result = await _handler.ValidateAsync(path);

        Assert.That(result, Is.False, "无效的 WAV 文件不应通过验证");
    }

    [Test]
    public async Task ValidateAsync_不存在的文件_返回False()
    {
        var result = await _handler.ValidateAsync("nonexistent.wav");

        Assert.That(result, Is.False, "不存在的文件不应通过验证");
    }

    [Test]
    public async Task ValidateAsync_Mp3文件ID3头_返回True()
    {
        var path = Path.Combine(_tempDir, "id3.mp3");
        File.WriteAllBytes(path, new byte[] { 0x49, 0x44, 0x33, 0x03, 0x00, 0x00, 0x00, 0x00 });

        var result = await _handler.ValidateAsync(path);

        Assert.That(result, Is.True, "带 ID3 头的 MP3 文件应通过验证");
    }

    [Test]
    public async Task ValidateAsync_Mp3同步字_返回True()
    {
        var path = Path.Combine(_tempDir, "sync.mp3");
        File.WriteAllBytes(path, new byte[] { 0xFF, 0xFB, 0x90, 0x00, 0x00, 0x00, 0x00, 0x00 });

        var result = await _handler.ValidateAsync(path);

        Assert.That(result, Is.True, "带同步字的 MP3 文件应通过验证");
    }

    [Test]
    public async Task ValidateAsync_Ogg文件_返回True()
    {
        var path = Path.Combine(_tempDir, "test.ogg");
        File.WriteAllBytes(path, new byte[] { 0x4F, 0x67, 0x67, 0x53, 0x00, 0x00, 0x00, 0x00 });

        var result = await _handler.ValidateAsync(path);

        Assert.That(result, Is.True, "有效的 OGG 文件应通过验证");
    }

    [Test]
    public async Task ValidateAsync_Flac文件_返回True()
    {
        var path = Path.Combine(_tempDir, "test.flac");
        File.WriteAllBytes(path, new byte[] { 0x66, 0x4C, 0x61, 0x43, 0x00, 0x00, 0x00, 0x00 });

        var result = await _handler.ValidateAsync(path);

        Assert.That(result, Is.True, "有效的 FLAC 文件应通过验证");
    }

    [Test]
    public async Task ValidateAsync_引擎格式_返回True()
    {
        var path = Path.Combine(_tempDir, "test.gnosis-audio");
        File.WriteAllBytes(path, "{}"u8.ToArray());

        var result = await _handler.ValidateAsync(path);

        Assert.That(result, Is.True, "引擎格式文件应通过验证");
    }

    [Test]
    public async Task ValidateAsync_文件过小_返回False()
    {
        var path = Path.Combine(_tempDir, "small.wav");
        File.WriteAllBytes(path, new byte[] { 0x52 });

        var result = await _handler.ValidateAsync(path);

        Assert.That(result, Is.False, "过小的文件不应通过验证");
    }

    [Test]
    public async Task ValidateAsync_无效的Mp3文件_返回False()
    {
        var path = Path.Combine(_tempDir, "invalid.mp3");
        File.WriteAllBytes(path, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

        var result = await _handler.ValidateAsync(path);

        Assert.That(result, Is.False, "无效的 MP3 文件不应通过验证");
    }

    #endregion

    #region CanHandle 测试

    [Test]
    public void CanHandle_Wav扩展名_返回True()
    {
        Assert.That(_handler.CanHandle("test.wav"), Is.True, "应支持 .wav 扩展名");
    }

    [Test]
    public void CanHandle_Mp3扩展名_返回True()
    {
        Assert.That(_handler.CanHandle("test.mp3"), Is.True, "应支持 .mp3 扩展名");
    }

    [Test]
    public void CanHandle_引擎格式扩展名_返回True()
    {
        Assert.That(_handler.CanHandle("test.gnosis-audio"), Is.True, "应支持 .gnosis-audio 扩展名");
    }

    [Test]
    public void CanHandle_不支持的扩展名_返回False()
    {
        Assert.That(_handler.CanHandle("test.xyz"), Is.False, "不应支持 .xyz 扩展名");
    }

    #endregion
}

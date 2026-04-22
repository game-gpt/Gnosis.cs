using Gnosis.Audio.Driver;
using Gnosis.Audio.Source;
using Gnosis.Core.StoryCommands;
using Gnosis.Runtime.Interop;
using Gnosis.Runtime.VM;

namespace Gnosis.Audio.StoryCommands;

public sealed class AudioCommands
{
    #region 字段

    private readonly IAudioSystem _audioSystem;
    private IAudioSource? _bgmSource;
    private readonly Dictionary<string, IAudioSource> _seSources = new();

    #endregion

    #region 构造函数

    public AudioCommands(IAudioSystem audioSystem)
    {
        _audioSystem = audioSystem;
    }

    #endregion

    #region BGM 命令

    /// <summary>
    /// 播放背景音乐
    /// Story 语法: %audio::play("bgm.mp3", 0.6)
    /// 参数: args[0] = 音频路径, args[1] = 音量 (可选, 默认 1.0)
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.AudioPlay, StoryCommandIds.AudioPlay)]
    public object? StoryAudioPlay(IVMState vm, object?[] args)
    {
        var path = args.ElementAtOrDefault(0)?.ToString();

        if (path is null)
        {
            return null;
        }

        var volume = Convert.ToSingle(args.ElementAtOrDefault(1) ?? 1.0);

        if (_bgmSource is not null)
        {
            _bgmSource.Stop();
            _audioSystem.DestroySource(_bgmSource);
        }

        _bgmSource = _audioSystem.CreateSource();
        _bgmSource.Clip = _audioSystem.LoadClip(path);
        _bgmSource.Volume = volume;
        _bgmSource.IsLooping = true;
        _bgmSource.Play();

        return null;
    }

    /// <summary>
    /// 停止背景音乐
    /// Story 语法: %audio::stop()
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.AudioStop, StoryCommandIds.AudioStop)]
    public object? StoryAudioStop(IVMState vm, object?[] args)
    {
        if (_bgmSource is not null)
        {
            _bgmSource.Stop();
            _audioSystem.DestroySource(_bgmSource);
            _bgmSource = null;
        }

        return null;
    }

    /// <summary>
    /// 播放音效
    /// Story 语法: %audio::play_se("se.mp3", 0.8)
    /// 参数: args[0] = 音频路径, args[1] = 音量 (可选, 默认 1.0)
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.AudioPlaySe, StoryCommandIds.AudioPlaySe)]
    public object? StoryAudioPlaySe(IVMState vm, object?[] args)
    {
        var path = args.ElementAtOrDefault(0)?.ToString();

        if (path is null)
        {
            return null;
        }

        var volume = Convert.ToSingle(args.ElementAtOrDefault(1) ?? 1.0);

        var source = _audioSystem.CreateSource();
        source.Clip = _audioSystem.LoadClip(path);
        source.Volume = volume;
        source.IsLooping = false;
        source.Play();

        _seSources[path] = source;

        return null;
    }

    /// <summary>
    /// 淡出背景音乐
    /// Story 语法: %audio::fade_out(2.0)
    /// 参数: args[0] = 淡出时长 (秒, 可选, 默认 1.0)
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.AudioFadeOut, StoryCommandIds.AudioFadeOut)]
    public object? StoryAudioFadeOut(IVMState vm, object?[] args)
    {
        if (_bgmSource is null)
        {
            return null;
        }

        var duration = Convert.ToSingle(args.ElementAtOrDefault(0) ?? 1.0);
        _bgmSource.FadeOut(duration);

        return null;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 清理所有音频源
    /// </summary>
    public void Cleanup()
    {
        if (_bgmSource is not null)
        {
            _audioSystem.DestroySource(_bgmSource);
            _bgmSource = null;
        }

        foreach (var source in _seSources.Values)
        {
            _audioSystem.DestroySource(source);
        }

        _seSources.Clear();
    }

    #endregion
}

using Gnosis.Audio.Driver;
using Gnosis.Audio.Source;
using Gnosis.Core.StoryCommands;
using Gnosis.Runtime.Interop;
using Gnosis.Runtime.VM;

namespace Gnosis.Audio.StoryCommands;

public sealed class AudioCommands : IStoryAudioCommands
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
    /// </summary>
    public void PlayBgm(string path, float volume)
    {
        if (path is null)
        {
            return;
        }

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
    }

    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopBgm()
    {
        if (_bgmSource is not null)
        {
            _bgmSource.Stop();
            _audioSystem.DestroySource(_bgmSource);
            _bgmSource = null;
        }
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    public void PlaySe(string path, float volume)
    {
        if (path is null)
        {
            return;
        }

        var source = _audioSystem.CreateSource();
        source.Clip = _audioSystem.LoadClip(path);
        source.Volume = volume;
        source.IsLooping = false;
        source.Play();

        _seSources[path] = source;
    }

    /// <summary>
    /// 淡出背景音乐
    /// </summary>
    public void FadeOutBgm(float duration)
    {
        if (_bgmSource is null)
        {
            return;
        }

        _bgmSource.FadeOut(duration);
    }

    #endregion

    #region VM 绑定方法

    [NativeFunctionBinding(StoryCommandNames.AudioPlay, StoryCommandIds.AudioPlay)]
    public object? StoryAudioPlay(IVMState vm, object?[] args)
    {
        var path = args.ElementAtOrDefault(0)?.ToString();
        var volume = Convert.ToSingle(args.ElementAtOrDefault(1) ?? 1.0);

        PlayBgm(path ?? "", volume);
        return null;
    }

    [NativeFunctionBinding(StoryCommandNames.AudioStop, StoryCommandIds.AudioStop)]
    public object? StoryAudioStop(IVMState vm, object?[] args)
    {
        StopBgm();
        return null;
    }

    [NativeFunctionBinding(StoryCommandNames.AudioPlaySe, StoryCommandIds.AudioPlaySe)]
    public object? StoryAudioPlaySe(IVMState vm, object?[] args)
    {
        var path = args.ElementAtOrDefault(0)?.ToString();
        var volume = Convert.ToSingle(args.ElementAtOrDefault(1) ?? 1.0);

        PlaySe(path ?? "", volume);
        return null;
    }

    [NativeFunctionBinding(StoryCommandNames.AudioFadeOut, StoryCommandIds.AudioFadeOut)]
    public object? StoryAudioFadeOut(IVMState vm, object?[] args)
    {
        var duration = Convert.ToSingle(args.ElementAtOrDefault(0) ?? 1.0);

        FadeOutBgm(duration);
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

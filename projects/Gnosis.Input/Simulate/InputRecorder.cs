using System.Diagnostics;
using Gnosis.Input.Device;

namespace Gnosis.Input.Simulate;

public sealed class InputRecorder
{
    #region 字段

    private readonly List<InputRecordEntry> _recordedEntries = new();
    private readonly Stopwatch _stopwatch = new();
    private int _playbackIndex;
    private float _playbackStartTime;

    #endregion

    #region 属性

    public bool IsRecording { get; private set; }

    public bool IsPlaying { get; private set; }

    public float PlaybackSpeed { get; set; } = 1.0f;

    public bool LoopPlayback { get; set; }

    public IReadOnlyList<InputRecordEntry> RecordedEntries => _recordedEntries;

    #endregion

    #region 构造函数

    public InputRecorder()
    {
        _stopwatch.Start();
    }

    #endregion

    #region 录制

    public void StartRecording()
    {
        if (IsPlaying)
        {
            StopPlayback();
        }

        IsRecording = true;
        _recordedEntries.Clear();
        _stopwatch.Restart();
    }

    public void StopRecording()
    {
        IsRecording = false;
    }

    public void RecordEntry(InputDeviceType deviceType, string action, float value)
    {
        if (!IsRecording)
        {
            return;
        }

        _recordedEntries.Add(new InputRecordEntry
        {
            Timestamp = (float)_stopwatch.Elapsed.TotalSeconds,
            DeviceType = deviceType,
            Action = action,
            Value = value
        });
    }

    #endregion

    #region 回放

    public void PlayRecording()
    {
        if (_recordedEntries.Count == 0)
        {
            return;
        }

        IsPlaying = true;
        IsRecording = false;
        _playbackIndex = 0;
        _playbackStartTime = (float)_stopwatch.Elapsed.TotalSeconds;
    }

    public void StopPlayback()
    {
        IsPlaying = false;
    }

    public bool TryGetNextEntry(out InputRecordEntry entry)
    {
        entry = default;

        if (!IsPlaying || _playbackIndex >= _recordedEntries.Count)
        {
            if (IsPlaying && LoopPlayback && _recordedEntries.Count > 0)
            {
                _playbackIndex = 0;
                _playbackStartTime = (float)_stopwatch.Elapsed.TotalSeconds;
            }
            else
            {
                return false;
            }
        }

        var currentTime = (float)_stopwatch.Elapsed.TotalSeconds;
        var elapsed = (currentTime - _playbackStartTime) * PlaybackSpeed;

        if (elapsed >= _recordedEntries[_playbackIndex].Timestamp)
        {
            entry = _recordedEntries[_playbackIndex];
            _playbackIndex++;
            return true;
        }

        return false;
    }

    #endregion
}

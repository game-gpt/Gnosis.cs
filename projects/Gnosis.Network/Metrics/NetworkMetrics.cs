using System;
using System.Collections.Generic;

namespace Gnosis.Network.Metrics;

/// <summary>
/// 网络度量快照，记录某一时刻的网络质量指标
/// </summary>
public readonly record struct NetworkMetricSnapshot
{
    /// <summary>
    /// 往返延迟（毫秒）
    /// </summary>
    public float Rtt { get; init; }

    /// <summary>
    /// 丢包率（0.0 - 1.0）
    /// </summary>
    public float PacketLossRate { get; init; }

    /// <summary>
    /// 上行带宽（字节/秒）
    /// </summary>
    public float UploadBandwidth { get; init; }

    /// <summary>
    /// 下行带宽（字节/秒）
    /// </summary>
    public float DownloadBandwidth { get; init; }

    /// <summary>
    /// 延迟抖动（毫秒）
    /// </summary>
    public float Jitter { get; init; }

    /// <summary>
    /// 快照时间戳
    /// </summary>
    public long TimestampMs { get; init; }
}

/// <summary>
/// 网络度量系统，监控网络延迟、丢包率、带宽和抖动
/// </summary>
public sealed class NetworkMetrics
{
    #region 字段

    private readonly int _maxSampleCount;
    private readonly List<float> _rttSamples;
    private readonly List<float> _jitterSamples;
    private long _bytesSent;
    private long _bytesReceived;
    private int _packetsSent;
    private int _packetsReceived;
    private int _packetsLost;
    private long _lastBandwidthCalcTime;
    private float _lastUploadBandwidth;
    private float _lastDownloadBandwidth;
    private long _lastBytesSent;
    private long _lastBytesReceived;
    private float _smoothedRtt;
    private float _rttVariance;

    #endregion

    #region 属性

    /// <summary>
    /// 获取平滑往返延迟（毫秒）
    /// </summary>
    public float SmoothedRtt => _smoothedRtt;

    /// <summary>
    /// 获取当前丢包率（0.0 - 1.0）
    /// </summary>
    public float PacketLossRate
    {
        get
        {
            var total = _packetsSent;

            if (total == 0)
            {
                return 0;
            }

            return (float)_packetsLost / total;
        }
    }

    /// <summary>
    /// 获取当前延迟抖动（毫秒）
    /// </summary>
    public float Jitter => _jitterSamples.Count > 0 ? _jitterSamples[^1] : 0;

    /// <summary>
    /// 获取上行带宽（字节/秒）
    /// </summary>
    public float UploadBandwidth => _lastUploadBandwidth;

    /// <summary>
    /// 获取下行带宽（字节/秒）
    /// </summary>
    public float DownloadBandwidth => _lastDownloadBandwidth;

    /// <summary>
    /// 获取总发送字节数
    /// </summary>
    public long TotalBytesSent => _bytesSent;

    /// <summary>
    /// 获取总接收字节数
    /// </summary>
    public long TotalBytesReceived => _bytesReceived;

    /// <summary>
    /// 获取总发送包数
    /// </summary>
    public int TotalPacketsSent => _packetsSent;

    /// <summary>
    /// 获取总接收包数
    /// </summary>
    public int TotalPacketsReceived => _packetsReceived;

    /// <summary>
    /// 获取总丢包数
    /// </summary>
    public int TotalPacketsLost => _packetsLost;

    /// <summary>
    /// 获取或设置 RTT 平滑因子（0-1，越大越跟手）
    /// </summary>
    public float RttSmoothingFactor { get; set; } = 0.125f;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化网络度量系统
    /// </summary>
    /// <param name="maxSampleCount">最大采样数量</param>
    public NetworkMetrics(int maxSampleCount = 64)
    {
        _maxSampleCount = maxSampleCount;
        _rttSamples = new List<float>(maxSampleCount);
        _jitterSamples = new List<float>(maxSampleCount);
        _smoothedRtt = 0;
        _rttVariance = 0;
        _lastBandwidthCalcTime = Environment.TickCount64;
    }

    #endregion

    #region 公共方法 - 记录

    /// <summary>
    /// 记录一次 RTT 测量值
    /// </summary>
    /// <param name="rttMs">往返延迟（毫秒）</param>
    public void RecordRtt(float rttMs)
    {
        if (rttMs < 0)
        {
            return;
        }

        _rttSamples.Add(rttMs);

        if (_rttSamples.Count > _maxSampleCount)
        {
            _rttSamples.RemoveAt(0);
        }

        if (_smoothedRtt == 0)
        {
            _smoothedRtt = rttMs;
            _rttVariance = rttMs / 2;
        }
        else
        {
            var diff = rttMs - _smoothedRtt;
            _rttVariance = (1 - RttSmoothingFactor * 2) * _rttVariance + RttSmoothingFactor * 2 * Math.Abs(diff);
            _smoothedRtt = (1 - RttSmoothingFactor) * _smoothedRtt + RttSmoothingFactor * rttMs;
        }

        if (_rttSamples.Count >= 2)
        {
            var jitter = Math.Abs(_rttSamples[^1] - _rttSamples[^2]);
            _jitterSamples.Add(jitter);

            if (_jitterSamples.Count > _maxSampleCount)
            {
                _jitterSamples.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// 记录发送的数据包
    /// </summary>
    /// <param name="bytes">发送的字节数</param>
    public void RecordPacketSent(int bytes)
    {
        _packetsSent++;
        _bytesSent += bytes;
    }

    /// <summary>
    /// 记录接收的数据包
    /// </summary>
    /// <param name="bytes">接收的字节数</param>
    public void RecordPacketReceived(int bytes)
    {
        _packetsReceived++;
        _bytesReceived += bytes;
    }

    /// <summary>
    /// 记录丢包
    /// </summary>
    /// <param name="count">丢包数量</param>
    public void RecordPacketLoss(int count = 1)
    {
        _packetsLost += count;
    }

    #endregion

    #region 公共方法 - 更新

    /// <summary>
    /// 更新带宽统计（应在每帧调用）
    /// </summary>
    /// <param name="currentTimeMs">当前时间（毫秒）</param>
    public void UpdateBandwidth(long currentTimeMs)
    {
        var elapsed = currentTimeMs - _lastBandwidthCalcTime;

        if (elapsed < 1000)
        {
            return;
        }

        var elapsedSeconds = elapsed / 1000.0f;

        _lastUploadBandwidth = (_bytesSent - _lastBytesSent) / elapsedSeconds;
        _lastDownloadBandwidth = (_bytesReceived - _lastBytesReceived) / elapsedSeconds;

        _lastBytesSent = _bytesSent;
        _lastBytesReceived = _bytesReceived;
        _lastBandwidthCalcTime = currentTimeMs;
    }

    /// <summary>
    /// 获取当前网络度量快照
    /// </summary>
    /// <returns>度量快照</returns>
    public NetworkMetricSnapshot GetSnapshot()
    {
        return new NetworkMetricSnapshot
        {
            Rtt = _smoothedRtt,
            PacketLossRate = PacketLossRate,
            UploadBandwidth = _lastUploadBandwidth,
            DownloadBandwidth = _lastDownloadBandwidth,
            Jitter = Jitter,
            TimestampMs = Environment.TickCount64
        };
    }

    /// <summary>
    /// 获取平均 RTT
    /// </summary>
    /// <returns>平均 RTT（毫秒）</returns>
    public float GetAverageRtt()
    {
        if (_rttSamples.Count == 0)
        {
            return 0;
        }

        var sum = 0f;

        foreach (var rtt in _rttSamples)
        {
            sum += rtt;
        }

        return sum / _rttSamples.Count;
    }

    /// <summary>
    /// 获取最大 RTT
    /// </summary>
    /// <returns>最大 RTT（毫秒）</returns>
    public float GetMaxRtt()
    {
        if (_rttSamples.Count == 0)
        {
            return 0;
        }

        var max = 0f;

        foreach (var rtt in _rttSamples)
        {
            if (rtt > max)
            {
                max = rtt;
            }
        }

        return max;
    }

    /// <summary>
    /// 获取平均抖动
    /// </summary>
    /// <returns>平均抖动（毫秒）</returns>
    public float GetAverageJitter()
    {
        if (_jitterSamples.Count == 0)
        {
            return 0;
        }

        var sum = 0f;

        foreach (var j in _jitterSamples)
        {
            sum += j;
        }

        return sum / _jitterSamples.Count;
    }

    /// <summary>
    /// 重置所有度量数据
    /// </summary>
    public void Reset()
    {
        _rttSamples.Clear();
        _jitterSamples.Clear();
        _bytesSent = 0;
        _bytesReceived = 0;
        _packetsSent = 0;
        _packetsReceived = 0;
        _packetsLost = 0;
        _lastBandwidthCalcTime = Environment.TickCount64;
        _lastUploadBandwidth = 0;
        _lastDownloadBandwidth = 0;
        _lastBytesSent = 0;
        _lastBytesReceived = 0;
        _smoothedRtt = 0;
        _rttVariance = 0;
    }

    #endregion
}

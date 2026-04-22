using Gnosis.Database.Core;

namespace Gnosis.Database.Compaction;

public sealed class BackgroundCompactor : IDisposable
{
    #region 字段

    private readonly ICompactionStrategy _strategy;
    private readonly ICompactionContext _context;
    private readonly TimeSpan _interval;
    private readonly double _fragmentationThreshold;
    private Timer? _timer;
    private bool _disposed;
    private bool _isCompacting;

    #endregion

    #region 构造函数

    public BackgroundCompactor(
        ICompactionStrategy strategy,
        ICompactionContext context,
        TimeSpan? interval = null,
        double fragmentationThreshold = 0.3)
    {
        _strategy = strategy;
        _context = context;
        _interval = interval ?? TimeSpan.FromSeconds(30);
        _fragmentationThreshold = fragmentationThreshold;
    }

    #endregion

    #region 属性

    public bool IsRunning => _timer is not null;

    public bool IsCompacting => _isCompacting;

    #endregion

    #region 公开方法

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_timer is not null)
        {
            return;
        }

        _timer = new Timer(OnTimerCallback, null, _interval, _interval);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public async ValueTask CompactNowAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isCompacting)
        {
            return;
        }

        _isCompacting = true;
        try
        {
            await _strategy.CompactAsync(_context, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _isCompacting = false;
        }
    }

    #endregion

    #region 私有方法

    private void OnTimerCallback(object? state)
    {
        if (_isCompacting || _disposed)
        {
            return;
        }

        if (_context.FragmentationRatio < _fragmentationThreshold)
        {
            return;
        }

        _ = CompactNowAsync();
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }

    #endregion
}

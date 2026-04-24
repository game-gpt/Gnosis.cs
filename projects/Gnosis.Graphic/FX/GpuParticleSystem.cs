using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.FX;

/// <summary>
/// GPU 粒子系统，基于 Compute Shader 实现粒子模拟与渲染
/// </summary>
public sealed class GpuParticleSystem : IParticleSystem, IDisposable
{
    #region 字段

    private readonly IDevice _device;
    private readonly GpuParticleEmitter _emitter;
    private readonly GpuParticleRenderer _renderer;
    private readonly List<IParticleModule> _modules;
    private bool _isDisposed;
    private bool _isPlaying;
    private float _elapsedTime;

    #endregion

    #region 属性

    public string Name { get; }
    public IParticleEmitter Emitter => _emitter;
    public IParticleRenderer Renderer => _renderer;
    public IReadOnlyList<IParticleModule> Modules => _modules;
    public bool IsPlaying => _isPlaying;

    public int ActiveParticleCount => _emitter.AliveCount;

    public float Duration { get; set; }
    public bool IsLooping { get; set; }
    public float PlaybackSpeed { get; set; }

    /// <summary>
    /// GPU 发射器实例
    /// </summary>
    public GpuParticleEmitter GpuEmitter => _emitter;

    /// <summary>
    /// GPU 渲染器实例
    /// </summary>
    public GpuParticleRenderer GpuRenderer => _renderer;

    #endregion

    #region 构造函数

    public GpuParticleSystem(IDevice device, string name, int maxParticles = 10000)
    {
        _device = device;
        Name = name;
        _emitter = new GpuParticleEmitter(device, maxParticles);
        _renderer = new GpuParticleRenderer(device);
        _modules = [];

        Duration = 0.0f;
        IsLooping = true;
        PlaybackSpeed = 1.0f;
        _isPlaying = false;
        _elapsedTime = 0.0f;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 初始化粒子系统
    /// </summary>
    /// <param name="emitShader">发射着色器程序</param>
    /// <param name="simulateShader">模拟着色器程序</param>
    /// <param name="renderShader">渲染着色器程序</param>
    public void Initialize(IShaderProgram? emitShader, IShaderProgram? simulateShader, IShaderProgram? renderShader)
    {
        _emitter.Initialize(emitShader, simulateShader);
        _renderer.Initialize();

        if (renderShader is not null)
        {
            _renderer.ShaderProgram = renderShader;
            _renderer.RebuildPipeline();
        }
    }

    public void AddModule(IParticleModule module)
    {
        _modules.Add(module);
    }

    public void RemoveModule(string name)
    {
        for (int i = _modules.Count - 1; i >= 0; i--)
        {
            if (_modules[i].Name == name)
            {
                _modules.RemoveAt(i);
                return;
            }
        }
    }

    public T? GetModule<T>() where T : IParticleModule
    {
        foreach (var module in _modules)
        {
            if (module is T typed)
            {
                return typed;
            }
        }

        return default;
    }

    public void Play()
    {
        _isPlaying = true;
    }

    public void Stop()
    {
        _isPlaying = false;
        _elapsedTime = 0.0f;
        _emitter.Reset();
    }

    public void Pause()
    {
        _isPlaying = false;
    }

    public void Update(float delta)
    {
        if (!_isPlaying)
        {
            return;
        }

        float scaledDelta = delta * PlaybackSpeed;
        _elapsedTime += scaledDelta;

        if (Duration > 0 && _elapsedTime >= Duration)
        {
            if (IsLooping)
            {
                _elapsedTime -= Duration;
            }
            else
            {
                _isPlaying = false;
                return;
            }
        }

        var commandTable = _device.CreateCommandTable();
        commandTable.Begin();

        _emitter.Update(commandTable, scaledDelta);

        commandTable.End();
        _device.Submit(commandTable);
        commandTable.Dispose();
    }

    /// <summary>
    /// 渲染粒子
    /// </summary>
    /// <param name="commandTable">命令表</param>
    /// <param name="width">渲染目标宽度</param>
    /// <param name="height">渲染目标高度</param>
    public void Render(ICommandTable commandTable, uint width, uint height)
    {
        if (!_isPlaying || _emitter.AliveCount <= 0)
        {
            return;
        }

        if (_emitter.ParticleBuffer is null || _emitter.AliveBuffer is null)
        {
            return;
        }

        _renderer.Render(commandTable, _emitter.ParticleBuffer, _emitter.AliveBuffer, _emitter.AliveCount, width, height);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _emitter.Dispose();
        _renderer.Dispose();
        _modules.Clear();

        _isDisposed = true;
    }

    #endregion
}

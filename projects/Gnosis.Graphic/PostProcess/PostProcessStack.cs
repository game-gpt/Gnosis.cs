using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.PostProcess;

/// <summary>
/// 后处理栈，管理后处理效果的执行顺序和中间资源
/// </summary>
public sealed class PostProcessStack : IDisposable
{
    #region 字段

    private readonly IDevice _device;
    private readonly List<PostProcessEffect> _effects;
    private readonly Dictionary<string, PostProcessEffect> _effectsByName;
    private IResource? _intermediateTextureA;
    private IResource? _intermediateTextureB;
    private uint _width;
    private uint _height;
    private bool _isDisposed;

    #endregion

    #region 属性

    public IReadOnlyList<PostProcessEffect> Effects => _effects;
    public int EffectCount => _effects.Count;

    #endregion

    #region 构造函数

    public PostProcessStack(IDevice device)
    {
        _device = device;
        _effects = [];
        _effectsByName = [];
    }

    #endregion

    #region 公开方法 - 效果管理

    public T AddEffect<T>(T effect) where T : PostProcessEffect
    {
        effect.SetDevice(_device);
        effect.Initialize();

        _effects.Add(effect);
        _effectsByName[effect.Name] = effect;
        _effects.Sort((a, b) => a.Order.CompareTo(b.Order));
        return effect;
    }

    public bool RemoveEffect(PostProcessEffect effect)
    {
        if (_effects.Remove(effect))
        {
            _effectsByName.Remove(effect.Name);
            effect.Dispose();
            return true;
        }
        return false;
    }

    public bool RemoveEffect(string name)
    {
        if (_effectsByName.TryGetValue(name, out var effect))
        {
            return RemoveEffect(effect);
        }
        return false;
    }

    public T? GetEffect<T>(string name) where T : PostProcessEffect
    {
        return _effectsByName.GetValueOrDefault(name) is T typed ? typed : default;
    }

    public void ClearEffects()
    {
        foreach (var effect in _effects)
        {
            effect.Dispose();
        }

        _effects.Clear();
        _effectsByName.Clear();
    }

    #endregion

    #region 公开方法 - 渲染

    public void Resize(uint width, uint height)
    {
        if (width == _width && height == _height)
        {
            return;
        }

        _width = width;
        _height = height;

        _intermediateTextureA?.Dispose();
        _intermediateTextureB?.Dispose();

        _intermediateTextureA = CreateIntermediateTexture(width, height);
        _intermediateTextureB = CreateIntermediateTexture(width, height);
    }

    public void Execute(ICommandTable commandTable, IResource sceneColor, IResource outputTexture)
    {
        if (_effects.Count == 0)
        {
            return;
        }

        var enabledEffects = new List<PostProcessEffect>();
        foreach (var effect in _effects)
        {
            if (effect.Enabled)
            {
                enabledEffects.Add(effect);
            }
        }

        if (enabledEffects.Count == 0)
        {
            return;
        }

        EnsureIntermediateTextures();

        var currentInput = sceneColor;
        IResource currentOutput;

        for (int i = 0; i < enabledEffects.Count; i++)
        {
            var effect = enabledEffects[i];

            if (i == enabledEffects.Count - 1)
            {
                currentOutput = outputTexture;
            }
            else
            {
                currentOutput = i % 2 == 0 ? _intermediateTextureA! : _intermediateTextureB!;
            }

            effect.Setup(commandTable, _width, _height);
            effect.Execute(commandTable, currentInput, currentOutput);

            currentInput = currentOutput;
        }
    }

    #endregion

    #region 私有方法

    private void EnsureIntermediateTextures()
    {
        if (_intermediateTextureA is null || _intermediateTextureB is null)
        {
            _intermediateTextureA?.Dispose();
            _intermediateTextureB?.Dispose();

            _width = 1;
            _height = 1;
            _intermediateTextureA = CreateIntermediateTexture(1, 1);
            _intermediateTextureB = CreateIntermediateTexture(1, 1);
        }
    }

    private IResource CreateIntermediateTexture(uint width, uint height)
    {
        return _device.CreateTexture(new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = width,
            Height = height,
            Depth = 1,
            Format = ResourceFormat.R16G16B16A16Float,
            Usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource,
            MipLevels = 1,
            ArrayLayers = 1,
            SampleCount = 1
        });
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        ClearEffects();

        _intermediateTextureA?.Dispose();
        _intermediateTextureB?.Dispose();

        _isDisposed = true;
    }

    #endregion
}

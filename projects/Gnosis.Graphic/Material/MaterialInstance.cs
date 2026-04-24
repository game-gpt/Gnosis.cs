using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Material;

public sealed class MaterialInstance : IMaterial
{
    private readonly MaterialTemplate _template;
    private readonly Dictionary<string, object> _overrides = [];
    private readonly Dictionary<string, IResource> _textureBindings = [];
    private readonly Dictionary<string, IResource> _bufferBindings = [];

    private IShaderProgram? _shader;
    private ulong _shaderHandle;
    private BlendMode _blendMode = BlendMode.None;
    private CullMode _cullMode = CullMode.Back;
    private bool _depthTest = true;
    private bool _depthWrite = true;
    private CompareFunction _depthFunc = CompareFunction.Less;

    public MaterialTemplate Template => _template;
    public string Name { get; set; }
    public IShaderProgram Shader => _shader!;
    public ulong ShaderHandle => _shaderHandle;
    public BlendMode BlendMode => _blendMode;
    public CullMode CullMode => _cullMode;
    public bool DepthTest => _depthTest;
    public bool DepthWrite => _depthWrite;
    public CompareFunction DepthFunc => _depthFunc;

    public MaterialInstance(MaterialTemplate template, string? name = null)
    {
        _template = template;
        Name = name ?? $"Material_{template.Name}";

        foreach (var prop in template.Properties)
        {
            if (prop.DefaultValue != null)
            {
                _overrides[prop.Name] = prop.DefaultValue;
            }
        }
    }

    public T GetParameter<T>(string name) where T : struct
    {
        if (_overrides.TryGetValue(name, out var value) && value is T typed)
        {
            return typed;
        }

        var descriptor = _template.GetProperty(name);
        if (descriptor?.DefaultValue is T defaultVal)
        {
            return defaultVal;
        }

        return default;
    }

    public void SetParameter<T>(string name, T value) where T : struct
    {
        _overrides[name] = value;
    }

    public void SetTexture(string name, IResource texture)
    {
        _textureBindings[name] = texture;
    }

    public IResource? GetTexture(string name)
    {
        return _textureBindings.GetValueOrDefault(name);
    }

    public void SetBuffer(string name, IResource buffer)
    {
        _bufferBindings[name] = buffer;
    }

    public IResource? GetBuffer(string name)
    {
        return _bufferBindings.GetValueOrDefault(name);
    }

    public MaterialInstance SetShader(IShaderProgram shader)
    {
        _shader = shader;
        return this;
    }

    public MaterialInstance SetShader(ulong shaderHandle)
    {
        _shaderHandle = shaderHandle;
        return this;
    }

    public MaterialInstance SetBlendMode(BlendMode blendMode)
    {
        _blendMode = blendMode;
        return this;
    }

    public MaterialInstance SetCullMode(CullMode cullMode)
    {
        _cullMode = cullMode;
        return this;
    }

    public MaterialInstance SetDepthTest(bool enabled, bool write = true, CompareFunction func = CompareFunction.Less)
    {
        _depthTest = enabled;
        _depthWrite = write;
        _depthFunc = func;
        return this;
    }

    internal IReadOnlyDictionary<string, object> GetOverrides() => _overrides;
    internal IReadOnlyDictionary<string, IResource> GetTextureBindings() => _textureBindings;
    internal IReadOnlyDictionary<string, IResource> GetBufferBindings() => _bufferBindings;
}

using System.Text.Json;
using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

public class ShaderFormatHandler : IShaderFormat
{
    #region 属性

    public IReadOnlyList<string> SupportedExtensions => _supportedExtensions;

    #endregion

    #region 内部状态

    private static readonly IReadOnlyList<string> _supportedExtensions = [".shader", ".ggs"];

    private IShaderCompiler? _compiler;

    #endregion

    #region 编译器注入

    public void SetCompiler(IShaderCompiler compiler)
    {
        _compiler = compiler;
    }

    #endregion

    #region 公开方法

    public async Task<ShaderData> LoadShaderAsync(string path, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<ShaderData>(json)
            ?? throw new InvalidOperationException($"反序列化着色器失败：{path}");
    }

    public async Task SaveShaderAsync(string path, ShaderData shader, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(shader, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllBytesAsync(path, json, cancellationToken);
    }

    public Task<byte[]> CompileAsync(ShaderData shader, ShaderTarget target, CancellationToken cancellationToken = default)
    {
        if (_compiler == null)
        {
            throw new InvalidOperationException("未设置着色器编译器，请先调用 SetCompiler");
        }

        if (target == ShaderTarget.Spirv && shader.Language == ShaderLanguage.Valkyrie)
        {
            var module = _compiler.Compile(shader.SourceCode, shader.Name, new ShaderCompileOptions());
            return Task.FromResult(module.Bytecode);
        }

        throw new NotSupportedException($"不支持的编译目标或着色器语言：目标={target}，语言={shader.Language}");
    }

    #endregion
}

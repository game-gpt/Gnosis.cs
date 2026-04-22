using System.Text.Json;
using Gnosis.Asset.Format;
using Gnosis.Compiler.ShaderFrontend;

namespace Gnosis.Rendering.Shader;

public class ShaderFormatHandler : FormatHandlerBase, IShaderFormat
{
    #region 属性

    public override FormatType SupportedFormat => FormatType.Shader;

    #endregion

    #region 公开方法

    /// <summary>
    /// 从指定路径加载着色器数据
    /// </summary>
    public async Task<ShaderData> LoadShaderAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<ShaderData>(data)
            ?? throw new InvalidOperationException($"反序列化着色器失败：{path}");
    }

    /// <summary>
    /// 将着色器数据保存到指定路径
    /// </summary>
    public async Task SaveShaderAsync(string path, ShaderData shader, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(shader, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await WriteAsync(path, data, null, cancellationToken);
    }

    /// <summary>
    /// 编译着色器为指定目标格式的字节码
    /// </summary>
    public Task<byte[]> CompileAsync(ShaderData shader, ShaderTarget target, CancellationToken cancellationToken = default)
    {
        if (target == ShaderTarget.SPIRV && shader.Language == ShaderLanguage.GGShader)
        {
            var compiler = new Compiler.ShaderFrontend.ShaderCompiler();

            try
            {
                var bytecode = compiler.CompileToSpirV(shader.SourceCode);
                return Task.FromResult(bytecode);
            }
            catch (ShaderCompilationException ex)
            {
                throw new InvalidOperationException($"着色器编译失败：{shader.Name}，共 {ex.Errors.Count} 个错误", ex);
            }
        }

        throw new NotSupportedException($"不支持的编译目标或着色器语言：目标={target}，语言={shader.Language}");
    }

    #endregion

    #region 受保护方法

    /// <summary>
    /// 获取支持的文件扩展名列表
    /// </summary>
    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".shader", ".glsl", ".hlsl", ".vert", ".frag", ".comp", ".ggs" };
    }

    #endregion
}

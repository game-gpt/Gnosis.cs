using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

public delegate float[] MicroFunctionExecute(ReadOnlySpan<float> input, int index);

public sealed class DelegateMicroFunction : IMicroFunction
{
    public string Name { get; }
    public MicroFunctionKind Kind { get; }
    public IReadOnlyList<ShaderParameter> InputParameters { get; }
    public ShaderParameter OutputParameter { get; }
    public IReadOnlyList<ShaderUniform> Uniforms { get; }
    public IReadOnlyList<ShaderAttribute> Attributes { get; }
    public IReadOnlyList<ShaderSampler> Samplers { get; }

    public MicroFunctionExecute? Execute { get; set; }

    public DelegateMicroFunction(
        string name,
        MicroFunctionKind kind,
        MicroFunctionExecute? execute = null,
        IReadOnlyList<ShaderParameter>? inputParameters = null,
        ShaderParameter? outputParameter = null,
        IReadOnlyList<ShaderUniform>? uniforms = null,
        IReadOnlyList<ShaderAttribute>? attributes = null,
        IReadOnlyList<ShaderSampler>? samplers = null)
    {
        Name = name;
        Kind = kind;
        Execute = execute;
        InputParameters = inputParameters ?? new List<ShaderParameter>();
        OutputParameter = outputParameter ?? new ShaderParameter { Name = "output", Type = ShaderParameterType.Vec4 };
        Uniforms = uniforms ?? new List<ShaderUniform>();
        Attributes = attributes ?? new List<ShaderAttribute>();
        Samplers = samplers ?? new List<ShaderSampler>();
    }
}

public sealed class DelegateShaderModule : IShaderModule
{
    public string Name { get; }
    public IReadOnlyList<IMicroFunction> Functions { get; }
    public byte[] Bytecode { get; }
    public ShaderLanguage Language { get; }
    public ShaderTarget Target { get; }

    private readonly Dictionary<string, object> _uniforms = new();

    public DelegateShaderModule(
        string name,
        IReadOnlyList<IMicroFunction> functions,
        ShaderLanguage language = ShaderLanguage.Valkyrie,
        ShaderTarget target = ShaderTarget.Spirv,
        byte[]? bytecode = null)
    {
        Name = name;
        Functions = functions;
        Bytecode = bytecode ?? [];
        Language = language;
        Target = target;
    }

    public void SetUniform<T>(string name, T value) where T : struct
    {
        _uniforms[name] = value;
    }

    public T? GetUniform<T>(string name) where T : struct
    {
        if (_uniforms.TryGetValue(name, out var value) && value is T typed)
        {
            return typed;
        }
        return null;
    }

    public void Dispose()
    {
    }
}

public static class BuiltinShaderModules
{
    /// <summary>
    /// 创建 UI Uber Shader，支持 SDF 字体渲染、矩形、描边、阴影
    /// </summary>
    public static IShaderModule CreateUiShader()
    {
        var vertexFunc = new DelegateMicroFunction(
            "ui_vertex",
            MicroFunctionKind.Vertex,
            execute: (input, index) =>
            {
                return
                [
                    input[0],
                    input[1],
                    input[2],
                    1.0f,
                    input.Length > 3 ? input[3] : 1.0f,
                    input.Length > 4 ? input[4] : 1.0f,
                    input.Length > 5 ? input[5] : 1.0f,
                    input.Length > 6 ? input[6] : 1.0f
                ];
            }
        );

        var fragmentFunc = new DelegateMicroFunction(
            "ui_fragment",
            MicroFunctionKind.Fragment,
            execute: (interpolated, index) =>
            {
                return SdfFragmentShader(interpolated);
            },
            uniforms:
            [
                new ShaderUniform { Name = "u_sdf_spread", Type = ShaderUniformType.Float },
                new ShaderUniform { Name = "u_outline_width", Type = ShaderUniformType.Float },
                new ShaderUniform { Name = "u_outline_color", Type = ShaderUniformType.Float4 },
                new ShaderUniform { Name = "u_shadow_offset", Type = ShaderUniformType.Float2 },
                new ShaderUniform { Name = "u_shadow_width", Type = ShaderUniformType.Float },
                new ShaderUniform { Name = "u_shadow_color", Type = ShaderUniformType.Float4 },
                new ShaderUniform { Name = "u_style_id", Type = ShaderUniformType.Int },
                new ShaderUniform { Name = "u_smoothness", Type = ShaderUniformType.Float }
            ],
            samplers:
            [
                new ShaderSampler { Name = "u_sdf_atlas", Binding = 0, Type = ShaderSamplerType.Sampler2D }
            ]
        );

        return new DelegateShaderModule("ui_uber", [vertexFunc, fragmentFunc]);
    }

    /// <summary>
    /// SDF 片段着色器核心逻辑
    /// 输入布局：[0-2] 位置, [3] 深度, [4-7] 颜色 RGBA, [8-9] UV 坐标
    /// </summary>
    private static float[] SdfFragmentShader(ReadOnlySpan<float> interpolated)
    {
        var r = interpolated.Length > 4 ? interpolated[4] : 1.0f;
        var g = interpolated.Length > 5 ? interpolated[5] : 1.0f;
        var b = interpolated.Length > 6 ? interpolated[6] : 1.0f;
        var a = interpolated.Length > 7 ? interpolated[7] : 1.0f;

        if (interpolated.Length <= 9)
        {
            return [r, g, b, a];
        }

        var u = interpolated[8];
        var v = interpolated[9];

        float dist = SampleSdfAtlas(u, v);

        float spread = GetUniform(interpolated, 10, 0.5f);
        float outlineWidth = GetUniform(interpolated, 11, 0.0f);
        float outlineR = GetUniform(interpolated, 12, 0.0f);
        float outlineG = GetUniform(interpolated, 13, 0.0f);
        float outlineB = GetUniform(interpolated, 14, 0.0f);
        float outlineA = GetUniform(interpolated, 15, 0.0f);
        float shadowOffsetX = GetUniform(interpolated, 16, 0.0f);
        float shadowOffsetY = GetUniform(interpolated, 17, 0.0f);
        float shadowWidth = GetUniform(interpolated, 18, 0.0f);
        float shadowR = GetUniform(interpolated, 19, 0.0f);
        float shadowG = GetUniform(interpolated, 20, 0.0f);
        float shadowB = GetUniform(interpolated, 21, 0.0f);
        float shadowA = GetUniform(interpolated, 22, 0.0f);
        float smoothness = GetUniform(interpolated, 25, 0.02f);

        float edge = 0.5f;
        float smoothEdge = Math.Max(smoothness, 0.001f);

        float fillAlpha = SmoothStep(edge - smoothEdge, edge + smoothEdge, dist);

        float outlineAlpha = 0.0f;
        if (outlineWidth > 0.0f)
        {
            float outerEdge = edge + outlineWidth / spread;
            outlineAlpha = SmoothStep(edge - smoothEdge, edge + smoothEdge, dist)
                         * (1.0f - SmoothStep(outerEdge - smoothEdge, outerEdge + smoothEdge, dist));
        }

        float shadowAlpha = 0.0f;
        if (shadowWidth > 0.0f)
        {
            float shadowU = u - shadowOffsetX;
            float shadowV = v - shadowOffsetY;
            float shadowDist = SampleSdfAtlas(shadowU, shadowV);
            float shadowEdge = edge - shadowWidth / spread;
            shadowAlpha = SmoothStep(shadowEdge - smoothEdge, shadowEdge + smoothEdge, shadowDist);
        }

        float outR = shadowR * shadowAlpha * shadowA + outlineR * outlineAlpha + r * fillAlpha * a;
        float outG = shadowG * shadowAlpha * shadowA + outlineG * outlineAlpha + g * fillAlpha * a;
        float outB = shadowB * shadowAlpha * shadowA + outlineB * outlineAlpha + b * fillAlpha * a;
        float outA = Math.Min(shadowAlpha * shadowA + outlineAlpha + fillAlpha * a, 1.0f);

        return [outR, outG, outB, outA];
    }

    /// <summary>
    /// 模拟 SDF 图集采样（CPU 端占位实现，GPU 端由着色器纹理采样替代）
    /// </summary>
    private static float SampleSdfAtlas(float u, float v)
    {
        if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
        {
            return 0.0f;
        }

        return 0.5f + 0.5f * MathF.Sin(u * 6.283f) * MathF.Cos(v * 6.283f);
    }

    /// <summary>
    /// 从插值数据中获取 uniform 值
    /// </summary>
    private static float GetUniform(ReadOnlySpan<float> data, int index, float defaultValue)
    {
        return index < data.Length ? data[index] : defaultValue;
    }

    /// <summary>
    /// 平滑阶梯函数，用于 SDF 边缘抗锯齿
    /// </summary>
    private static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = Math.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
        return t * t * (3.0f - 2.0f * t);
    }

    public static IShaderModule CreateMeshShader()
    {
        var vertexFunc = new DelegateMicroFunction(
            "mesh_vertex",
            MicroFunctionKind.Vertex,
            execute: (input, index) =>
            {
                var x = input[0];
                var y = input[1];
                var z = input[2];

                var r = input.Length > 3 ? input[3] : 1.0f;
                var g = input.Length > 4 ? input[4] : 1.0f;
                var b = input.Length > 5 ? input[5] : 1.0f;

                return [x, y, z, 1.0f, r, g, b];
            }
        );

        var fragmentFunc = new DelegateMicroFunction(
            "mesh_fragment",
            MicroFunctionKind.Fragment,
            execute: (interpolated, index) =>
            {
                var r = interpolated.Length > 4 ? interpolated[4] : 1.0f;
                var g = interpolated.Length > 5 ? interpolated[5] : 1.0f;
                var b = interpolated.Length > 6 ? interpolated[6] : 1.0f;

                return [r, g, b, 1.0f];
            }
        );

        return new DelegateShaderModule("mesh_default", [vertexFunc, fragmentFunc]);
    }
}

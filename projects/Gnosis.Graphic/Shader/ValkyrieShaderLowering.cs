using Gnosis.IR.Shader;
using Oak.Syntax;
using Oak.Valkyrie.Lexer;

namespace Gnosis.Graphic.Shader;

internal sealed class ValkyrieShaderLowering
{
    #region 内部状态

    private readonly ShaderCompileOptions _options;
    private readonly Dictionary<string, ShaderIrType> _types = new();
    private uint _nextResultId = 1;

    #endregion

    #region 构造函数

    public ValkyrieShaderLowering(ShaderCompileOptions options)
    {
        _options = options;
        RegisterBuiltinTypes();
    }

    #endregion

    #region 公开方法

    public ShaderModuleIr LowerFromTokens(IReadOnlyList<GreenLeafNode> tokens, string moduleName)
    {
        var module = new ShaderModuleIr
        {
            Name = moduleName,
            Language = ShaderLanguage.Valkyrie,
            Target = ShaderTarget.Spirv
        };

        var i = 0;
        while (i < tokens.Count)
        {
            var token = tokens[i];

            if (token.Kind == ValkyrieNodeKind.Keyword && token.Text == "shader")
            {
                i = ParseShaderDecl(tokens, ref i, module);
            }
            else if (token.Kind == ValkyrieNodeKind.Keyword && token.Text == "struct")
            {
                i = ParseStructDecl(tokens, ref i, module);
            }
            else
            {
                i++;
            }
        }

        return module;
    }

    #endregion

    #region 私有方法 - Token 解析

    private int ParseShaderDecl(IReadOnlyList<GreenLeafNode> tokens, ref int i, ShaderModuleIr module)
    {
        i++;

        if (i >= tokens.Count) return i;
        var shaderName = tokens[i].Text;
        i++;

        if (i >= tokens.Count || tokens[i].Text != "{") return i;
        i++;

        while (i < tokens.Count && tokens[i].Text != "}")
        {
            var token = tokens[i];

            if (token.Kind == ValkyrieNodeKind.Keyword)
            {
                switch (token.Text)
                {
                    case "vertex":
                        i = ParseShaderStage(tokens, ref i, shaderName, ShaderExecutionModel.Vertex, module);
                        break;
                    case "fragment":
                        i = ParseShaderStage(tokens, ref i, shaderName, ShaderExecutionModel.Fragment, module);
                        break;
                    case "compute":
                        i = ParseShaderStage(tokens, ref i, shaderName, ShaderExecutionModel.GLCompute, module);
                        break;
                    case "uniform":
                        i = SkipToEndOfStatement(tokens, ref i);
                        break;
                    case "varying":
                        i = SkipToEndOfStatement(tokens, ref i);
                        break;
                    case "cbuffer":
                        i = SkipBlock(tokens, ref i);
                        break;
                    case "texture":
                        i = SkipToEndOfStatement(tokens, ref i);
                        break;
                    case "sampler":
                        i = SkipToEndOfStatement(tokens, ref i);
                        break;
                    default:
                        i++;
                        break;
                }
            }
            else
            {
                i++;
            }
        }

        if (i < tokens.Count && tokens[i].Text == "}") i++;

        return i;
    }

    private int ParseShaderStage(IReadOnlyList<GreenLeafNode> tokens, ref int i, string shaderName, ShaderExecutionModel model, ShaderModuleIr module)
    {
        i++;

        if (i >= tokens.Count) return i;
        var stageName = tokens[i].Text;
        i++;

        var funcName = $"{shaderName}_{stageName}";
        var func = new ShaderFunctionIr
        {
            Name = funcName,
            ReturnType = model == ShaderExecutionModel.Fragment
                ? ShaderIrType.Vec4()
                : ShaderIrType.Void,
            IsEntryPoint = true,
            EntryPointModel = model
        };

        module.Functions.Add(func);
        module.EntryPoints.Add(new ShaderEntryPointIr
        {
            Name = funcName,
            ExecutionModel = model,
            FunctionName = funcName
        });

        if (i < tokens.Count && tokens[i].Text == "{")
        {
            i = SkipBlock(tokens, ref i);
        }

        return i;
    }

    private int ParseStructDecl(IReadOnlyList<GreenLeafNode> tokens, ref int i, ShaderModuleIr module)
    {
        i++;

        if (i >= tokens.Count) return i;
        var structName = tokens[i].Text;
        i++;

        var fields = new List<ShaderStructFieldIr>();
        uint offset = 0;

        if (i < tokens.Count && tokens[i].Text == "{")
        {
            i++;

            while (i < tokens.Count && tokens[i].Text != "}")
            {
                if (tokens[i].Kind == ValkyrieNodeKind.Identifier)
                {
                    var fieldName = tokens[i].Text;
                    i++;

                    if (i < tokens.Count && tokens[i].Text == ":")
                    {
                        i++;
                        if (i < tokens.Count)
                        {
                            var fieldType = ResolveType(tokens[i].Text);
                            fields.Add(new ShaderStructFieldIr
                            {
                                Name = fieldName,
                                Type = fieldType,
                                Offset = offset
                            });
                            offset += (uint)fieldType.GetScalarSize();
                            i++;
                        }
                    }

                    if (i < tokens.Count && tokens[i].Text == ";") i++;
                }
                else
                {
                    i++;
                }
            }

            if (i < tokens.Count && tokens[i].Text == "}") i++;
        }

        var structIr = new ShaderStructIr
        {
            Name = structName
        };

        foreach (var field in fields)
        {
            structIr.Fields.Add(field);
        }

        module.Structs.Add(structIr);
        _types[structName] = new ShaderIrType.StructType(structName, fields);

        return i;
    }

    private int SkipToEndOfStatement(IReadOnlyList<GreenLeafNode> tokens, ref int i)
    {
        i++;
        while (i < tokens.Count && tokens[i].Text != ";")
        {
            i++;
        }
        if (i < tokens.Count) i++;
        return i;
    }

    private int SkipBlock(IReadOnlyList<GreenLeafNode> tokens, ref int i)
    {
        if (i >= tokens.Count || tokens[i].Text != "{") return i;
        i++;

        var depth = 1;
        while (i < tokens.Count && depth > 0)
        {
            if (tokens[i].Text == "{") depth++;
            else if (tokens[i].Text == "}") depth--;
            i++;
        }

        return i;
    }

    #endregion

    #region 私有方法 - 类型注册

    private void RegisterBuiltinTypes()
    {
        _types["void"] = ShaderIrType.Void;
        _types["bool"] = ShaderIrType.Bool;
        _types["f32"] = ShaderIrType.Float32;
        _types["i32"] = ShaderIrType.Int32;
        _types["u32"] = ShaderIrType.UInt32;
        _types["vec2"] = ShaderIrType.Vec2();
        _types["vec3"] = ShaderIrType.Vec3();
        _types["vec4"] = ShaderIrType.Vec4();
        _types["mat3"] = ShaderIrType.Mat3();
        _types["mat4"] = ShaderIrType.Mat4();
        _types["sampler2D"] = new ShaderIrType.SamplerType();
        _types["samplerCube"] = new ShaderIrType.SamplerType();
    }

    private ShaderIrType ResolveType(string typeName)
    {
        if (_types.TryGetValue(typeName, out var type))
        {
            return type;
        }

        return ShaderIrType.Float32;
    }

    private uint AllocateId()
    {
        return _nextResultId++;
    }

    #endregion
}

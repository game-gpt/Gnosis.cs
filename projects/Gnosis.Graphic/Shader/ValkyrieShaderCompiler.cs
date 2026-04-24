using Gnosis.Graphic.Shader.Spirv;
using Gnosis.IR.Shader;
using Oak.Valkyrie;
using Oak.Valkyrie.Lexer;

namespace Gnosis.Graphic.Shader;

public sealed class ValkyrieShaderCompiler : IShaderCompiler
{
    #region 属性

    public ShaderTarget Target => ShaderTarget.Spirv;

    #endregion

    #region 公开方法

    public IShaderModule Compile(string sourceCode, string moduleName, ShaderCompileOptions options)
    {
        var lexer = new ValkyrieLexer(ValkyrieLanguage.Shader);
        var tokens = lexer.Tokenize(sourceCode);

        var ir = ParseAndLower(tokens, moduleName, options);
        var spirv = GenerateSpirv(ir);

        if (options.OptimizationLevel != OptimizationLevel.None)
        {
            SpirvOptimizationLevel level;
            switch (options.OptimizationLevel)
            {
                case OptimizationLevel.Default:
                    level = SpirvOptimizationLevel.Default;
                    break;
                case OptimizationLevel.Maximum:
                    level = SpirvOptimizationLevel.Aggressive;
                    break;
                default:
                    level = SpirvOptimizationLevel.Minimal;
                    break;
            }

            spirv = new SpirvOptimizer().Optimize(spirv, level);
        }

        var functions = ir.EntryPoints.Select(MapEntryPoint).ToList<IMicroFunction>();

        return new DelegateShaderModule(
            moduleName,
            functions,
            ShaderLanguage.Valkyrie,
            ShaderTarget.Spirv,
            spirv);
    }

    public bool Validate(string sourceCode, out string errorMessage)
    {
        try
        {
            var lexer = new ValkyrieLexer(ValkyrieLanguage.Shader);
            lexer.Tokenize(sourceCode);

            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    #endregion

    #region 私有方法

    private ShaderModuleIr ParseAndLower(IReadOnlyList<Oak.Syntax.GreenLeafNode> tokens, string moduleName, ShaderCompileOptions options)
    {
        var lowering = new ValkyrieShaderLowering(options);
        return lowering.LowerFromTokens(tokens, moduleName);
    }

    private byte[] GenerateSpirv(ShaderModuleIr ir)
    {
        var generator = new SpirvGenerator();
        return generator.Generate(ir);
    }

    private static IMicroFunction MapEntryPoint(ShaderEntryPointIr entryPoint)
    {
        var kind = entryPoint.ExecutionModel switch
        {
            ShaderExecutionModel.Vertex => MicroFunctionKind.Vertex,
            ShaderExecutionModel.Fragment => MicroFunctionKind.Fragment,
            ShaderExecutionModel.GLCompute => MicroFunctionKind.Compute,
            ShaderExecutionModel.RayGenerationKHR => MicroFunctionKind.RayGen,
            _ => MicroFunctionKind.Vertex
        };

        return new DelegateMicroFunction(entryPoint.Name, kind);
    }

    #endregion
}

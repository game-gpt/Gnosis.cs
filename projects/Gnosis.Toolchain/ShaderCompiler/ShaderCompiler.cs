using Oak.Diagnostics;
using Oak.GGScript.AST;
using Oak.GGShader.Lexer;
using Oak.GGShader.Parser;
using Gnosis.Toolchain.ScriptCompiler.ScriptFrontend;
using Gnosis.Toolchain.ScriptCompiler.Backend;
using Gnosis.IR.Shader;
using Gnosis.Rendering.Backends.ShaderIR;
using Gnosis.Rendering.Backends.Spirv;

namespace Gnosis.Toolchain.ShaderCompiler;

public class ShaderCompiler : IShaderCompiler
{
    #region Fields

    private readonly DiagnosticSink _diagnostics;

    #endregion

    #region Constructors

    public ShaderCompiler(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new DiagnosticSink();
    }

    #endregion

    #region Properties

    public ShaderTarget Target => ShaderTarget.SPIRV;

    // 暴露诊断信息给调用方
    public DiagnosticSink Diagnostics => _diagnostics;

    #endregion

    #region Public Methods

    // 便捷重载，使用空的通道宏
    public byte[] CompileToSpirV(string source)
    {
        return CompileToSpirV(source, new ChannelMacros());
    }

    // 使用显式通道宏编译着色器到 SPIR-V
    public byte[] CompileToSpirV(string source, ChannelMacros macros)
    {
        var lexer = new GGShaderLexer(_diagnostics);
        var tokens = lexer.Tokenize(source);

        var parser = new GGShaderParser(_diagnostics);
        var ast = parser.Parse(tokens);

        var macroTable = new MacroTable();
        RegisterBuiltInMacros(macroTable);

        foreach (var (key, value) in macros.Macros)
        {
            macroTable.Add(key, value);
        }

        var evaluator = new MetaLanguageEvaluator(macroTable);
        ast = evaluator.Evaluate(ast, new ChannelMacros());

        var semanticAnalyzer = new ShaderSemanticAnalyzer(_diagnostics);
        if (ast is CompilationUnit unit)
        {
            ast = semanticAnalyzer.Analyze(unit);
        }

        if (_diagnostics.HasErrors)
        {
            throw new ShaderCompilationException(_diagnostics.Errors);
        }

        var irGenerator = new IrGenerator(_diagnostics);
        var ir = irGenerator.Generate((CompilationUnit)ast);

        var spirvGenerator = new SpirvGenerator();
        return spirvGenerator.Generate(ir);
    }

    public IShaderModule Compile(string sourceCode, string moduleName, ShaderCompileOptions options)
    {
        var bytecode = CompileToSpirV(sourceCode);

        var irGenerator = new IrGenerator(_diagnostics);
        var lexer = new GGShaderLexer(_diagnostics);
        var tokens = lexer.Tokenize(sourceCode);
        var parser = new GGShaderParser(_diagnostics);
        var ast = parser.Parse(tokens);
        var ir = irGenerator.Generate((CompilationUnit)ast);

        var functions = new List<IMicroFunction>();
        foreach (var funcIr in ir.Functions)
        {
            functions.Add(new DelegateMicroFunction(
                funcIr.Name,
                MapExecutionModelToKind(funcIr.ExecutionModel)));
        }

        return new DelegateShaderModule(moduleName, functions)
        {
        };
    }

    public bool Validate(string sourceCode, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            var lexer = new GGShaderLexer(_diagnostics);
            var tokens = lexer.Tokenize(sourceCode);

            var parser = new GGShaderParser(_diagnostics);
            var ast = parser.Parse(tokens);

            if (_diagnostics.Errors.Any())
            {
                errorMessage = string.Join("\n", _diagnostics.Errors.Select(e => e.Message));
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    #endregion

    #region Private Methods

    // 注册着色器内置宏的默认值
    private static void RegisterBuiltInMacros(MacroTable macroTable)
    {
        macroTable.Add("RENDER_MODE", "RASTER");
        macroTable.Add("TARGET_BACKEND", "SPIRV");
        macroTable.Add("HAS_RAY_TRACING", "false");
        macroTable.Add("HAS_NEURAL_RENDERING", "false");
        macroTable.Add("HAS_DIFFUSION", "false");
    }

    private static MicroFunctionKind MapExecutionModelToKind(ShaderExecutionModel? model) => model switch
    {
        ShaderExecutionModel.Vertex => MicroFunctionKind.Vertex,
        ShaderExecutionModel.Fragment => MicroFunctionKind.Fragment,
        ShaderExecutionModel.GLCompute => MicroFunctionKind.Compute,
        ShaderExecutionModel.RayGenerationKHR => MicroFunctionKind.RayGen,
        ShaderExecutionModel.ClosestHitKHR => MicroFunctionKind.RayClosestHit,
        ShaderExecutionModel.MissKHR => MicroFunctionKind.RayMiss,
        ShaderExecutionModel.AnyHitKHR => MicroFunctionKind.RayAnyHit,
        _ => MicroFunctionKind.Fragment
    };

    private void GenerateDeclarationSpirV(BinaryWriter writer, AstNode decl)
    {
        switch (decl.Type)
        {
            case NodeType.FunctionDecl:
                break;
            case NodeType.ComponentDecl:
                break;
            case NodeType.StructDecl:
                GenerateStructSpirV(writer, (StructDecl)decl);
                break;
            case NodeType.UniformBindingDecl:
                break;
            case NodeType.UsingDecl:
                break;
        }
    }

    private void GenerateStructSpirV(BinaryWriter writer, StructDecl decl)
    {
        writer.Write(0x0000001B);
        writer.Write(0x00000000);
    }

    #endregion
}
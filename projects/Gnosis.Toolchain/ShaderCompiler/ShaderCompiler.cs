using Oak.Diagnostics;
using Oak.Valkyrie;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;
using Gnosis.Toolchain.ScriptCompiler;
using Gnosis.Toolchain.ScriptCompiler.ScriptFrontend;
using Gnosis.Toolchain.ShaderCompiler.Backend;
using Gnosis.IR.Shader;
using Gnosis.Graphic.Shader;

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

    public ShaderTarget Target => ShaderTarget.Spirv;

    public DiagnosticSink Diagnostics => _diagnostics;

    #endregion

    #region Public Methods

    public byte[] CompileToSpirV(string source)
    {
        return CompileToSpirV(source, new ChannelMacros());
    }

    public byte[] CompileToSpirV(string source, ChannelMacros macros)
    {
        var ast = ParseToAst(source);

        var macroTable = new MacroTable();
        RegisterBuiltInMacros(macroTable);

        foreach (var (key, value) in macros.Macros)
        {
            macroTable.Add(key, value);
        }

        var evaluator = new MetaLanguageEvaluator(macroTable);
        ast = evaluator.Evaluate(ast, new ChannelMacros());

        if (ast is CompilationUnit unit)
        {
            var semanticAnalyzer = new ShaderSemanticAnalyzer(_diagnostics);
            ast = semanticAnalyzer.Analyze(unit);
        }

        if (_diagnostics.HasErrors)
        {
            throw new ShaderCompilationException(_diagnostics.Errors);
        }

        var lowering = new ShaderAstLowering(_diagnostics);
        var shaderModule = lowering.Lower((CompilationUnit)ast);

        var spirvGenerator = new Gnosis.Graphic.Shader.Spirv.SpirvGenerator();
        return spirvGenerator.Generate(shaderModule);
    }

    public IShaderModule Compile(string sourceCode, string moduleName, ShaderCompileOptions options)
    {
        var bytecode = CompileToSpirV(sourceCode);

        var lowering = new ShaderAstLowering(_diagnostics);
        var shaderModule = ParseAndLower(sourceCode);

        var functions = new List<IMicroFunction>();
        foreach (var entryPoint in shaderModule.EntryPoints)
        {
            functions.Add(new DelegateMicroFunction(
                entryPoint.FunctionName,
                MapExecutionModelToKind(entryPoint.ExecutionModel)));
        }

        return new DelegateShaderModule(moduleName, functions)
        {
            Bytecode = bytecode
        };
    }

    public bool Validate(string sourceCode, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            var ast = ParseToAst(sourceCode);

            if (_diagnostics.Errors.Any())
            {
                errorMessage = string.Join("\n", _diagnostics.Errors.Select(e => e.Message));
                return false;
            }

            return true;
        }
        catch (ShaderCompilationException ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    #endregion

    #region Private Methods

    private AstNode ParseToAst(string source)
    {
        var lexer = new ValkyrieLexer(_diagnostics);
        var tokens = lexer.Tokenize(source);

        var parser = new ValkyrieParser(ValkyrieLanguage.Shader, _diagnostics);
        return parser.Parse(tokens);
    }

    private ShaderModuleIr ParseAndLower(string source)
    {
        var ast = ParseToAst(source);

        if (ast is CompilationUnit unit)
        {
            var semanticAnalyzer = new ShaderSemanticAnalyzer(_diagnostics);
            var analyzed = semanticAnalyzer.Analyze(unit);

            var lowering = new ShaderAstLowering(_diagnostics);
            return lowering.Lower(analyzed);
        }

        throw new ShaderCompilationException(_diagnostics.Errors);
    }

    private static void RegisterBuiltInMacros(MacroTable macroTable)
    {
        macroTable.Add("RENDER_MODE", "RASTER");
        macroTable.Add("TARGET_BACKEND", "SPIRV");
        macroTable.Add("HAS_RAY_TRACING", "false");
        macroTable.Add("HAS_NEURAL_RENDERING", "false");
        macroTable.Add("HAS_DIFFUSION", "false");
    }

    private static MicroFunctionKind MapExecutionModelToKind(ShaderExecutionModel model) => model switch
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

    #endregion
}

using Oak.Diagnostics;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;
using Gnosis.Core.Diagnostic;
using Gnosis.IR.Debug;
using Gnosis.IR.Graph;
using Gnosis.IR.Instruction;
using Gnosis.IR.Transform;
using Gnosis.Toolchain.Compiler;
using Gnosis.Toolchain.ScriptCompiler.Backend;
using Gnosis.Toolchain.ScriptCompiler.Cache;
using MetaLanguageEvaluator = Gnosis.Toolchain.ScriptCompiler.ScriptFrontend.MetaLanguageEvaluator;

namespace Gnosis.Toolchain.ScriptCompiler;

public class Compiler : ICompiler
{
    #region Fields

    private readonly DiagnosticSink _diagnostics;
    private readonly ICompilationCache _cache;
    private readonly IMacroTable _macroTable;

    #endregion

    #region Constructors

    public Compiler(DiagnosticSink? diagnostics = null, ICompilationCache? cache = null, IMacroTable? macroTable = null)
    {
        _diagnostics = diagnostics ?? new DiagnosticSink();
        _cache = cache ?? new InMemoryCompilationCache();
        _macroTable = macroTable ?? new MacroTable();
    }

    #endregion

    #region Public Methods

    public CompilationResult Compile(
        IReadOnlyList<string> sourceFiles,
        ArchTarget arch,
        ChannelMacros macros,
        bool isEditorBuild = false)
    {
        var allDeclarations = new List<AstNode>();

        foreach (var sourceFile in sourceFiles)
        {
            if (!File.Exists(sourceFile))
            {
                _diagnostics.AddError(
                    sourceFile,
                    null,
                    "GG1001",
                    $"源文件不存在: {sourceFile}");
                continue;
            }

            var content = File.ReadAllText(sourceFile);

            var ast = CompileToAst(content, sourceFile, arch, macros, isEditorBuild);

            if (ast is CompilationUnit unit)
            {
                allDeclarations.AddRange(unit.Declarations);
            }
        }

        var compilationUnit = new CompilationUnit(allDeclarations);

        var evaluator = new MetaLanguageEvaluator(_macroTable);
        compilationUnit = (CompilationUnit)evaluator.Evaluate(compilationUnit, macros);

        var bytecodeGen = new BytecodeGenerator(_diagnostics);
        return bytecodeGen.GenerateFull(compilationUnit, arch, isEditorBuild);
    }

    public CompilationResult CompileSource(
        string source,
        string filePath,
        ArchTarget arch,
        ChannelMacros macros,
        bool isEditorBuild = false)
    {
        var ast = CompileToAst(source, filePath, arch, macros, isEditorBuild);

        var evaluator = new MetaLanguageEvaluator(_macroTable);
        ast = evaluator.Evaluate(ast, macros);

        var bytecodeGen = new BytecodeGenerator(_diagnostics);
        return bytecodeGen.GenerateFull(ast, arch, isEditorBuild);
    }

    public CompilationResult CompileOptimized(
        string source,
        string filePath,
        ArchTarget arch,
        ChannelMacros macros,
        int optimizationLevel = 2,
        bool isEditorBuild = false)
    {
        var ast = CompileToAst(source, filePath, arch, macros, isEditorBuild);

        var evaluator = new MetaLanguageEvaluator(_macroTable);
        ast = evaluator.Evaluate(ast, macros);

        if (ast is not CompilationUnit unit)
        {
            var bytecodeGen = new BytecodeGenerator(_diagnostics);
            return bytecodeGen.GenerateFull(ast, arch, isEditorBuild);
        }

        var irLowering = new AstIrLowering(_diagnostics);
        var irModule = irLowering.Lower(unit);

        if (optimizationLevel > 0)
        {
            var pipeline = new OptimizationPipeline(irModule, optimizationLevel);
            pipeline.Run();
        }

        var backend = new GnosisBytecodeBuilder();
        var options = new GnosisCompileOptions
        {
            OptimizationLevel = optimizationLevel,
            GenerateDebugInfo = true
        };

        var bytecodeUnit = backend.Compile(irModule, options);

        var bytecodeGen = new BytecodeGenerator(_diagnostics);
        var result = bytecodeGen.GenerateFull(ast, arch, isEditorBuild);

        var debugInfo = GenerateDebugInfo(bytecodeUnit, unit);

        return new CompilationResult(result.Bytecode, result.VmSourceCode, debugInfo);
    }

    public CompilationResult CompileOptimized(
        IReadOnlyList<string> sourceFiles,
        ArchTarget arch,
        ChannelMacros macros,
        int optimizationLevel = 2,
        bool isEditorBuild = false)
    {
        var allDeclarations = new List<AstNode>();

        foreach (var sourceFile in sourceFiles)
        {
            if (!File.Exists(sourceFile))
            {
                _diagnostics.AddError(
                    sourceFile,
                    null,
                    "GG1001",
                    $"源文件不存在: {sourceFile}");
                continue;
            }

            var content = File.ReadAllText(sourceFile);
            var ast = CompileToAst(content, sourceFile, arch, macros, isEditorBuild);

            if (ast is CompilationUnit unit)
            {
                allDeclarations.AddRange(unit.Declarations);
            }
        }

        var compilationUnit = new CompilationUnit(allDeclarations);

        var evaluator = new MetaLanguageEvaluator(_macroTable);
        compilationUnit = (CompilationUnit)evaluator.Evaluate(compilationUnit, macros);

        var irLowering = new AstIrLowering(_diagnostics);
        var irModule = irLowering.Lower(compilationUnit);

        if (optimizationLevel > 0)
        {
            var pipeline = new OptimizationPipeline(irModule, optimizationLevel);
            pipeline.Run();
        }

        var backend = new GnosisBytecodeBuilder();
        var options = new GnosisCompileOptions
        {
            OptimizationLevel = optimizationLevel,
            GenerateDebugInfo = true
        };

        var bytecodeUnit = backend.Compile(irModule, options);

        var bytecodeGen = new BytecodeGenerator(_diagnostics);
        var result = bytecodeGen.GenerateFull(compilationUnit, arch, isEditorBuild);

        var debugInfo = GenerateDebugInfo(bytecodeUnit, compilationUnit);

        return new CompilationResult(result.Bytecode, result.VmSourceCode, debugInfo);
    }

    #endregion

    #region Private Methods

    private AstNode CompileToAst(
        string source,
        string filePath,
        ArchTarget arch,
        ChannelMacros macros,
        bool isEditorBuild)
    {
        var lexer = new ValkyrieLexer();
        var tokens = lexer.Tokenize(source);

        var parser = new ValkyrieParser();
        return parser.Parse(tokens);
    }

    private static byte[]? GenerateDebugInfo(BytecodeUnit bytecodeUnit, CompilationUnit ast)
    {
        var debugGen = new DebugInfoGenerator();
        var debugUnit = debugGen.Generate(bytecodeUnit.ModuleName, bytecodeUnit.SourceMap, ast);

        var serializer = new DebugInfoSerializer();
        return serializer.Serialize(debugUnit);
    }

    #endregion
}

using Oak.Core.Diagnostics;
using Oak.GGScript.AST;
using Oak.GGScript.Lexer;
using Oak.GGScript.Parser;
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

    #endregion

    #region Private Methods

    private AstNode CompileToAst(
        string source,
        string filePath,
        ArchTarget arch,
        ChannelMacros macros,
        bool isEditorBuild)
    {
        var lexer = new GGScriptLexer();
        var tokens = lexer.Tokenize(source);

        var parser = new GGScriptParser();
        return parser.Parse(tokens);
    }

    #endregion
}

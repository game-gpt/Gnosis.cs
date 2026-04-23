using Oak.Diagnostics;
using Oak.Verse.AST;
using Oak.Verse.Lexer;
using Oak.Verse.Parser;
using Gnosis.IR.Graph;
using Gnosis.IR.Instruction;
using Gnosis.Toolchain.Compiler;
using Gnosis.Toolchain.VerseCompiler.Frontend;
using Gnosis.Toolchain.VerseCompiler.Backend;

namespace Gnosis.Toolchain.VerseCompiler;

/// <summary>
/// Verse 编译结果
/// </summary>
public sealed class VerseCompilationResult
{
    /// <summary>
    /// 是否编译成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 编译产生的 IR 模块
    /// </summary>
    public IrModule? IrModule { get; init; }

    /// <summary>
    /// 编译产生的字节码单元
    /// </summary>
    public BytecodeUnit? BytecodeUnit { get; init; }

    /// <summary>
    /// 诊断消息
    /// </summary>
    public IReadOnlyList<DiagnosticMessage> Diagnostics { get; init; } = [];

    /// <summary>
    /// IR 文本表示
    /// </summary>
    public string? IrText { get; init; }
}

/// <summary>
/// Verse 编译器选项
/// </summary>
public sealed class VerseCompileOptions
{
    /// <summary>
    /// 是否生成 IR 文本
    /// </summary>
    public bool EmitIrText { get; init; }

    /// <summary>
    /// 是否生成字节码
    /// </summary>
    public bool EmitBytecode { get; init; } = true;

    /// <summary>
    /// 是否跳过语义分析
    /// </summary>
    public bool SkipSemanticAnalysis { get; init; }
}

/// <summary>
/// Verse 编译器，编排 Verse 源码 → AST → 语义分析 → IR → 字节码的完整编译管线
/// </summary>
public sealed class VerseCompiler
{
    #region 字段

    private readonly DiagnosticSink _diagnostics = new();

    #endregion

    #region 公共方法

    /// <summary>
    /// 编译 Verse 源码
    /// </summary>
    /// <param name="source">Verse 源码文本</param>
    /// <param name="filePath">源文件路径（用于诊断信息）</param>
    /// <param name="options">编译选项</param>
    /// <returns>编译结果</returns>
    public VerseCompilationResult Compile(string source, string filePath = "", VerseCompileOptions? options = null)
    {
        options ??= new VerseCompileOptions();
        _diagnostics.Clear();

        var ast = ParseToAst(source, filePath);
        if (_diagnostics.HasErrors)
        {
            return CreateFailureResult();
        }

        if (!options.SkipSemanticAnalysis)
        {
            ast = AnalyzeSemantics(ast);
            if (_diagnostics.HasErrors)
            {
                return CreateFailureResult();
            }
        }

        var irModule = GenerateIr(ast);
        if (_diagnostics.HasErrors)
        {
            return CreateFailureResult();
        }

        BytecodeUnit? bytecodeUnit = null;
        if (options.EmitBytecode)
        {
            bytecodeUnit = GenerateBytecode(irModule);
        }

        return new VerseCompilationResult
        {
            Success = true,
            IrModule = irModule,
            BytecodeUnit = bytecodeUnit,
            Diagnostics = _diagnostics.Messages.ToList(),
            IrText = options.EmitIrText ? irModule.ToIrText() : null
        };
    }

    /// <summary>
    /// 仅执行词法分析和语法分析
    /// </summary>
    public CompilationUnit ParseToAst(string source, string filePath = "")
    {
        var lexer = new VerseLexer(_diagnostics);
        var tokens = lexer.Tokenize(source);

        var parser = new VerseParser(_diagnostics);
        return parser.Parse(source);
    }

    /// <summary>
    /// 仅执行语义分析
    /// </summary>
    public CompilationUnit AnalyzeSemantics(CompilationUnit ast)
    {
        var analyzer = new VerseSemanticAnalyzer(_diagnostics);
        return analyzer.Analyze(ast);
    }

    /// <summary>
    /// 仅执行 IR 生成
    /// </summary>
    public IrModule GenerateIr(CompilationUnit ast)
    {
        var generator = new VerseIrGenerator(_diagnostics);
        return generator.Generate(ast);
    }

    /// <summary>
    /// 仅执行字节码生成
    /// </summary>
    public BytecodeUnit GenerateBytecode(IrModule irModule)
    {
        var builder = new GnosisBytecodeBuilder();
        return builder.Compile(irModule, new GnosisCompileOptions());
    }

    #endregion

    #region 私有方法

    private VerseCompilationResult CreateFailureResult()
    {
        return new VerseCompilationResult
        {
            Success = false,
            Diagnostics = _diagnostics.Messages.ToList()
        };
    }

    #endregion
}

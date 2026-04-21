using Gnosis.Compiler;
using Gnosis.Compiler.AST;
using Gnosis.Compiler.Diagnostics;
using Gnosis.Compiler.Frontend;
using Gnosis.Rendering.Shader;
using Gnosis.Rendering.ShaderCompiler.Backend;
using Gnosis.Rendering.ShaderCompiler.Backend.ShaderIR;
using Gnosis.Rendering.ShaderCompiler.Backend.Spirv;

namespace Gnosis.Rendering.ShaderCompiler.Frontend;

public class GgShaderLexer
{
    #region Fields

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "micro", "struct", "let", "import", "using", "return", "if", "else",
        "for", "while", "true", "false", "null", "new", "discard"
    };

    private static readonly HashSet<string> TypeKeywords = new(StringComparer.Ordinal)
    {
        "void", "bool", "i32", "u32", "f32", "f64",
        "vec2", "vec3", "vec4", "mat2", "mat3", "mat4",
        "texture_2d", "sampler", "image_2d"
    };

    private static readonly HashSet<string> AttributeNames = new(StringComparer.Ordinal)
    {
        "Vertex", "Fragment", "Compute", "WorkgroupSize",
        "Group", "Binding", "Builtin", "Location", "PushConstant",
        "SpecializationConstant", "InputAttachment"
    };

    private string _source = string.Empty;
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private DiagnosticSink? _diagnostics;

    #endregion

    #region Constructors

    public GgShaderLexer(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    #endregion

    #region Public Methods

    public IReadOnlyList<Token> Tokenize(string source)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;

        var tokens = new List<Token>();

        while (!IsAtEnd())
        {
            SkipWhitespaceAndComments();

            if (IsAtEnd())
            {
                break;
            }

            var token = ScanToken();

            if (token is not null)
            {
                tokens.Add(token);
            }
        }

        tokens.Add(new Token(TokenType.Eof, string.Empty, _line, _column));
        return tokens;
    }

    #endregion

    #region Private Methods

    private bool IsAtEnd() => _position >= _source.Length;

    private char Peek() => IsAtEnd() ? '\0' : _source[_position];

    private char PeekNext() => _position + 1 >= _source.Length ? '\0' : _source[_position + 1];

    private char Advance()
    {
        var c = _source[_position++];
        if (c == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }
        return c;
    }

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            var c = Peek();
            if (char.IsWhiteSpace(c))
            {
                Advance();
            }
            else if (c == '/' && PeekNext() == '/')
            {
                while (!IsAtEnd() && Peek() != '\n')
                {
                    Advance();
                }
            }
            else if (c == '/' && PeekNext() == '*')
            {
                Advance();
                Advance();
                while (!IsAtEnd())
                {
                    if (Peek() == '*' && PeekNext() == '/')
                    {
                        Advance();
                        Advance();
                        break;
                    }
                    Advance();
                }
            }
            else
            {
                break;
            }
        }
    }

    private Token? ScanToken()
    {
        var startLine = _line;
        var startColumn = _column;

        if (Peek() == '<' && PeekNext() == '%')
        {
            return ScanMetaBlock(startLine, startColumn);
        }

        var c = Peek();

        if (c is '"' or '\'')
        {
            return ScanString(startLine, startColumn);
        }

        if (char.IsDigit(c))
        {
            return ScanNumber(startLine, startColumn);
        }

        if (c == '_' || char.IsLetter(c))
        {
            return ScanIdentifierOrKeyword(startLine, startColumn);
        }

        if (c == '[')
        {
            return ScanAttribute(startLine, startColumn);
        }

        if (c == '<' && (PeekNext() == '<' || char.IsLetter(PeekNext())))
        {
            return ScanOperatorOrGeneric(startLine, startColumn);
        }

        if (IsOperatorChar(c))
        {
            return ScanOperator(startLine, startColumn);
        }

        if (IsDelimiter(c))
        {
            Advance();
            return new Token(TokenType.Delimiter, c.ToString(), startLine, startColumn);
        }

        Advance();
        _diagnostics?.AddError(
            string.Empty,
            new SourceSpan(string.Empty, startLine, startColumn, startLine, startColumn + 1),
            "GG3001",
            $"着色器中意外的字符 '{c}'");

        return null;
    }

    private Token ScanMetaBlock(int startLine, int startColumn)
    {
        Advance();
        Advance();

        if (Peek() == '=')
        {
            Advance();
            var content = ScanMetaContent();
            return new Token(TokenType.MetaExpression, content, startLine, startColumn);
        }

        var blockContent = ScanMetaContent();
        return new Token(TokenType.MetaBlockStart, blockContent, startLine, startColumn);
    }

    private string ScanMetaContent()
    {
        var sb = new System.Text.StringBuilder();
        var depth = 1;

        while (!IsAtEnd() && depth > 0)
        {
            if (Peek() == '<' && PeekNext() == '%')
            {
                Advance();
                Advance();
                depth++;
                sb.Append("<%");
            }
            else if (Peek() == '%' && PeekNext() == '>')
            {
                Advance();
                Advance();
                depth--;
                if (depth > 0)
                {
                    sb.Append("%>");
                }
            }
            else
            {
                sb.Append(Advance());
            }
        }

        return sb.ToString().Trim();
    }

    private Token ScanString(int startLine, int startColumn)
    {
        var quote = Advance();
        var sb = new System.Text.StringBuilder();

        while (!IsAtEnd() && Peek() != quote)
        {
            if (Peek() == '\\')
            {
                Advance();
                if (!IsAtEnd())
                {
                    sb.Append(Advance());
                }
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd())
        {
            Advance();
        }

        return new Token(TokenType.String, sb.ToString(), startLine, startColumn);
    }

    private Token ScanNumber(int startLine, int startColumn)
    {
        var sb = new System.Text.StringBuilder();

        while (!IsAtEnd() && char.IsDigit(Peek()))
        {
            sb.Append(Advance());
        }

        if (!IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext()))
        {
            sb.Append(Advance());
            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            sb.Append(Advance());
            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-'))
            {
                sb.Append(Advance());
            }
            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                sb.Append(Advance());
            }
        }

        if (!IsAtEnd() && (Peek() == 'f' || Peek() == 'i' || Peek() == 'u'))
        {
            sb.Append(Advance());
        }

        return new Token(TokenType.Number, sb.ToString(), startLine, startColumn);
    }

    private Token ScanIdentifierOrKeyword(int startLine, int startColumn)
    {
        var sb = new System.Text.StringBuilder();

        while (!IsAtEnd() && (Peek() == '_' || char.IsLetterOrDigit(Peek())))
        {
            sb.Append(Advance());
        }

        var text = sb.ToString();

        if (text is "true" or "false" or "null")
        {
            return new Token(TokenType.Literal, text, startLine, startColumn);
        }

        if (Keywords.Contains(text))
        {
            return new Token(TokenType.Keyword, text, startLine, startColumn);
        }

        if (TypeKeywords.Contains(text))
        {
            return new Token(TokenType.TypeKeyword, text, startLine, startColumn);
        }

        return new Token(TokenType.Identifier, text, startLine, startColumn);
    }

    private Token ScanAttribute(int startLine, int startColumn)
    {
        Advance();
        var sb = new System.Text.StringBuilder("[");

        while (!IsAtEnd() && Peek() != ']')
        {
            sb.Append(Advance());
        }

        if (!IsAtEnd())
        {
            sb.Append(Advance());
        }

        return new Token(TokenType.Attribute, sb.ToString(), startLine, startColumn);
    }

    private Token ScanOperatorOrGeneric(int startLine, int startColumn)
    {
        Advance();

        if (Peek() == '<')
        {
            Advance();
            return new Token(TokenType.Operator, "<<", startLine, startColumn);
        }

        return new Token(TokenType.Operator, "<", startLine, startColumn);
    }

    private Token ScanOperator(int startLine, int startColumn)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(Advance());

        if (!IsAtEnd())
        {
            var candidate = sb.ToString() + Peek();
            if (candidate is "==" or "!=" or "<=" or ">=" or ">>" or "+=" or "-=" or "*=" or "/=" or "&&" or "||" or "->" or "=>")
            {
                sb.Append(Advance());
            }
        }

        return new Token(TokenType.Operator, sb.ToString(), startLine, startColumn);
    }

    private static bool IsOperatorChar(char c)
    {
        return c is '+' or '-' or '*' or '/' or '%' or '=' or '!' or '<' or '>' or '&' or '|' or '^' or '~' or '?' or ':';
    }

    private static bool IsDelimiter(char c)
    {
        return c is '(' or ')' or '{' or '}' or '[' or ']' or ',' or ';' or '.';
    }

    #endregion
}

public class GgShaderCompiler : IShaderCompiler
{
    #region Fields

    private readonly DiagnosticSink _diagnostics;

    #endregion

    #region Constructors

    public GgShaderCompiler(DiagnosticSink? diagnostics = null)
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
        var lexer = new GgShaderLexer(_diagnostics);
        var tokens = lexer.Tokenize(source);

        var parser = new GgShaderParser(_diagnostics);
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

        var errors = _diagnostics.GetErrors().ToList();
        if (errors.Count > 0)
        {
            throw new ShaderCompilationException(errors);
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
        var lexer = new GgShaderLexer(_diagnostics);
        var tokens = lexer.Tokenize(sourceCode);
        var parser = new GgShaderParser(_diagnostics);
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
            var lexer = new GgShaderLexer(_diagnostics);
            var tokens = lexer.Tokenize(sourceCode);

            var parser = new GgShaderParser(_diagnostics);
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

public class ShaderCompilationException : Exception
{
    #region Properties

    public IReadOnlyList<Diagnostic> Errors { get; }

    #endregion

    #region Constructors

    public ShaderCompilationException(IReadOnlyList<Diagnostic> errors)
        : base($"着色器编译失败，共 {errors.Count} 个错误")
    {
        Errors = errors;
    }

    #endregion
}

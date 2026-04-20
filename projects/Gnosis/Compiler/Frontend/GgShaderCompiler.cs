using Gnosis.Compiler.Diagnostics;
using Gnosis.Compiler.ValueObjects;
using Gnosis.Compiler.ValueObjects.AST;

namespace Gnosis.Compiler.Frontend;

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

        if (c == '"' || c == '\'')
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

        if (text == "true" || text == "false" || text == "null")
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

public class GgShaderCompiler
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

    #region Public Methods

    public byte[] CompileToSpirV(string source)
    {
        var lexer = new GgShaderLexer(_diagnostics);
        var tokens = lexer.Tokenize(source);

        var parser = new GgScriptParser(_diagnostics);
        var ast = parser.Parse(tokens);

        var evaluator = new MetaLanguageEvaluator(new MacroTable());
        ast = evaluator.Evaluate(ast, new ChannelMacros());

        return GenerateSpirV(ast);
    }

    #endregion

    #region Private Methods

    private byte[] GenerateSpirV(AstNode ast)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(0x07230203);
        writer.Write(0x00010000);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u);

        if (ast is CompilationUnit unit)
        {
            foreach (var decl in unit.Declarations)
            {
                GenerateDeclarationSpirV(writer, decl);
            }
        }

        var totalSize = (uint)(ms.Length / 4);
        ms.Position = 8;
        writer.Write(totalSize);

        return ms.ToArray();
    }

    private void GenerateDeclarationSpirV(BinaryWriter writer, AstNode decl)
    {
        switch (decl.Type)
        {
            case NodeType.FunctionDecl:
                GenerateFunctionSpirV(writer, (FunctionDecl)decl);
                break;
            case NodeType.ComponentDecl:
                GenerateStructSpirV(writer, (ComponentDecl)decl);
                break;
        }
    }

    private void GenerateFunctionSpirV(BinaryWriter writer, FunctionDecl decl)
    {
        writer.Write(0x00000015);
        writer.Write(0x00000000);
    }

    private void GenerateStructSpirV(BinaryWriter writer, ComponentDecl decl)
    {
        writer.Write(0x0000001B);
        writer.Write(0x00000000);
    }

    #endregion
}

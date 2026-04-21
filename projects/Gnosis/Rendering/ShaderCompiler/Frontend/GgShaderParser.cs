using Gnosis.Compiler;
using Gnosis.Compiler.AST;
using Gnosis.Compiler.Diagnostics;

namespace Gnosis.Rendering.ShaderCompiler.Frontend;

public class GgShaderParser : IParser
{
    #region Fields

    private IReadOnlyList<Token> _tokens = Array.Empty<Token>();
    private int _current;
    private DiagnosticSink? _diagnostics;
    private string _filePath = string.Empty;

    #endregion

    #region Constructors

    public GgShaderParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    #endregion

    #region Public Methods

    public AstNode Parse(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens;
        _current = 0;
        _diagnostics ??= new DiagnosticSink();

        var declarations = new List<AstNode>();

        while (!IsAtEnd())
        {
            var decl = ParseDeclaration();

            if (decl is not null)
            {
                declarations.Add(decl);
            }
        }

        return new CompilationUnit(null, declarations, _filePath);
    }

    #endregion

    #region Private Methods - Token Access

    private bool IsAtEnd()
    {
        return Peek().TokenType == TokenType.Eof;
    }

    private Token Peek()
    {
        return _current < _tokens.Count ? _tokens[_current] : _tokens[^1];
    }

    private Token PeekAt(int offset)
    {
        var index = _current + offset;
        return index < _tokens.Count ? _tokens[index] : _tokens[^1];
    }

    private Token Previous()
    {
        return _tokens[_current - 1];
    }

    private Token Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }

        return Previous();
    }

    private bool Check(TokenType type)
    {
        return !IsAtEnd() && Peek().TokenType == type;
    }

    private bool Check(TokenType type, string value)
    {
        return !IsAtEnd() && Peek().TokenType == type && Peek().Value == value;
    }

    private bool Match(TokenType type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool Match(TokenType type, string value)
    {
        if (Check(type, value))
        {
            Advance();
            return true;
        }

        return false;
    }

    private Token Consume(TokenType type, string errorCode, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        var token = Peek();
        _diagnostics?.AddError(
            _filePath,
            SourceSpan.FromToken(token),
            errorCode,
            message);

        throw new ParseException(message);
    }

    private Token ConsumeKeyword(string keyword, string errorCode, string message)
    {
        if (Check(TokenType.Keyword, keyword))
        {
            return Advance();
        }

        var token = Peek();
        _diagnostics?.AddError(
            _filePath,
            SourceSpan.FromToken(token),
            errorCode,
            message);

        throw new ParseException(message);
    }

    private Token Consume(TokenType type, string value, string errorCode, string message)
    {
        if (Check(type, value))
        {
            return Advance();
        }

        var token = Peek();
        _diagnostics?.AddError(
            _filePath,
            SourceSpan.FromToken(token),
            errorCode,
            message);

        throw new ParseException(message);
    }

    private bool CheckDot()
    {
        return Check(TokenType.Punctuation, ".") || Check(TokenType.Delimiter, ".");
    }

    private bool MatchDot()
    {
        if (Check(TokenType.Punctuation, "."))
        {
            Advance();
            return true;
        }

        if (Check(TokenType.Delimiter, "."))
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool CheckColon()
    {
        return Check(TokenType.Punctuation, ":") || Check(TokenType.Operator, ":");
    }

    private bool MatchColon()
    {
        if (Check(TokenType.Punctuation, ":"))
        {
            Advance();
            return true;
        }

        if (Check(TokenType.Operator, ":"))
        {
            Advance();
            return true;
        }

        return false;
    }

    private Token ConsumeColon(string errorCode, string message)
    {
        if (Check(TokenType.Punctuation, ":"))
        {
            return Advance();
        }

        if (Check(TokenType.Operator, ":"))
        {
            return Advance();
        }

        var token = Peek();
        _diagnostics?.AddError(
            _filePath,
            SourceSpan.FromToken(token),
            errorCode,
            message);

        throw new ParseException(message);
    }

    private bool CheckDoubleColon()
    {
        if (!Check(TokenType.Operator, "::") && !Check(TokenType.Punctuation, "::"))
        {
            return false;
        }

        return true;
    }

    private bool MatchDoubleColon()
    {
        if (Check(TokenType.Operator, "::"))
        {
            Advance();
            return true;
        }

        if (Check(TokenType.Punctuation, "::"))
        {
            Advance();
            return true;
        }

        return false;
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().TokenType == TokenType.Delimiter && Previous().Value == ";")
            {
                return;
            }

            if (Peek().TokenType == TokenType.Keyword)
            {
                switch (Peek().Value)
                {
                    case "micro":
                    case "struct":
                    case "let":
                    case "import":
                    case "using":
                        return;
                }
            }

            Advance();
        }
    }

    #endregion

    #region Private Methods - Declarations

    private AstNode? ParseDeclaration()
    {
        try
        {
            if (Check(TokenType.Keyword, "micro"))
            {
                return ParseFunctionDecl();
            }

            if (Check(TokenType.Keyword, "struct"))
            {
                return ParseStructDecl();
            }

            if (Check(TokenType.Keyword, "import"))
            {
                return ParseImportDecl();
            }

            if (Check(TokenType.Keyword, "using"))
            {
                return ParseUsingDecl();
            }

            if (Check(TokenType.Identifier, "cbuffer"))
            {
                return ParseCbufferDecl();
            }

            if (Check(TokenType.Keyword, "let"))
            {
                if (IsUniformBindingLet())
                {
                    return ParseUniformBindingDecl();
                }

                return ParseVariableDecl();
            }

            if (Check(TokenType.Attribute))
            {
                var attrs = ParseAttributes();

                if (Check(TokenType.Keyword, "micro"))
                {
                    return ParseFunctionDecl(attrs);
                }

                if (Check(TokenType.Keyword, "struct"))
                {
                    return ParseStructDecl(attrs);
                }

                if (Check(TokenType.Identifier, "cbuffer"))
                {
                    return ParseCbufferDecl(attrs);
                }

                if (Check(TokenType.Keyword, "let"))
                {
                    if (IsUniformBindingLet())
                    {
                        return ParseUniformBindingDecl(attrs);
                    }

                    return ParseVariableDecl();
                }

                _diagnostics?.AddError(
                    _filePath,
                    SourceSpan.FromToken(Peek()),
                    "GG3001",
                    $"属性标注后应为声明，但遇到 '{Peek().Value}'");

                return null;
            }

            return ParseStatement();
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private ImportDecl ParseImportDecl()
    {
        var startToken = ConsumeKeyword("import", "GG3002", "期望 'import' 关键字");

        var modulePath = Consume(TokenType.Identifier, "GG3003", "期望模块路径").Value;

        while (MatchDot())
        {
            modulePath += "." + Consume(TokenType.Identifier, "GG3004", "期望标识符").Value;
        }

        string? alias = null;
        if (Match(TokenType.Keyword, "as"))
        {
            alias = Consume(TokenType.Identifier, "GG3005", "期望别名标识符").Value;
        }

        Match(TokenType.Delimiter, ";");

        return new ImportDecl(SourceSpan.FromToken(startToken), modulePath, alias);
    }

    private UsingDecl ParseUsingDecl()
    {
        var startToken = ConsumeKeyword("using", "GG3006", "期望 'using' 关键字");

        var namespacePath = Consume(TokenType.Identifier, "GG3007", "期望命名空间路径").Value;

        while (MatchDoubleColon() || MatchDot())
        {
            namespacePath += "::" + Consume(TokenType.Identifier, "GG3008", "期望标识符").Value;
        }

        var selections = new List<string>();
        if (Match(TokenType.Operator, "=>"))
        {
            selections.Add(Consume(TokenType.Identifier, "GG3009", "期望选择标识符").Value);

            while (Match(TokenType.Delimiter, ","))
            {
                selections.Add(Consume(TokenType.Identifier, "GG3010", "期望选择标识符").Value);
            }
        }

        Match(TokenType.Delimiter, ";");

        return new UsingDecl(SourceSpan.FromToken(startToken), namespacePath, selections);
    }

    private VariableDecl ParseVariableDecl()
    {
        var startToken = ConsumeKeyword("let", "GG3011", "期望 'let' 关键字");

        var isMutable = Match(TokenType.Keyword, "mut");

        var name = Consume(TokenType.Identifier, "GG3012", "期望变量名").Value;

        TypeAnnotation? varType = null;
        if (MatchColon())
        {
            varType = ParseTypeAnnotation();
        }

        AstNode? initializer = null;
        if (Match(TokenType.Operator, "="))
        {
            initializer = ParseExpression();
        }

        Match(TokenType.Delimiter, ";");

        return new VariableDecl(SourceSpan.FromToken(startToken), name, varType, initializer, isMutable);
    }

    private bool IsUniformBindingLet()
    {
        var t1 = PeekAt(1);
        var t2 = PeekAt(2);
        var t3 = PeekAt(3);

        if (t1.TokenType != TokenType.Operator || t1.Value != "<")
        {
            return false;
        }

        if (t2.TokenType != TokenType.Identifier || (t2.Value != "uniform" && t2.Value != "storage"))
        {
            return false;
        }

        if (t3.TokenType != TokenType.Operator || t3.Value != ">")
        {
            return false;
        }

        return true;
    }

    private UniformBindingDecl ParseUniformBindingDecl(IReadOnlyList<AttributeDecl>? attrs = null)
    {
        var startToken = ConsumeKeyword("let", "GG3070", "期望 'let' 关键字");

        Consume(TokenType.Operator, "<", "GG3071", "期望 '<'");

        var bindingType = Consume(TokenType.Identifier, "GG3072", "期望 'uniform' 或 'storage'").Value;

        Consume(TokenType.Operator, ">", "GG3073", "期望 '>'");

        var name = Consume(TokenType.Identifier, "GG3074", "期望绑定名").Value;

        ConsumeColon("GG3075", "期望 ':'");

        var typeAnnotation = ParseTypeAnnotation();

        Match(TokenType.Delimiter, ";");

        int? group = null;
        int? binding = null;

        if (attrs is not null)
        {
            foreach (var attr in attrs)
            {
                if (attr.Name == "Group" && attr.Arguments.Count > 0 && int.TryParse(attr.Arguments[0].Value, out var g))
                {
                    group = g;
                }
                else if (attr.Name == "Binding" && attr.Arguments.Count > 0 && int.TryParse(attr.Arguments[0].Value, out var b))
                {
                    binding = b;
                }
            }
        }

        return new UniformBindingDecl(SourceSpan.FromToken(startToken), name, bindingType, typeAnnotation, group, binding, attrs ?? Array.Empty<AttributeDecl>());
    }

    private StructDecl ParseStructDecl(IReadOnlyList<AttributeDecl>? attrs = null)
    {
        var startToken = ConsumeKeyword("struct", "GG3013", "期望 'struct' 关键字");

        var name = Consume(TokenType.Identifier, "GG3014", "期望结构体名").Value;

        Consume(TokenType.Delimiter, "{", "GG3015", "期望 '{'");

        var fields = new List<FieldDecl>();

        while (!Check(TokenType.Delimiter, "}") && !IsAtEnd())
        {
            var fieldAttrs = ParseAttributes();
            var field = ParseFieldDecl(fieldAttrs);
            fields.Add(field);
        }

        Consume(TokenType.Delimiter, "}", "GG3016", "期望 '}'");

        var allAttrs = new List<AttributeDecl>();
        allAttrs.Add(new AttributeDecl(SourceSpan.FromToken(startToken), "Struct", Array.Empty<KeyValuePair<string, string>>()));
        if (attrs is not null)
        {
            allAttrs.AddRange(attrs);
        }

        return new StructDecl(SourceSpan.FromToken(startToken), name, fields, allAttrs);
    }

    private FieldDecl ParseFieldDecl(IReadOnlyList<AttributeDecl> attrs)
    {
        var startToken = Peek();

        var name = Consume(TokenType.Identifier, "GG3017", "期望字段名").Value;

        ConsumeColon("GG3018", "期望 ':'");

        var fieldType = ParseTypeAnnotation();

        AstNode? defaultValue = null;
        if (Match(TokenType.Operator, "="))
        {
            defaultValue = ParseExpression();
        }

        Match(TokenType.Delimiter, ";");

        return new FieldDecl(SourceSpan.FromToken(startToken), name, fieldType, defaultValue, attrs);
    }

    private ComponentDecl ParseCbufferDecl(IReadOnlyList<AttributeDecl>? attrs = null)
    {
        var startToken = Peek();
        Advance();

        var name = Consume(TokenType.Identifier, "GG3019", "期望缓冲区名").Value;

        Consume(TokenType.Delimiter, "{", "GG3020", "期望 '{'");

        var fields = new List<FieldDecl>();

        while (!Check(TokenType.Delimiter, "}") && !IsAtEnd())
        {
            var fieldAttrs = ParseAttributes();
            var field = ParseFieldDecl(fieldAttrs);
            fields.Add(field);
        }

        Consume(TokenType.Delimiter, "}", "GG3021", "期望 '}'");

        var allAttrs = new List<AttributeDecl>();
        allAttrs.Add(new AttributeDecl(SourceSpan.FromToken(startToken), "Binding", Array.Empty<KeyValuePair<string, string>>()));
        if (attrs is not null)
        {
            allAttrs.AddRange(attrs);
        }

        return new ComponentDecl(SourceSpan.FromToken(startToken), name, allAttrs, fields);
    }

    private FunctionDecl ParseFunctionDecl(IReadOnlyList<AttributeDecl>? attrs = null)
    {
        var startToken = ConsumeKeyword("micro", "GG3027", "期望 'micro' 关键字");

        var name = Consume(TokenType.Identifier, "GG3028", "期望函数名").Value;

        Consume(TokenType.Delimiter, "(", "GG3029", "期望 '('");

        var parameters = new List<ParameterDecl>();

        if (!Check(TokenType.Delimiter, ")"))
        {
            parameters.Add(ParseParameterDecl());

            while (Match(TokenType.Delimiter, ","))
            {
                parameters.Add(ParseParameterDecl());
            }
        }

        Consume(TokenType.Delimiter, ")", "GG3030", "期望 ')'");

        TypeAnnotation? returnType = null;
        if (MatchColon())
        {
            returnType = ParseTypeAnnotation();
        }

        BlockStmt? body = null;
        if (Check(TokenType.Delimiter, "{"))
        {
            body = ParseBlockStmt();
        }
        else
        {
            Match(TokenType.Delimiter, ";");
        }

        return new FunctionDecl(SourceSpan.FromToken(startToken), name, parameters, returnType, body, attrs ?? Array.Empty<AttributeDecl>());
    }

    private ParameterDecl ParseParameterDecl()
    {
        var startToken = Peek();

        var name = Consume(TokenType.Identifier, "GG3031", "期望参数名").Value;

        ConsumeColon("GG3032", "期望 ':'");

        var paramType = ParseTypeAnnotation();

        return new ParameterDecl(SourceSpan.FromToken(startToken), name, paramType, Array.Empty<AttributeDecl>());
    }

    #endregion

    #region Private Methods - Attributes

    private IReadOnlyList<AttributeDecl> ParseAttributes()
    {
        var attrs = new List<AttributeDecl>();

        while (Check(TokenType.Attribute))
        {
            attrs.Add(ParseAttribute());
        }

        return attrs;
    }

    private AttributeDecl ParseAttribute()
    {
        var startToken = Advance();

        var content = startToken.Value;

        if (content.StartsWith("[") && content.EndsWith("]"))
        {
            content = content[1..^1].Trim();
        }

        var parenIdx = content.IndexOf('(');
        string attrName;
        var args = new List<KeyValuePair<string, string>>();

        if (parenIdx >= 0)
        {
            attrName = content[..parenIdx].Trim();
            var argsContent = content[(parenIdx + 1)..^1].Trim();

            if (!string.IsNullOrEmpty(argsContent))
            {
                foreach (var arg in argsContent.Split(','))
                {
                    var eqIdx = arg.IndexOf('=');
                    if (eqIdx >= 0)
                    {
                        var key = arg[..eqIdx].Trim();
                        var value = arg[(eqIdx + 1)..].Trim().Trim('"');
                        args.Add(new KeyValuePair<string, string>(key, value));
                    }
                    else
                    {
                        args.Add(new KeyValuePair<string, string>(arg.Trim(), "true"));
                    }
                }
            }
        }
        else
        {
            attrName = content.Trim();
        }

        return new AttributeDecl(SourceSpan.FromToken(startToken), attrName, args);
    }

    #endregion

    #region Private Methods - Types

    private TypeAnnotation ParseTypeAnnotation()
    {
        var startToken = Peek();

        var name = ParseTypeName();

        var genericArgs = new List<TypeAnnotation>();

        if (Match(TokenType.Operator, "<"))
        {
            genericArgs.Add(ParseTypeAnnotation());

            while (Match(TokenType.Delimiter, ","))
            {
                genericArgs.Add(ParseTypeAnnotation());
            }

            Consume(TokenType.Operator, ">", "GG3040", "期望 '>'");
        }

        return new TypeAnnotation(SourceSpan.FromToken(startToken), name, genericArgs);
    }

    private string ParseTypeName()
    {
        if (Check(TokenType.TypeKeyword))
        {
            return Advance().Value;
        }

        return Consume(TokenType.Identifier, "GG3041", "期望类型名").Value;
    }

    #endregion

    #region Private Methods - Statements

    private AstNode ParseStatement()
    {
        if (Check(TokenType.Keyword, "if"))
        {
            return ParseIfStmt();
        }

        if (Check(TokenType.Keyword, "for"))
        {
            return ParseForStmt();
        }

        if (Check(TokenType.Keyword, "while"))
        {
            return ParseWhileStmt();
        }

        if (Check(TokenType.Keyword, "loop"))
        {
            return ParseLoopStmt();
        }

        if (Check(TokenType.Keyword, "return"))
        {
            return ParseReturnStmt();
        }

        if (Check(TokenType.Keyword, "discard"))
        {
            return ParseDiscardStmt();
        }

        if (Check(TokenType.Delimiter, "{"))
        {
            return ParseBlockStmt();
        }

        return ParseExprStmt();
    }

    private BlockStmt ParseBlockStmt()
    {
        var startToken = Consume(TokenType.Delimiter, "{", "GG3050", "期望 '{'");

        var statements = new List<AstNode>();

        while (!Check(TokenType.Delimiter, "}") && !IsAtEnd())
        {
            var stmt = ParseDeclaration();

            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        Consume(TokenType.Delimiter, "}", "GG3051", "期望 '}'");

        return new BlockStmt(SourceSpan.FromToken(startToken), statements);
    }

    private IfStmt ParseIfStmt()
    {
        var startToken = ConsumeKeyword("if", "GG3052", "期望 'if' 关键字");

        var condition = ParseExpression();

        var thenBlock = ParseBlockStmt();

        AstNode? elseBlock = null;
        if (Match(TokenType.Keyword, "else"))
        {
            if (Check(TokenType.Keyword, "if"))
            {
                elseBlock = ParseIfStmt();
            }
            else
            {
                elseBlock = ParseBlockStmt();
            }
        }

        return new IfStmt(SourceSpan.FromToken(startToken), condition, thenBlock, elseBlock);
    }

    private ForStmt ParseForStmt()
    {
        var startToken = ConsumeKeyword("for", "GG3053", "期望 'for' 关键字");

        Consume(TokenType.Delimiter, "(", "GG3054", "期望 '('");

        AstNode? initializer = null;
        if (!Check(TokenType.Delimiter, ";"))
        {
            if (Check(TokenType.Keyword, "let"))
            {
                initializer = ParseVariableDecl();
            }
            else
            {
                initializer = ParseExpression();
                Match(TokenType.Delimiter, ";");
            }
        }
        else
        {
            Match(TokenType.Delimiter, ";");
        }

        AstNode? condition = null;
        if (!Check(TokenType.Delimiter, ";"))
        {
            condition = ParseExpression();
        }

        Match(TokenType.Delimiter, ";");

        AstNode? update = null;
        if (!Check(TokenType.Delimiter, ")"))
        {
            update = ParseExpression();
        }

        Consume(TokenType.Delimiter, ")", "GG3055", "期望 ')'");

        var body = ParseBlockStmt();

        return new ForStmt(SourceSpan.FromToken(startToken), initializer, condition, update, body);
    }

    private WhileStmt ParseWhileStmt()
    {
        var startToken = ConsumeKeyword("while", "GG3056", "期望 'while' 关键字");

        var condition = ParseExpression();

        var body = ParseBlockStmt();

        return new WhileStmt(SourceSpan.FromToken(startToken), condition, body);
    }

    private LoopStmt ParseLoopStmt()
    {
        var startToken = ConsumeKeyword("loop", "GG3057", "期望 'loop' 关键字");

        string? iteratorName = null;
        AstNode? iterable = null;

        if (Check(TokenType.Identifier) && !Check(TokenType.Delimiter, "{"))
        {
            iteratorName = Advance().Value;

            if (Match(TokenType.Keyword, "in"))
            {
                iterable = ParseExpression();
            }
        }

        var body = ParseBlockStmt();

        return new LoopStmt(SourceSpan.FromToken(startToken), iteratorName, iterable, body);
    }

    private ReturnStmt ParseReturnStmt()
    {
        var startToken = ConsumeKeyword("return", "GG3058", "期望 'return' 关键字");

        AstNode? value = null;

        if (!Check(TokenType.Delimiter, ";") && !Check(TokenType.Delimiter, "}"))
        {
            value = ParseExpression();
        }

        Match(TokenType.Delimiter, ";");

        return new ReturnStmt(SourceSpan.FromToken(startToken), value);
    }

    private DiscardStmt ParseDiscardStmt()
    {
        var startToken = ConsumeKeyword("discard", "GG3059", "期望 'discard' 关键字");

        Match(TokenType.Delimiter, ";");

        return new DiscardStmt(SourceSpan.FromToken(startToken));
    }

    private ExprStmt ParseExprStmt()
    {
        var expr = ParseExpression();
        Match(TokenType.Delimiter, ";");
        return new ExprStmt(null, expr);
    }

    #endregion

    #region Private Methods - Expressions

    private AstNode ParseExpression()
    {
        return ParseAssignment();
    }

    private AstNode ParseAssignment()
    {
        var expr = ParseOr();

        if (Check(TokenType.Operator, "=") || Check(TokenType.Operator, "+=") ||
            Check(TokenType.Operator, "-=") || Check(TokenType.Operator, "*=") ||
            Check(TokenType.Operator, "/="))
        {
            var op = Advance().Value;
            var value = ParseAssignment();

            return new AssignmentExpr(null, expr, op, value);
        }

        return expr;
    }

    private AstNode ParseOr()
    {
        var left = ParseAnd();

        while (Match(TokenType.Operator, "||"))
        {
            var op = Previous().Value;
            var right = ParseAnd();
            left = new BinaryExpr(null, left, op, right);
        }

        return left;
    }

    private AstNode ParseAnd()
    {
        var left = ParseEquality();

        while (Match(TokenType.Operator, "&&"))
        {
            var op = Previous().Value;
            var right = ParseEquality();
            left = new BinaryExpr(null, left, op, right);
        }

        return left;
    }

    private AstNode ParseEquality()
    {
        var left = ParseComparison();

        while (Check(TokenType.Operator, "==") || Check(TokenType.Operator, "!="))
        {
            var op = Advance().Value;
            var right = ParseComparison();
            left = new BinaryExpr(null, left, op, right);
        }

        return left;
    }

    private AstNode ParseComparison()
    {
        var left = ParseAddition();

        while (Check(TokenType.Operator, "<") || Check(TokenType.Operator, ">") ||
               Check(TokenType.Operator, "<=") || Check(TokenType.Operator, ">="))
        {
            var op = Advance().Value;
            var right = ParseAddition();
            left = new BinaryExpr(null, left, op, right);
        }

        return left;
    }

    private AstNode ParseAddition()
    {
        var left = ParseMultiplication();

        while (Check(TokenType.Operator, "+") || Check(TokenType.Operator, "-"))
        {
            var op = Advance().Value;
            var right = ParseMultiplication();
            left = new BinaryExpr(null, left, op, right);
        }

        return left;
    }

    private AstNode ParseMultiplication()
    {
        var left = ParseUnary();

        while (Check(TokenType.Operator, "*") || Check(TokenType.Operator, "/") || Check(TokenType.Operator, "%"))
        {
            var op = Advance().Value;
            var right = ParseUnary();
            left = new BinaryExpr(null, left, op, right);
        }

        return left;
    }

    private AstNode ParseUnary()
    {
        if (Check(TokenType.Operator, "-") || Check(TokenType.Operator, "!") || Check(TokenType.Operator, "~"))
        {
            var op = Advance().Value;
            var operand = ParseUnary();
            return new UnaryExpr(null, op, operand, true);
        }

        return ParsePostfix();
    }

    private AstNode ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (MatchDot())
            {
                if (Check(TokenType.Identifier))
                {
                    var memberName = Advance().Value;

                    if (IsSwizzlePattern(memberName))
                    {
                        expr = new SwizzleExpr(null, expr, memberName);
                    }
                    else
                    {
                        expr = new MemberAccessExpr(null, expr, memberName);
                    }
                }
                else
                {
                    var memberName = Consume(TokenType.Identifier, "GG3060", "期望成员名").Value;
                    expr = new MemberAccessExpr(null, expr, memberName);
                }
            }
            else if (Check(TokenType.Delimiter, "("))
            {
                var args = new List<AstNode>();
                Advance();

                if (!Check(TokenType.Delimiter, ")"))
                {
                    args.Add(ParseExpression());

                    while (Match(TokenType.Delimiter, ","))
                    {
                        args.Add(ParseExpression());
                    }
                }

                Consume(TokenType.Delimiter, ")", "GG3061", "期望 ')'");
                expr = new CallExpr(null, expr, args);
            }
            else if (Check(TokenType.Delimiter, "["))
            {
                Advance();
                var index = ParseExpression();
                Consume(TokenType.Delimiter, "]", "GG3062", "期望 ']'");
                expr = new IndexExpr(null, expr, index);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private static bool IsSwizzlePattern(string name)
    {
        if (name.Length < 1 || name.Length > 4)
        {
            return false;
        }

        foreach (var c in name)
        {
            if (c != 'x' && c != 'y' && c != 'z' && c != 'w' &&
                c != 'r' && c != 'g' && c != 'b' && c != 'a' &&
                c != 's' && c != 't' && c != 'p' && c != 'q')
            {
                return false;
            }
        }

        return true;
    }

    private AstNode ParsePrimary()
    {
        if (Check(TokenType.Number))
        {
            var token = Advance();
            return new LiteralExpr(SourceSpan.FromToken(token), LiteralType.Number, token.Value);
        }

        if (Check(TokenType.String))
        {
            var token = Advance();
            return new LiteralExpr(SourceSpan.FromToken(token), LiteralType.String, token.Value);
        }

        if (Check(TokenType.Literal))
        {
            var token = Advance();
            var kind = token.Value switch
            {
                "true" or "false" => LiteralType.Boolean,
                "null" => LiteralType.Null,
                _ => LiteralType.Null
            };
            return new LiteralExpr(SourceSpan.FromToken(token), kind, token.Value);
        }

        if (Check(TokenType.Identifier) || Check(TokenType.TypeKeyword))
        {
            var token = Advance();
            return new IdentifierExpr(SourceSpan.FromToken(token), token.Value);
        }

        if (Check(TokenType.Delimiter, "("))
        {
            Advance();
            var expr = ParseExpression();
            Consume(TokenType.Delimiter, ")", "GG3063", "期望 ')'");

            if (Check(TokenType.Operator, "=>"))
            {
                return ParseLambdaAfterParams(new[] { new ParameterDecl(null, "it", new TypeAnnotation(null, "auto", Array.Empty<TypeAnnotation>()), Array.Empty<AttributeDecl>()) });
            }

            return expr;
        }

        if (Check(TokenType.MetaBlockStart) || Check(TokenType.MetaExpression))
        {
            var token = Advance();
            return new MetaBlock(SourceSpan.FromToken(token), token.Value, token.TokenType == TokenType.MetaExpression);
        }

        var errorToken = Peek();
        _diagnostics?.AddError(
            _filePath,
            SourceSpan.FromToken(errorToken),
            "GG3064",
            $"意外的标记 '{errorToken.Value}'");

        throw new ParseException($"意外的标记 '{errorToken.Value}'");
    }

    private LambdaExpr ParseLambdaAfterParams(IReadOnlyList<ParameterDecl> parameters)
    {
        Consume(TokenType.Operator, "=>", "GG3065", "期望 '=>'");

        AstNode body;

        if (Check(TokenType.Delimiter, "{"))
        {
            body = ParseBlockStmt();
        }
        else
        {
            body = ParseExpression();
        }

        return new LambdaExpr(null, parameters, body);
    }

    #endregion

    #region Nested Types

    private sealed class ParseException : Exception
    {
        public ParseException(string message) : base(message) { }
    }

    #endregion
}

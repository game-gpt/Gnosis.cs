using Gnosis.Compiler.Diagnostics;
using Gnosis.Compiler.ValueObjects;
using Gnosis.Compiler.ValueObjects.AST;

namespace Gnosis.Compiler.Frontend;

public class GgScriptParser : IParser
{
    #region Fields

    private IReadOnlyList<Token> _tokens = Array.Empty<Token>();
    private int _current;
    private DiagnosticSink? _diagnostics;
    private string _filePath = string.Empty;

    #endregion

    #region Constructors

    public GgScriptParser(DiagnosticSink? diagnostics = null)
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
                    case "component":
                    case "system":
                    case "widget":
                    case "scene":
                    case "plugin":
                    case "micro":
                    case "let":
                    case "import":
                    case "export":
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
            if (Check(TokenType.Keyword, "component"))
            {
                return ParseComponentDecl();
            }

            if (Check(TokenType.Keyword, "system"))
            {
                return ParseSystemDecl();
            }

            if (Check(TokenType.Keyword, "widget"))
            {
                return ParseWidgetDecl();
            }

            if (Check(TokenType.Keyword, "scene"))
            {
                return ParseSceneDecl();
            }

            if (Check(TokenType.Keyword, "plugin"))
            {
                return ParsePluginDecl();
            }

            if (Check(TokenType.Keyword, "micro"))
            {
                return ParseFunctionDecl();
            }

            if (Check(TokenType.Keyword, "import"))
            {
                return ParseImportDecl();
            }

            if (Check(TokenType.Keyword, "let"))
            {
                return ParseVariableDecl();
            }

            if (Check(TokenType.Attribute))
            {
                var attrs = ParseAttributes();

                if (Check(TokenType.Keyword, "component"))
                {
                    return ParseComponentDecl(attrs);
                }

                if (Check(TokenType.Keyword, "system"))
                {
                    return ParseSystemDecl(attrs);
                }

                if (Check(TokenType.Keyword, "micro"))
                {
                    return ParseFunctionDecl(attrs);
                }

                _diagnostics?.AddError(
                    _filePath,
                    SourceSpan.FromToken(Peek()),
                    "GG0101",
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
        var startToken = ConsumeKeyword("import", "GG0102", "期望 'import' 关键字");

        var modulePath = Consume(TokenType.Identifier, "GG0103", "期望模块路径").Value;

        while (Match(TokenType.Punctuation, "."))
        {
            modulePath += "." + Consume(TokenType.Identifier, "GG0104", "期望标识符").Value;
        }

        string? alias = null;
        if (Match(TokenType.Keyword, "as"))
        {
            alias = Consume(TokenType.Identifier, "GG0105", "期望别名标识符").Value;
        }

        Match(TokenType.Delimiter, ";");

        return new ImportDecl(SourceSpan.FromToken(startToken), modulePath, alias);
    }

    private VariableDecl ParseVariableDecl()
    {
        var startToken = ConsumeKeyword("let", "GG0106", "期望 'let' 关键字");

        var isMutable = Match(TokenType.Keyword, "mut");

        var name = Consume(TokenType.Identifier, "GG0107", "期望变量名").Value;

        TypeAnnotation? varType = null;
        if (Match(TokenType.Punctuation, ":"))
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

    private ComponentDecl ParseComponentDecl(IReadOnlyList<AttributeDecl>? attrs = null)
    {
        var startToken = ConsumeKeyword("component", "GG0108", "期望 'component' 关键字");

        var name = Consume(TokenType.Identifier, "GG0109", "期望组件名").Value;

        Consume(TokenType.Delimiter, "{", "GG0110", "期望 '{'");

        var fields = new List<FieldDecl>();

        while (!Check(TokenType.Delimiter, "}") && !IsAtEnd())
        {
            var fieldAttrs = ParseAttributes();
            var field = ParseFieldDecl(fieldAttrs);
            fields.Add(field);
        }

        Consume(TokenType.Delimiter, "}", "GG0111", "期望 '}'");

        return new ComponentDecl(SourceSpan.FromToken(startToken), name, attrs ?? Array.Empty<AttributeDecl>(), fields);
    }

    private FieldDecl ParseFieldDecl(IReadOnlyList<AttributeDecl> attrs)
    {
        var startToken = Peek();

        var name = Consume(TokenType.Identifier, "GG0112", "期望字段名").Value;

        Consume(TokenType.Punctuation, ":", "GG0113", "期望 ':'");

        var fieldType = ParseTypeAnnotation();

        AstNode? defaultValue = null;
        if (Match(TokenType.Operator, "="))
        {
            defaultValue = ParseExpression();
        }

        Match(TokenType.Delimiter, ";");

        return new FieldDecl(SourceSpan.FromToken(startToken), name, fieldType, defaultValue, attrs);
    }

    private SystemDecl ParseSystemDecl(IReadOnlyList<AttributeDecl>? attrs = null)
    {
        var startToken = ConsumeKeyword("system", "GG0114", "期望 'system' 关键字");

        var name = Consume(TokenType.Identifier, "GG0115", "期望系统名").Value;

        Consume(TokenType.Delimiter, "{", "GG0116", "期望 '{'");

        var queries = new List<QueryExpr>();
        var methods = new List<FunctionDecl>();

        while (!Check(TokenType.Delimiter, "}") && !IsAtEnd())
        {
            if (Check(TokenType.Keyword, "query") || Check(TokenType.Identifier, "query"))
            {
                queries.Add(ParseQueryDecl());
            }
            else
            {
                var methodAttrs = ParseAttributes();
                methods.Add(ParseLifecycleMethod(methodAttrs));
            }
        }

        Consume(TokenType.Delimiter, "}", "GG0117", "期望 '}'");

        return new SystemDecl(SourceSpan.FromToken(startToken), name, attrs ?? Array.Empty<AttributeDecl>(), queries, methods);
    }

    private QueryExpr ParseQueryDecl()
    {
        var startToken = Advance();

        var name = Consume(TokenType.Identifier, "GG0118", "期望查询名").Value;

        Consume(TokenType.Operator, "=", "GG0119", "期望 '='");

        var query = ParseQueryExpr();

        Match(TokenType.Delimiter, ";");

        return query;
    }

    private QueryExpr ParseQueryExpr()
    {
        var startToken = Peek();

        Consume(TokenType.Identifier, "GG0120", "期望 'Query'");
        Consume(TokenType.Punctuation, ".", "GG0121", "期望 '.'");

        var kindStr = Consume(TokenType.Identifier, "GG0122", "期望 'all'、'any' 或 'none'").Value;
        var kind = kindStr switch
        {
            "all" => QueryKind.All,
            "any" => QueryKind.Any,
            "none" => QueryKind.None,
            _ => QueryKind.All
        };

        Consume(TokenType.Delimiter, "(", "GG0123", "期望 '('");

        var componentTypes = new List<TypeAnnotation>();

        if (!Check(TokenType.Delimiter, ")"))
        {
            componentTypes.Add(ParseTypeAnnotation());

            while (Match(TokenType.Delimiter, ","))
            {
                componentTypes.Add(ParseTypeAnnotation());
            }
        }

        Consume(TokenType.Delimiter, ")", "GG0124", "期望 ')'");

        IReadOnlyList<QueryExpr>? filters = null;

        if (Match(TokenType.Punctuation, "."))
        {
            var filterList = new List<QueryExpr>();
            filterList.Add(ParseQueryExpr());

            while (Match(TokenType.Punctuation, "."))
            {
                filterList.Add(ParseQueryExpr());
            }

            filters = filterList;
        }

        return new QueryExpr(SourceSpan.FromToken(startToken), kind, componentTypes, filters);
    }

    private FunctionDecl ParseLifecycleMethod(IReadOnlyList<AttributeDecl> attrs)
    {
        var startToken = Peek();

        var name = Consume(TokenType.Identifier, "GG0125", "期望生命周期方法名").Value;

        Consume(TokenType.Delimiter, "(", "GG0126", "期望 '('");

        var parameters = new List<ParameterDecl>();

        if (!Check(TokenType.Delimiter, ")"))
        {
            parameters.Add(ParseParameterDecl());

            while (Match(TokenType.Delimiter, ","))
            {
                parameters.Add(ParseParameterDecl());
            }
        }

        Consume(TokenType.Delimiter, ")", "GG0127", "期望 ')'");

        TypeAnnotation? returnType = null;
        if (Match(TokenType.Punctuation, ":"))
        {
            returnType = ParseTypeAnnotation();
        }

        var body = ParseBlockStmt();

        return new FunctionDecl(SourceSpan.FromToken(startToken), name, parameters, returnType, body, attrs);
    }

    private FunctionDecl ParseFunctionDecl(IReadOnlyList<AttributeDecl>? attrs = null)
    {
        var startToken = ConsumeKeyword("micro", "GG0128", "期望 'micro' 关键字");

        var name = Consume(TokenType.Identifier, "GG0129", "期望函数名").Value;

        Consume(TokenType.Delimiter, "(", "GG0130", "期望 '('");

        var parameters = new List<ParameterDecl>();

        if (!Check(TokenType.Delimiter, ")"))
        {
            parameters.Add(ParseParameterDecl());

            while (Match(TokenType.Delimiter, ","))
            {
                parameters.Add(ParseParameterDecl());
            }
        }

        Consume(TokenType.Delimiter, ")", "GG0131", "期望 ')'");

        TypeAnnotation? returnType = null;
        if (Match(TokenType.Punctuation, ":"))
        {
            returnType = ParseTypeAnnotation();
        }

        var body = ParseBlockStmt();

        return new FunctionDecl(SourceSpan.FromToken(startToken), name, parameters, returnType, body, attrs ?? Array.Empty<AttributeDecl>());
    }

    private ParameterDecl ParseParameterDecl()
    {
        var startToken = Peek();

        var name = Consume(TokenType.Identifier, "GG0132", "期望参数名").Value;

        Consume(TokenType.Punctuation, ":", "GG0133", "期望 ':'");

        var paramType = ParseTypeAnnotation();

        return new ParameterDecl(SourceSpan.FromToken(startToken), name, paramType);
    }

    private WidgetDecl ParseWidgetDecl()
    {
        var startToken = ConsumeKeyword("widget", "GG0134", "期望 'widget' 关键字");

        var name = Consume(TokenType.Identifier, "GG0135", "期望 Widget 名").Value;

        Consume(TokenType.Delimiter, "{", "GG0136", "期望 '{'");

        var properties = new List<FieldDecl>();
        FunctionDecl? renderMethod = null;

        while (!Check(TokenType.Delimiter, "}") && !IsAtEnd())
        {
            if (Check(TokenType.Identifier, "render"))
            {
                renderMethod = ParseLifecycleMethod(Array.Empty<AttributeDecl>());
            }
            else
            {
                var propAttrs = ParseAttributes();
                properties.Add(ParseFieldDecl(propAttrs));
            }
        }

        Consume(TokenType.Delimiter, "}", "GG0137", "期望 '}'");

        return new WidgetDecl(SourceSpan.FromToken(startToken), name, properties, renderMethod);
    }

    private SceneDecl ParseSceneDecl()
    {
        var startToken = ConsumeKeyword("scene", "GG0138", "期望 'scene' 关键字");

        var name = Consume(TokenType.Identifier, "GG0139", "期望场景名").Value;

        Consume(TokenType.Delimiter, "{", "GG0140", "期望 '{'");

        var variables = new List<VariableDecl>();
        var methods = new List<FunctionDecl>();

        while (!Check(TokenType.Delimiter, "}") && !IsAtEnd())
        {
            if (Check(TokenType.Keyword, "let"))
            {
                variables.Add(ParseVariableDecl());
            }
            else
            {
                var methodAttrs = ParseAttributes();
                methods.Add(ParseLifecycleMethod(methodAttrs));
            }
        }

        Consume(TokenType.Delimiter, "}", "GG0141", "期望 '}'");

        return new SceneDecl(SourceSpan.FromToken(startToken), name, variables, methods);
    }

    private PluginDecl ParsePluginDecl()
    {
        var startToken = ConsumeKeyword("plugin", "GG0142", "期望 'plugin' 关键字");

        var name = Consume(TokenType.Identifier, "GG0143", "期望插件名").Value;

        Consume(TokenType.Delimiter, "{", "GG0144", "期望 '{'");

        var requiresArch = new List<string>();
        var providesMacros = new List<string>();
        var providesCapabilities = new List<string>();
        var functions = new List<FunctionDecl>();

        while (!Check(TokenType.Delimiter, "}") && !IsAtEnd())
        {
            var fieldName = Consume(TokenType.Identifier, "GG0145", "期望字段名").Value;

            Consume(TokenType.Operator, "=", "GG0146", "期望 '='");

            switch (fieldName)
            {
                case "requires_arch":
                    requiresArch = ParseStringArrayLiteral();
                    break;
                case "provides_macros":
                    providesMacros = ParseStringArrayLiteral();
                    break;
                case "provides_capabilities":
                    providesCapabilities = ParseStringArrayLiteral();
                    break;
                default:
                    _diagnostics?.AddWarning(
                        _filePath,
                        SourceSpan.FromToken(Previous()),
                        "GG0147",
                        $"未知的插件字段 '{fieldName}'");
                    SkipToSemicolonOrBrace();
                    break;
            }

            Match(TokenType.Delimiter, ";");
        }

        Consume(TokenType.Delimiter, "}", "GG0148", "期望 '}'");

        return new PluginDecl(SourceSpan.FromToken(startToken), name, requiresArch, providesMacros, providesCapabilities, functions);
    }

    private List<string> ParseStringArrayLiteral()
    {
        var result = new List<string>();

        Consume(TokenType.Delimiter, "[", "GG0149", "期望 '['");

        if (!Check(TokenType.Delimiter, "]"))
        {
            result.Add(Consume(TokenType.String, "GG0150", "期望字符串").Value);

            while (Match(TokenType.Delimiter, ","))
            {
                result.Add(Consume(TokenType.String, "GG0151", "期望字符串").Value);
            }
        }

        Consume(TokenType.Delimiter, "]", "GG0152", "期望 ']'");

        return result;
    }

    private void SkipToSemicolonOrBrace()
    {
        var depth = 0;

        while (!IsAtEnd())
        {
            if (Peek().TokenType == TokenType.Delimiter && Peek().Value == "{")
            {
                depth++;
            }
            else if (Peek().TokenType == TokenType.Delimiter && Peek().Value == "}")
            {
                if (depth == 0)
                {
                    return;
                }

                depth--;
            }
            else if (Peek().TokenType == TokenType.Delimiter && Peek().Value == ";" && depth == 0)
            {
                Advance();
                return;
            }

            Advance();
        }
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

            Consume(TokenType.Operator, ">", "GG0160", "期望 '>'");
        }

        return new TypeAnnotation(SourceSpan.FromToken(startToken), name, genericArgs);
    }

    private string ParseTypeName()
    {
        if (Check(TokenType.TypeKeyword))
        {
            return Advance().Value;
        }

        return Consume(TokenType.Identifier, "GG0161", "期望类型名").Value;
    }

    #endregion

    #region Private Methods - Statements

    private AstNode ParseStatement()
    {
        if (Check(TokenType.Keyword, "if"))
        {
            return ParseIfStmt();
        }

        if (Check(TokenType.Keyword, "loop"))
        {
            return ParseLoopStmt();
        }

        if (Check(TokenType.Keyword, "while"))
        {
            return ParseWhileStmt();
        }

        if (Check(TokenType.Keyword, "return"))
        {
            return ParseReturnStmt();
        }

        if (Check(TokenType.Delimiter, "{"))
        {
            return ParseBlockStmt();
        }

        return ParseExprStmt();
    }

    private BlockStmt ParseBlockStmt()
    {
        var startToken = Consume(TokenType.Delimiter, "{", "GG0170", "期望 '{'");

        var statements = new List<AstNode>();

        while (!Check(TokenType.Delimiter, "}") && !IsAtEnd())
        {
            var stmt = ParseDeclaration();

            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        Consume(TokenType.Delimiter, "}", "GG0171", "期望 '}'");

        return new BlockStmt(SourceSpan.FromToken(startToken), statements);
    }

    private IfStmt ParseIfStmt()
    {
        var startToken = ConsumeKeyword("if", "GG0172", "期望 'if' 关键字");

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

    private LoopStmt ParseLoopStmt()
    {
        var startToken = ConsumeKeyword("loop", "GG0173", "期望 'loop' 关键字");

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

    private WhileStmt ParseWhileStmt()
    {
        var startToken = ConsumeKeyword("while", "GG0174", "期望 'while' 关键字");

        var condition = ParseExpression();

        var body = ParseBlockStmt();

        return new WhileStmt(SourceSpan.FromToken(startToken), condition, body);
    }

    private ReturnStmt ParseReturnStmt()
    {
        var startToken = ConsumeKeyword("return", "GG0175", "期望 'return' 关键字");

        AstNode? value = null;

        if (!Check(TokenType.Delimiter, ";") && !Check(TokenType.Delimiter, "}"))
        {
            value = ParseExpression();
        }

        Match(TokenType.Delimiter, ";");

        return new ReturnStmt(SourceSpan.FromToken(startToken), value);
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
            if (Match(TokenType.Punctuation, "."))
            {
                var memberName = Consume(TokenType.Identifier, "GG0180", "期望成员名").Value;
                expr = new MemberAccessExpr(null, expr, memberName);
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

                Consume(TokenType.Delimiter, ")", "GG0181", "期望 ')'");
                expr = new CallExpr(null, expr, args);
            }
            else if (Check(TokenType.Delimiter, "["))
            {
                Advance();
                var index = ParseExpression();
                Consume(TokenType.Delimiter, "]", "GG0182", "期望 ']'");
                expr = new IndexExpr(null, expr, index);
            }
            else
            {
                break;
            }
        }

        return expr;
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

        if (Check(TokenType.Keyword, "create_entity"))
        {
            Advance();
            return new CallExpr(null, new IdentifierExpr(null, "create_entity"), Array.Empty<AstNode>());
        }

        if (Check(TokenType.Keyword, "destroy_entity"))
        {
            Advance();
            var arg = ParseExpression();
            return new CallExpr(null, new IdentifierExpr(null, "destroy_entity"), new[] { arg });
        }

        if (Check(TokenType.Keyword, "new"))
        {
            Advance();
            var typeName = ParseTypeAnnotation();
            var args = new List<AstNode>();

            if (Check(TokenType.Delimiter, "("))
            {
                Advance();

                if (!Check(TokenType.Delimiter, ")"))
                {
                    args.Add(ParseExpression());

                    while (Match(TokenType.Delimiter, ","))
                    {
                        args.Add(ParseExpression());
                    }
                }

                Consume(TokenType.Delimiter, ")", "GG0183", "期望 ')'");
            }

            return new CallExpr(null, new IdentifierExpr(null, $"new_{typeName.Name}"), args);
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
            Consume(TokenType.Delimiter, ")", "GG0184", "期望 ')'");

            if (Check(TokenType.Operator, "=>"))
            {
                return ParseLambdaAfterParams(new[] { new ParameterDecl(null, "it", new TypeAnnotation(null, "auto", Array.Empty<TypeAnnotation>())) });
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
            "GG0185",
            $"意外的标记 '{errorToken.Value}'");

        throw new ParseException($"意外的标记 '{errorToken.Value}'");
    }

    private LambdaExpr ParseLambdaAfterParams(IReadOnlyList<ParameterDecl> parameters)
    {
        Consume(TokenType.Operator, "=>", "GG0186", "期望 '=>'");

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

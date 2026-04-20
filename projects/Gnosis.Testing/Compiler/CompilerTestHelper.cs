using Gnosis.Compiler;
using Gnosis.Compiler.ValueObjects;
using Gnosis.Compiler.ValueObjects.AST;

namespace Gnosis.Testing.Compiler
{
    public static class CompilerTestHelper
    {
        public static Token CreateToken(TokenType type, string value, int line = 1, int column = 1)
        {
            return new Token(type, value, line, column);
        }

        public static Token CreateIdentifierToken(string name, int line = 1, int column = 1)
        {
            return CreateToken(TokenType.Identifier, name, line, column);
        }

        public static Token CreateKeywordToken(string keyword, int line = 1, int column = 1)
        {
            return CreateToken(TokenType.Keyword, keyword, line, column);
        }

        public static Token CreateTypeKeywordToken(string typeName, int line = 1, int column = 1)
        {
            return CreateToken(TokenType.TypeKeyword, typeName, line, column);
        }

        public static Token CreateLiteralToken(string value, int line = 1, int column = 1)
        {
            return CreateToken(TokenType.Literal, value, line, column);
        }

        public static Token CreateNumberToken(string value, int line = 1, int column = 1)
        {
            return CreateToken(TokenType.Number, value, line, column);
        }

        public static Token CreateStringToken(string value, int line = 1, int column = 1)
        {
            return CreateToken(TokenType.String, value, line, column);
        }

        public static Token CreateOperatorToken(string op, int line = 1, int column = 1)
        {
            return CreateToken(TokenType.Operator, op, line, column);
        }

        public static Token CreateDelimiterToken(string delimiter, int line = 1, int column = 1)
        {
            return CreateToken(TokenType.Delimiter, delimiter, line, column);
        }

        public static Token CreatePunctuationToken(string punct, int line = 1, int column = 1)
        {
            return CreateToken(TokenType.Punctuation, punct, line, column);
        }

        public static Token CreateAttributeToken(string attr, int line = 1, int column = 1)
        {
            return CreateToken(TokenType.Attribute, attr, line, column);
        }

        public static Token CreateMetaBlockStartToken(int line = 1, int column = 1)
        {
            return CreateToken(TokenType.MetaBlockStart, "<%", line, column);
        }

        public static Token CreateMetaBlockEndToken(int line = 1, int column = 1)
        {
            return CreateToken(TokenType.MetaBlockEnd, "%>", line, column);
        }

        public static Token CreateMetaExpressionToken(string expr, int line = 1, int column = 1)
        {
            return CreateToken(TokenType.MetaExpression, expr, line, column);
        }

        public static Token CreateEofToken(int line = 1, int column = 1)
        {
            return CreateToken(TokenType.Eof, "", line, column);
        }

        public static List<Token> CreateTokenList(params Token[] tokens)
        {
            return tokens.ToList();
        }

        public static CompilationUnit CreateCompilationUnit(params AstNode[] declarations)
        {
            return new CompilationUnit(null, declarations.ToList());
        }

        public static ComponentDecl CreateComponentDecl(string name, params FieldDecl[] fields)
        {
            return new ComponentDecl(null, name, Array.Empty<AttributeDecl>(), fields);
        }

        public static SystemDecl CreateSystemDecl(string name, params FunctionDecl[] methods)
        {
            return new SystemDecl(null, name, Array.Empty<AttributeDecl>(), Array.Empty<QueryExpr>(), methods);
        }

        public static FunctionDecl CreateFunctionDecl(string name, string? returnType = null, params ParameterDecl[] parameters)
        {
            var returnTypeAnnotation = returnType is not null
                ? new TypeAnnotation(null, returnType, Array.Empty<TypeAnnotation>())
                : null;
            return new FunctionDecl(null, name, parameters, returnTypeAnnotation, null, Array.Empty<AttributeDecl>());
        }

        public static VariableDecl CreateVariableDecl(string name, string? type = null, bool isMutable = false)
        {
            var typeAnnotation = type is not null
                ? new TypeAnnotation(null, type, Array.Empty<TypeAnnotation>())
                : null;
            return new VariableDecl(null, name, typeAnnotation, null, isMutable);
        }

        public static FieldDecl CreateFieldDecl(string name, string type)
        {
            return new FieldDecl(null, name, new TypeAnnotation(null, type, Array.Empty<TypeAnnotation>()), null, Array.Empty<AttributeDecl>());
        }

        public static ParameterDecl CreateParameterDecl(string name, string type)
        {
            return new ParameterDecl(null, name, new TypeAnnotation(null, type, Array.Empty<TypeAnnotation>()));
        }

        public static TypeAnnotation CreateTypeAnnotation(string name, params TypeAnnotation[] genericArgs)
        {
            return new TypeAnnotation(null, name, genericArgs);
        }

        public static AttributeDecl CreateAttributeDecl(string name, params KeyValuePair<string, string>[] args)
        {
            return new AttributeDecl(null, name, args);
        }

        public static IdentifierExpr CreateIdentifierExpr(string name)
        {
            return new IdentifierExpr(null, name);
        }

        public static LiteralExpr CreateLiteralExpr(LiteralType kind, object? value)
        {
            return new LiteralExpr(null, kind, value);
        }

        public static BinaryExpr CreateBinaryExpr(AstNode left, string op, AstNode right)
        {
            return new BinaryExpr(null, left, op, right);
        }

        public static CallExpr CreateCallExpr(AstNode callee, params AstNode[] args)
        {
            return new CallExpr(null, callee, args);
        }

        public static string CreateTestSource(params string[] lines)
        {
            return string.Join(Environment.NewLine, lines);
        }

        public static void AssertTokenSequence(IEnumerable<Token> actual, IEnumerable<Token> expected)
        {
            var actualList = actual.ToList();
            var expectedList = expected.ToList();

            if (actualList.Count != expectedList.Count)
            {
                throw new Exception($"Token 数量不匹配：期望 {expectedList.Count}，实际 {actualList.Count}");
            }

            for (int i = 0; i < actualList.Count; i++)
            {
                if (actualList[i].TokenType != expectedList[i].TokenType || actualList[i].Value != expectedList[i].Value)
                {
                    throw new Exception($"Token {i} 不匹配：期望 ({expectedList[i].TokenType}, \"{expectedList[i].Value}\")，实际 ({actualList[i].TokenType}, \"{actualList[i].Value}\")");
                }
            }
        }

        public static string CreateComponentSource(string componentName, params string[] fields)
        {
            var fieldLines = fields.Select(f => $"    {f}");
            return CreateTestSource(
                $"component {componentName}",
                "{",
                string.Join(Environment.NewLine, fieldLines),
                "}"
            );
        }

        public static string CreateSystemSource(string systemName, params string[] methods)
        {
            var methodLines = methods.Select(m => $"    {m}");
            return CreateTestSource(
                $"system {systemName}",
                "{",
                string.Join(Environment.NewLine, methodLines),
                "}"
            );
        }

        public static string CreateFunctionSource(string functionName, string returnType, params string[] statements)
        {
            var statementLines = statements.Select(s => $"    {s}");
            return CreateTestSource(
                $"micro {functionName}() : {returnType}",
                "{",
                string.Join(Environment.NewLine, statementLines),
                "}"
            );
        }
    }
}

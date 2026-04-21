using NUnit.Framework;
using Gnosis.Compiler;
using Gnosis.Compiler.ValueObjects;
using Gnosis.Testing;
using Gnosis.Testing.Compiler;

namespace TestProject.Compiler
{
    // Lexer 测试类
    [TestFixture]
    public class LexerTests : TestBase
    {
        private ILexer _lexer;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            // _lexer = new Lexer();
        }

        // 测试空源码
        [Test]
        public void Tokenize_EmptySource_ShouldReturnEofToken()
        {
            // Arrange
            var source = "";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens, Is.Not.Null);
            Assert.That(tokens.Count, Is.EqualTo(1));
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.Eof));
        }

        // 测试空白字符
        [Test]
        public void Tokenize_WhitespaceOnly_ShouldReturnEofToken()
        {
            // Arrange
            var source = "   \t\n\r\n   ";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens, Is.Not.Null);
            Assert.That(tokens.Count, Is.EqualTo(1));
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.Eof));
        }

        // 测试标识符
        [Test]
        public void Tokenize_Identifier_ShouldReturnIdentifierToken()
        {
            // Arrange
            var source = "playerName";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(2)); // identifier + eof
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[0].Value, Is.EqualTo("playerName"));
        }

        // 测试关键字
        [Test]
        public void Tokenize_Keyword_ShouldReturnKeywordToken()
        {
            // Arrange
            var source = "component";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(2)); // keyword + eof
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.Keyword));
            Assert.That(tokens[0].Value, Is.EqualTo("component"));
        }

        // 测试数字字面量
        [Test]
        public void Tokenize_Number_ShouldReturnNumberToken()
        {
            // Arrange
            var source = "42";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(2)); // number + eof
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.Number));
            Assert.That(tokens[0].Value, Is.EqualTo("42"));
        }

        // 测试浮点数字面量
        [Test]
        public void Tokenize_FloatNumber_ShouldReturnNumberToken()
        {
            // Arrange
            var source = "3.14";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(2)); // number + eof
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.Number));
            Assert.That(tokens[0].Value, Is.EqualTo("3.14"));
        }

        // 测试字符串字面量
        [Test]
        public void Tokenize_String_ShouldReturnStringToken()
        {
            // Arrange
            var source = "\"Hello, World!\"";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(2)); // string + eof
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.String));
            Assert.That(tokens[0].Value, Is.EqualTo("Hello, World!"));
        }

        // 测试运算符
        [Test]
        public void Tokenize_Operator_ShouldReturnOperatorToken()
        {
            // Arrange
            var source = "+";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(2)); // operator + eof
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.Operator));
            Assert.That(tokens[0].Value, Is.EqualTo("+"));
        }

        // 测试标点符号
        [Test]
        public void Tokenize_Punctuation_ShouldReturnPunctuationToken()
        {
            // Arrange
            var source = ";";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(2)); // punctuation + eof
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.Punctuation));
            Assert.That(tokens[0].Value, Is.EqualTo(";"));
        }

        // 测试元语言块开始
        [Test]
        public void Tokenize_MetaBlockStart_ShouldReturnMetaBlockStartToken()
        {
            // Arrange
            var source = "<%";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(2)); // meta block start + eof
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.MetaBlockStart));
            Assert.That(tokens[0].Value, Is.EqualTo("<%"));
        }

        // 测试元语言块结束
        [Test]
        public void Tokenize_MetaBlockEnd_ShouldReturnMetaBlockEndToken()
        {
            // Arrange
            var source = "%>";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(2)); // meta block end + eof
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.MetaBlockEnd));
            Assert.That(tokens[0].Value, Is.EqualTo("%>"));
        }

        // 测试复合表达式
        [Test]
        public void Tokenize_ComplexExpression_ShouldReturnCorrectTokens()
        {
            // Arrange
            var source = "let x = 42;";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens.Count, Is.EqualTo(6)); // let, x, =, 42, ;, eof
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.Keyword));
            Assert.That(tokens[0].Value, Is.EqualTo("let"));
            Assert.That(tokens[1].TokenType, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[1].Value, Is.EqualTo("x"));
            Assert.That(tokens[2].TokenType, Is.EqualTo(TokenType.Operator));
            Assert.That(tokens[2].Value, Is.EqualTo("="));
            Assert.That(tokens[3].TokenType, Is.EqualTo(TokenType.Number));
            Assert.That(tokens[3].Value, Is.EqualTo("42"));
            Assert.That(tokens[4].TokenType, Is.EqualTo(TokenType.Punctuation));
            Assert.That(tokens[4].Value, Is.EqualTo(";"));
        }

        // 测试组件定义
        [Test]
        public void Tokenize_ComponentDefinition_ShouldReturnCorrectTokens()
        {
            // Arrange
            var source = CompilerTestHelper.CreateComponentSource("Position", "x: float", "y: float", "z: float");

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens, Is.Not.Empty);
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.Keyword));
            Assert.That(tokens[0].Value, Is.EqualTo("component"));
            Assert.That(tokens[1].TokenType, Is.EqualTo(TokenType.Identifier));
            Assert.That(tokens[1].Value, Is.EqualTo("Position"));
        }

        // 测试行号和列号
        [Test]
        public void Tokenize_MultipleLines_ShouldTrackLineAndColumn()
        {
            // Arrange
            var source = "let x = 1;\nlet y = 2;";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens, Is.Not.Empty);
            // 第一行
            Assert.That(tokens[0].Line, Is.EqualTo(1));
            // 第二行
            var secondLineToken = tokens.First(t => t.Value == "y");
            Assert.That(secondLineToken.Line, Is.EqualTo(2));
        }

        // 测试注释
        [Test]
        public void Tokenize_Comment_ShouldSkipComment()
        {
            // Arrange
            var source = "// 这是一个注释\nlet x = 1;";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens, Is.Not.Empty);
            // 注释应该被跳过，第一个 token 应该是 let
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.Keyword));
            Assert.That(tokens[0].Value, Is.EqualTo("let"));
        }

        // 测试多行注释
        [Test]
        public void Tokenize_MultiLineComment_ShouldSkipComment()
        {
            // Arrange
            var source = "/* 这是一个\n多行注释 */\nlet x = 1;";

            // Act
            var tokens = _lexer.Tokenize(source);

            // Assert
            Assert.That(tokens, Is.Not.Empty);
            // 注释应该被跳过，第一个 token 应该是 let
            Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.Keyword));
            Assert.That(tokens[0].Value, Is.EqualTo("let"));
        }
    }
}

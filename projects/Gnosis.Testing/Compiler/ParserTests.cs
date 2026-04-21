using NUnit.Framework;
using Gnosis.Compiler;
using Gnosis.Compiler.ValueObjects;
using Gnosis.Testing;
using Gnosis.Testing.Compiler;

namespace TestProject.Compiler
{
    // Parser 测试类
    [TestFixture]
    public class ParserTests : TestBase
    {
        private IParser _parser;
        private ILexer _lexer;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            // _parser = new Parser();
            // _lexer = new Lexer();
        }

        // 辅助方法：将源码解析为 AST
        private AstNode ParseSource(string source)
        {
            var tokens = _lexer.Tokenize(source);
            return _parser.Parse(tokens);
        }

        // 测试空源码
        [Test]
        public void Parse_EmptySource_ShouldReturnEmptyAst()
        {
            // Arrange
            var source = "";

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
        }

        // 测试组件定义
        [Test]
        public void Parse_ComponentDefinition_ShouldReturnComponentNode()
        {
            // Arrange
            var source = CompilerTestHelper.CreateComponentSource(
                "Position",
                "x: float",
                "y: float",
                "z: float"
            );

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
            // 这里需要根据实际的 AST 结构进行断言
        }

        // 测试系统定义
        [Test]
        public void Parse_SystemDefinition_ShouldReturnSystemNode()
        {
            // Arrange
            var source = CompilerTestHelper.CreateSystemSource(
                "MovementSystem",
                "on_update(deltaTime: float) { }"
            );

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
        }

        // 测试函数定义
        [Test]
        public void Parse_FunctionDefinition_ShouldReturnFunctionNode()
        {
            // Arrange
            var source = CompilerTestHelper.CreateFunctionSource(
                "add",
                "int",
                "return a + b;"
            );

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
        }

        // 测试变量声明
        [Test]
        public void Parse_VariableDeclaration_ShouldReturnVariableNode()
        {
            // Arrange
            var source = "let x: int = 42;";

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
        }

        // 测试赋值表达式
        [Test]
        public void Parse_AssignmentExpression_ShouldReturnAssignmentNode()
        {
            // Arrange
            var source = "x = 42;";

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
        }

        // 测试二元运算表达式
        [Test]
        public void Parse_BinaryExpression_ShouldReturnBinaryNode()
        {
            // Arrange
            var source = "let result = a + b * c;";

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
        }

        // 测试函数调用
        [Test]
        public void Parse_FunctionCall_ShouldReturnCallNode()
        {
            // Arrange
            var source = "print(\"Hello, World!\");";

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
        }

        // 测试 if 语句
        [Test]
        public void Parse_IfStatement_ShouldReturnIfNode()
        {
            // Arrange
            var source = @"
                if (x > 0) {
                    print(""positive"");
                }
            ";

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
        }

        // 测试 if-else 语句
        [Test]
        public void Parse_IfElseStatement_ShouldReturnIfNode()
        {
            // Arrange
            var source = @"
                if (x > 0) {
                    print(""positive"");
                } else {
                    print(""non-positive"");
                }
            ";

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
        }

        // 测试 while 循环
        [Test]
        public void Parse_WhileLoop_ShouldReturnWhileNode()
        {
            // Arrange
            var source = @"
                while (x > 0) {
                    x = x - 1;
                }
            ";

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
        }

        // 测试 for 循环
        [Test]
        public void Parse_ForLoop_ShouldReturnForNode()
        {
            // Arrange
            var source = @"
                for (let i = 0; i < 10; i = i + 1) {
                    print(i);
                }
            ";

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
        }

        // 测试 return 语句
        [Test]
        public void Parse_ReturnStatement_ShouldReturnReturnNode()
        {
            // Arrange
            var source = "return 42;";

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
        }

        // 测试元语言块
        [Test]
        public void Parse_MetaBlock_ShouldReturnMetaBlockNode()
        {
            // Arrange
            var source = @"
                <% 
                    for (var entity in entities) {
                        // 元语言代码
                    }
                %>
            ";

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
        }

        // 测试嵌套结构
        [Test]
        public void Parse_NestedStructures_ShouldReturnCorrectAst()
        {
            // Arrange
            var source = @"
                component Position {
                    x: float;
                    y: float;
                    z: float;
                }

                system MovementSystem {
                    on_update(deltaTime: float) {
                        foreach (entity in Query.all<Position>()) {
                            entity.Position.x += 1.0;
                        }
                    }
                }
            ";

            // Act
            var ast = ParseSource(source);

            // Assert
            Assert.That(ast, Is.Not.Null);
        }

        // 测试语法错误处理
        [Test]
        public void Parse_SyntaxError_ShouldThrowException()
        {
            // Arrange
            var source = "let x = ;"; // 缺少表达式

            // Act & Assert
            Assert.Throws<Exception>(() => ParseSource(source));
        }

        // 测试未闭合的括号
        [Test]
        public void Parse_UnclosedParenthesis_ShouldThrowException()
        {
            // Arrange
            var source = "let x = (1 + 2;"; // 缺少右括号

            // Act & Assert
            Assert.Throws<Exception>(() => ParseSource(source));
        }

        // 测试未闭合的大括号
        [Test]
        public void Parse_UnclosedBrace_ShouldThrowException()
        {
            // Arrange
            var source = "function test() {"; // 缺少右大括号

            // Act & Assert
            Assert.Throws<Exception>(() => ParseSource(source));
        }
    }
}

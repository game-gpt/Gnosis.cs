namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示函数声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// fn add(a: int, b: int): int {        // 带参数和返回类型的函数
///     return a + b;
/// }
/// 
/// fn greet(name: string) {              // 无返回类型的函数
///     print("Hello, " + name);
/// }
/// 
/// fn main() {                           // 无参数函数
///     run();
/// }
/// 
/// [inline]                              // 带属性的函数
/// fn fastAdd(a: int, b: int): int {
///     return a + b;
/// }
/// </code>
/// </remarks>
public sealed record FunctionDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<ParameterDecl> Parameters,
    TypeAnnotation? ReturnType,
    BlockStmt? Body,
    IReadOnlyList<AttributeDecl> Attributes) : AstNode(NodeType.FunctionDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFunctionDecl(this);
}

namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示函数声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 带参数和返回类型的函数
/// micro add(a: int, b: int): int {
///     return a + b
/// }
/// 
/// # 无返回类型的函数
/// micro greet(name: string) {
///     print("Hello, " + name)
/// }
/// 
/// # 无参数函数
/// micro main {
///     run()
/// }
/// 
/// # 带 GGScript/GGShader 特性标注的函数
/// [inline]
/// micro fast_add(a: int, b: int): int {
///     return a + b
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

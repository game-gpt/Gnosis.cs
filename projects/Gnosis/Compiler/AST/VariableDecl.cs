namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示变量声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// let x = 10;                   // 不可变变量声明
/// let x: int = 10;              // 带类型注解的不可变变量
/// var y = 20;                   // 可变变量声明
/// var y: string = "hello";      // 带类型注解的可变变量
/// let z;                        // 无初始值的声明（需要类型注解）
/// let z: float;                 // 无初始值但带类型注解
/// </code>
/// </remarks>
public sealed record VariableDecl(
    SourceSpan? Span,
    string Name,
    TypeAnnotation? VarType,
    AstNode? Initializer,
    bool IsMutable) : AstNode(NodeType.VariableDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitVariableDecl(this);
}

using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示变量声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 不可变变量声明
/// let x = 10
/// 
/// # 带类型注解的不可变变量
/// let x: int = 10
/// 
/// # 可变变量声明
/// var y = 20
/// 
/// # 带类型注解的可变变量
/// var y: string = "hello"
/// 
/// # 无初始值的声明（需要类型注解）
/// let z: float
/// </code>
/// </remarks>
public sealed record VariableDecl(
    string Name,
    TypeAnnotation? VarType,
    AstNode? Initializer,
    bool IsMutable,
    SourceSpan? Span = null) : AstNode(NodeType.VariableDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitVariableDecl(this);
}

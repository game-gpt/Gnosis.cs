using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示 ECS 组件声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 简单组件
/// component Position {
///     x: float
///     y: float
/// }
/// 
/// # 带默认值的组件
/// component Velocity {
///     dx: float = 0.0
///     dy: float = 0.0
/// }
/// 
/// # 带 GGScript 特性标注的组件
/// [serialize]
/// component Health {
///     current: int
///     max: int
/// }
/// </code>
/// </remarks>
public sealed record ComponentDecl(
    string Name,
    IReadOnlyList<AttributeDecl> Attributes,
    IReadOnlyList<FieldDecl> Fields,
    SourceSpan? Span = null) : AstNode(NodeType.ComponentDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitComponentDecl(this);
}

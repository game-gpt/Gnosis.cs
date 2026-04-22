using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示 ECS 系统声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// system MovementSystem {
///     # 查询声明
///     query all(Position, Velocity)
///     
///     # 生命周期方法
///     micro on_init { }
///     
///     micro on_update(dt: float) {
///         for entity in query {
///             entity.position.x += entity.velocity.dx * dt
///         }
///     }
///     
///     micro on_destroy { }
/// }
/// 
/// # 带 GGScript 特性标注的系统
/// [priority(100)]
/// system RenderSystem {
///     query all(Position, Sprite)
///     
///     micro on_update(dt: float) { }
/// }
/// </code>
/// </remarks>
public sealed record SystemDecl(
    string Name,
    IReadOnlyList<AttributeDecl> Attributes,
    IReadOnlyList<QueryExpr> Queries,
    IReadOnlyList<FunctionDecl> LifecycleMethods,
    SourceSpan? Span = null) : AstNode(NodeType.SystemDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitSystemDecl(this);
}

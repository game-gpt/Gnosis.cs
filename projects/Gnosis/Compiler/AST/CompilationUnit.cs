namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示编译单元节点（源文件的根节点）
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 文件: game.gnosis
/// 
/// import "std/io"
/// 
/// component Position { x: float, y: float }
/// component Velocity { dx: float, dy: float }
/// 
/// system MovementSystem {
///     query all(Position, Velocity)
///     fn onUpdate(dt: float) { }
/// }
/// 
/// scene GameScene {
///     fn onEnter() { }
///     fn onUpdate(dt: float) { }
/// }
/// </code>
/// </remarks>
public sealed record CompilationUnit(
    SourceSpan? Span,
    IReadOnlyList<AstNode> Declarations,
    string? FilePath = null) : AstNode(NodeType.CompilationUnit, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCompilationUnit(this);
}

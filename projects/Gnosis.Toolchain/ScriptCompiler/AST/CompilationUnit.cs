using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

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
///     micro on_update(dt: float) { }
/// }
/// 
/// scene GameScene {
///     micro on_enter { }
///     micro on_update(dt: float) { }
/// }
/// </code>
/// </remarks>
public sealed record CompilationUnit(
    IReadOnlyList<AstNode> Declarations,
    string? FilePath = null,
    SourceSpan? Span = null) : AstNode(NodeType.CompilationUnit, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCompilationUnit(this);
}

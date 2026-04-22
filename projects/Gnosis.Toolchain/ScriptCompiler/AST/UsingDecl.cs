using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示命名空间使用声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 导入整个命名空间
/// using std::io
/// 
/// # 选择性导入
/// using std::math::{sin, cos}
/// 
/// # 导入所有内容
/// using engine::render::*
/// </code>
/// </remarks>
public sealed record UsingDecl(
    string NamespacePath,
    IReadOnlyList<string> Selections,
    SourceSpan? Span = null) : AstNode(NodeType.UsingDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUsingDecl(this);
}

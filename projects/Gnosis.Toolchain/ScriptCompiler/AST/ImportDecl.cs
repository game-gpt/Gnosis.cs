using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示模块导入声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 导入模块
/// import "std/math"
/// 
/// # 带别名的导入
/// import "std/io" as io
/// 
/// # 使用别名简化访问
/// import "engine/core" as core
/// </code>
/// </remarks>
public sealed record ImportDecl(
    string ModulePath,
    string? Alias = null,
    SourceSpan? Span = null) : AstNode(NodeType.ImportDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitImportDecl(this);
}

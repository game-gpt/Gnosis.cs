using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示参数声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 简单参数
/// micro foo(x: int) { }
/// 
/// # 多参数
/// micro bar(a: int, b: string) { }
/// 
/// # 数组类型参数
/// micro baz(data: vec3[]) { }
/// 
/// # 带 GGShader 特性标注的参数
/// micro qux([range(0, 10)] value: int) { }
/// </code>
/// </remarks>
public sealed record ParameterDecl(
    string Name,
    TypeAnnotation ParamType,
    IReadOnlyList<AttributeDecl> Attributes,
    SourceSpan? Span = null) : AstNode(NodeType.ParameterDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitParameterDecl(this);
}

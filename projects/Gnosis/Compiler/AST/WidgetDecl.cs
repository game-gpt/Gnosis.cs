namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示 UI 组件声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 简单按钮组件
/// widget Button {
///     text: string
///     on_click: () =&gt; void
///     
///     micro render {
///         &lt;button onclick={on_click}&gt;{text}&lt;/button&gt;
///     }
/// }
/// 
/// # 滑动条组件
/// widget Slider {
///     value: float
///     min: float = 0.0
///     max: float = 100.0
///     on_change: (float) =&gt; void
///     
///     micro render {
///         &lt;input type="range" min={min} max={max} value={value} onchange={on_change} /&gt;
///     }
/// }
/// </code>
/// </remarks>
public sealed record WidgetDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<FieldDecl> Properties,
    FunctionDecl? RenderMethod) : AstNode(NodeType.WidgetDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitWidgetDecl(this);
}

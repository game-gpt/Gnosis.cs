namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示场景声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 主菜单场景
/// scene MainMenu {
///     let selected_option = 0
///     
///     # 进入场景时调用
///     micro on_enter {
///         play_music("menu_bgm")
///     }
///     
///     # 离开场景时调用
///     micro on_exit {
///         stop_music()
///     }
///     
///     # 每帧更新
///     micro on_update(dt: float) {
///         handle_input()
///         render()
///     }
/// }
/// 
/// # 游戏关卡场景
/// scene GameLevel {
///     let score = 0
///     let player: Entity
///     
///     micro on_enter { }
///     micro on_update(dt: float) { }
///     micro on_exit { }
/// }
/// </code>
/// </remarks>
public sealed record SceneDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<VariableDecl> Variables,
    IReadOnlyList<FunctionDecl> LifecycleMethods) : AstNode(NodeType.SceneDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitSceneDecl(this);
}

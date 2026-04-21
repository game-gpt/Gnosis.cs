namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示场景声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// scene MainMenu {                       // 主菜单场景
///     let selectedOption = 0;
///     
///     fn onEnter() {                     // 进入场景时调用
///         playMusic("menu_bgm");
///     }
///     
///     fn onExit() {                      // 离开场景时调用
///         stopMusic();
///     }
///     
///     fn onUpdate(dt: float) {           // 每帧更新
///         handleInput();
///         render();
///     }
/// }
/// 
/// scene GameLevel {                      // 游戏关卡场景
///     let score = 0;
///     let player: Entity;
///     
///     fn onEnter() { }
///     fn onUpdate(dt: float) { }
///     fn onExit() { }
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

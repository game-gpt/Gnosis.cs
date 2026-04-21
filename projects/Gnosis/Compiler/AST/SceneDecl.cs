namespace Gnosis.Compiler.AST;

public sealed record SceneDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<VariableDecl> Variables,
    IReadOnlyList<FunctionDecl> LifecycleMethods) : AstNode(NodeType.SceneDecl, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitSceneDecl(this);
}

using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record PluginDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<string> RequiresArch,
    IReadOnlyList<string> ProvidesMacros,
    IReadOnlyList<string> ProvidesCapabilities,
    IReadOnlyList<FunctionDecl> Functions) : AstNode(NodeType.PluginDecl, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitPluginDecl(this);
}

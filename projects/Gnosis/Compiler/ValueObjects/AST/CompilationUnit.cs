using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record CompilationUnit(
    SourceSpan? Span,
    IReadOnlyList<AstNode> Declarations,
    string? FilePath = null) : AstNode(NodeType.CompilationUnit, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCompilationUnit(this);
}

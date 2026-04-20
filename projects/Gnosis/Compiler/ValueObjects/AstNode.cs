using Gnosis.Compiler.ValueObjects.AST;

namespace Gnosis.Compiler.ValueObjects;

public abstract record AstNode(NodeType Type, SourceSpan? Span)
{
    public abstract T Accept<T>(IAstVisitor<T> visitor);
}

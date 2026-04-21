using Gnosis.Compiler.AST;

namespace Gnosis.Compiler;

public abstract record AstNode(NodeType Type, SourceSpan? Span)
{
    public abstract T Accept<T>(IAstVisitor<T> visitor);
}

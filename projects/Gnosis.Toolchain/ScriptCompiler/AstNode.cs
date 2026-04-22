using Gnosis.Core.Diagnostic;
using Gnosis.Toolchain.ScriptCompiler.AST;

namespace Gnosis.Toolchain.ScriptCompiler;

public abstract record AstNode(NodeType Type, SourceSpan? Span = null)
{
    public abstract T Accept<T>(IAstVisitor<T> visitor);
}

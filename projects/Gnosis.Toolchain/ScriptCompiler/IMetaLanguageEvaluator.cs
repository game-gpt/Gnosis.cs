using Oak.Valkyrie.AST;

namespace Gnosis.Toolchain.ScriptCompiler;

public interface IMetaLanguageEvaluator
{
    AstNode Evaluate(AstNode ast, ChannelMacros macros);
}

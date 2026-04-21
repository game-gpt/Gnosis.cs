namespace Gnosis.Compiler;

public interface IMetaLanguageEvaluator
{
    AstNode Evaluate(AstNode ast, ChannelMacros macros);
}

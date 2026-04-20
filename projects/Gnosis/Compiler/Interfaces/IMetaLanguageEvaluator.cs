namespace Gnosis.Compiler.Interfaces;

public interface IMetaLanguageEvaluator
{
    AstNode Evaluate(AstNode ast, ChannelMacros macros);
}

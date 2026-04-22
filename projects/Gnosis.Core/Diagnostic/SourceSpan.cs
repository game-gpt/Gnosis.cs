namespace Gnosis.Core.Diagnostic;

public sealed record SourceSpan(string FilePath, int StartLine, int StartColumn, int EndLine, int EndColumn)
{
    public override string ToString() => $"({StartLine},{StartColumn})-({EndLine},{EndColumn})";
}

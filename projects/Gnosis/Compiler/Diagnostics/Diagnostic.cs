using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.Diagnostics;

public sealed record Diagnostic
{
    public DiagnosticSeverity Severity { get; }
    public string FilePath { get; }
    public SourceSpan? Span { get; }
    public string ErrorCode { get; }
    public string Message { get; }
    public IReadOnlyList<string> Suggestions { get; }

    public Diagnostic(
        DiagnosticSeverity severity,
        string filePath,
        SourceSpan? span,
        string errorCode,
        string message,
        IReadOnlyList<string>? suggestions = null)
    {
        Severity = severity;
        FilePath = filePath;
        Span = span;
        ErrorCode = errorCode;
        Message = message;
        Suggestions = suggestions ?? Array.Empty<string>();
    }

    public static Diagnostic Create(
        DiagnosticSeverity severity,
        string filePath,
        SourceSpan? span,
        string errorCode,
        string message,
        params string[] suggestions)
    {
        return new Diagnostic(severity, filePath, span, errorCode, message, suggestions);
    }

    public override string ToString()
    {
        var location = Span is not null
            ? $"{FilePath}{Span}"
            : FilePath;

        var result = $"[{Severity}] {ErrorCode}: {Message} ({location})";

        if (Suggestions.Count > 0)
        {
            result += Environment.NewLine + string.Join(
                Environment.NewLine,
                Suggestions.Select(s => $"  建议: {s}"));
        }

        return result;
    }
}

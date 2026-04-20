using System.Text;
using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.Diagnostics;

public class DiagnosticSink
{
    #region Fields

    private readonly List<Diagnostic> _diagnostics = new();

    #endregion

    #region Properties

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.AsReadOnly();

    public IEnumerable<Diagnostic> Errors => _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);

    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    #endregion

    #region Public Methods

    public void Add(Diagnostic diagnostic)
    {
        _diagnostics.Add(diagnostic);
    }

    public void AddError(
        string filePath,
        SourceSpan? span,
        string errorCode,
        string message,
        params string[] suggestions)
    {
        _diagnostics.Add(Diagnostic.Create(
            DiagnosticSeverity.Error, filePath, span, errorCode, message, suggestions));
    }

    public void AddWarning(
        string filePath,
        SourceSpan? span,
        string errorCode,
        string message,
        params string[] suggestions)
    {
        _diagnostics.Add(Diagnostic.Create(
            DiagnosticSeverity.Warning, filePath, span, errorCode, message, suggestions));
    }

    public void AddInfo(
        string filePath,
        SourceSpan? span,
        string errorCode,
        string message)
    {
        _diagnostics.Add(Diagnostic.Create(
            DiagnosticSeverity.Info, filePath, span, errorCode, message));
    }

    public void Clear()
    {
        _diagnostics.Clear();
    }

    public IEnumerable<Diagnostic> GetErrors()
    {
        return _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
    }

    public IEnumerable<Diagnostic> GetWarnings()
    {
        return _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);
    }

    public string FormatAll()
    {
        if (_diagnostics.Count == 0)
        {
            return "无诊断信息。";
        }

        var sb = new StringBuilder();

        foreach (var diagnostic in _diagnostics)
        {
            sb.AppendLine(diagnostic.ToString());
        }

        return sb.ToString().TrimEnd();
    }

    #endregion
}

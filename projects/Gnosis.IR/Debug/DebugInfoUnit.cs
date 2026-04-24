using Gnosis.Core.Diagnostic;

namespace Gnosis.IR.Debug;

public sealed class DebugInfoUnit
{
    #region Properties

    public string ModuleName { get; }

    public IReadOnlyList<SourceMapEntry> SourceMap { get; }

    public IReadOnlyList<FunctionDebugInfo> Functions { get; }

    public IReadOnlyList<string> StringTable { get; }

    #endregion

    #region Constructors

    public DebugInfoUnit(
        string moduleName,
        IReadOnlyList<SourceMapEntry> sourceMap,
        IReadOnlyList<FunctionDebugInfo> functions,
        IReadOnlyList<string> stringTable)
    {
        ModuleName = moduleName;
        SourceMap = sourceMap;
        Functions = functions;
        StringTable = stringTable;
    }

    #endregion
}

public sealed class SourceMapEntry
{
    #region Properties

    public int BytecodeOffset { get; }

    public SourceSpan? SourceSpan { get; }

    #endregion

    #region Constructors

    public SourceMapEntry(int bytecodeOffset, SourceSpan? sourceSpan)
    {
        BytecodeOffset = bytecodeOffset;
        SourceSpan = sourceSpan;
    }

    #endregion
}

public sealed class FunctionDebugInfo
{
    #region Properties

    public string Name { get; }

    public int StartOffset { get; }

    public int EndOffset { get; }

    public IReadOnlyList<ParameterDebugInfo> Parameters { get; }

    public IReadOnlyList<LocalVariableDebugInfo> LocalVariables { get; }

    public SourceSpan? DefinitionSpan { get; }

    #endregion

    #region Constructors

    public FunctionDebugInfo(
        string name,
        int startOffset,
        int endOffset,
        IReadOnlyList<ParameterDebugInfo> parameters,
        IReadOnlyList<LocalVariableDebugInfo> localVariables,
        SourceSpan? definitionSpan)
    {
        Name = name;
        StartOffset = startOffset;
        EndOffset = endOffset;
        Parameters = parameters;
        LocalVariables = localVariables;
        DefinitionSpan = definitionSpan;
    }

    #endregion
}

public sealed class ParameterDebugInfo
{
    #region Properties

    public string Name { get; }

    public int Index { get; }

    public SourceSpan? DefinitionSpan { get; }

    #endregion

    #region Constructors

    public ParameterDebugInfo(string name, int index, SourceSpan? definitionSpan)
    {
        Name = name;
        Index = index;
        DefinitionSpan = definitionSpan;
    }

    #endregion
}

public sealed class LocalVariableDebugInfo
{
    #region Properties

    public string Name { get; }

    public int Index { get; }

    public int ScopeStartOffset { get; }

    public int ScopeEndOffset { get; }

    public SourceSpan? DefinitionSpan { get; }

    #endregion

    #region Constructors

    public LocalVariableDebugInfo(
        string name,
        int index,
        int scopeStartOffset,
        int scopeEndOffset,
        SourceSpan? definitionSpan)
    {
        Name = name;
        Index = index;
        ScopeStartOffset = scopeStartOffset;
        ScopeEndOffset = scopeEndOffset;
        DefinitionSpan = definitionSpan;
    }

    #endregion
}

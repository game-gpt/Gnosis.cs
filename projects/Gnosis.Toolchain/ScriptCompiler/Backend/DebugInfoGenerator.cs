using Gnosis.Core.Diagnostic;
using Gnosis.IR.Debug;
using Gnosis.IR.Instruction;
using Oak.Valkyrie.AST;

namespace Gnosis.Toolchain.ScriptCompiler.Backend;

public sealed class DebugInfoGenerator
{
    #region Public Methods

    public DebugInfoUnit Generate(
        string moduleName,
        IReadOnlyList<(int Offset, SourceSpan? Span)> sourceMap,
        CompilationUnit ast)
    {
        var stringTable = BuildStringTable(sourceMap, ast);
        var sourceMapEntries = ConvertSourceMap(sourceMap);
        var functionDebugInfos = ExtractFunctionDebugInfos(ast, sourceMap);

        return new DebugInfoUnit(moduleName, sourceMapEntries, functionDebugInfos, stringTable);
    }

    #endregion

    #region Private Methods - String Table

    private static List<string> BuildStringTable(
        IReadOnlyList<(int Offset, SourceSpan? Span)> sourceMap,
        CompilationUnit ast)
    {
        var strings = new HashSet<string>();

        if (!string.IsNullOrEmpty(ast.FilePath))
        {
            strings.Add(ast.FilePath);
        }

        foreach (var (_, span) in sourceMap)
        {
            if (span is not null && !string.IsNullOrEmpty(span.FilePath))
            {
                strings.Add(span.FilePath);
            }
        }

        foreach (var decl in ast.Declarations)
        {
            CollectDeclarationStrings(decl, strings);
        }

        return strings.OrderBy(s => s).ToList();
    }

    private static void CollectDeclarationStrings(AstNode node, HashSet<string> strings)
    {
        switch (node.Type)
        {
            case NodeType.FunctionDecl:
                var func = (FunctionDecl)node;
                strings.Add(func.Name);
                foreach (var param in func.Parameters)
                {
                    strings.Add(param.Name);
                }

                break;

            case NodeType.SystemDecl:
                var sys = (SystemDecl)node;
                strings.Add(sys.Name);
                foreach (var method in sys.Methods)
                {
                    CollectDeclarationStrings(method, strings);
                }

                break;

            case NodeType.ComponentDecl:
                var comp = (ComponentDecl)node;
                strings.Add(comp.Name);
                foreach (var field in comp.Fields)
                {
                    strings.Add(field.Name);
                }

                break;

            case NodeType.WidgetDecl:
                var widget = (WidgetDecl)node;
                strings.Add(widget.Name);
                if (widget.RenderMethod is not null)
                {
                    CollectDeclarationStrings(widget.RenderMethod, strings);
                }

                break;

            case NodeType.PluginDecl:
                var plugin = (PluginDecl)node;
                strings.Add(plugin.Name);
                foreach (var func in plugin.Functions)
                {
                    CollectDeclarationStrings(func, strings);
                }

                break;
        }
    }

    #endregion

    #region Private Methods - Source Map Conversion

    private static List<SourceMapEntry> ConvertSourceMap(IReadOnlyList<(int Offset, SourceSpan? Span)> sourceMap)
    {
        var entries = new List<SourceMapEntry>(sourceMap.Count);

        foreach (var (offset, span) in sourceMap)
        {
            entries.Add(new SourceMapEntry(offset, span));
        }

        return entries;
    }

    #endregion

    #region Private Methods - Function Debug Info

    private static List<FunctionDebugInfo> ExtractFunctionDebugInfos(
        CompilationUnit ast,
        IReadOnlyList<(int Offset, SourceSpan? Span)> sourceMap)
    {
        var functions = new List<FunctionDebugInfo>();

        foreach (var decl in ast.Declarations)
        {
            CollectFunctionDebugInfos(decl, sourceMap, functions);
        }

        return functions;
    }

    private static void CollectFunctionDebugInfos(
        AstNode node,
        IReadOnlyList<(int Offset, SourceSpan? Span)> sourceMap,
        List<FunctionDebugInfo> functions)
    {
        switch (node.Type)
        {
            case NodeType.FunctionDecl:
                var func = (FunctionDecl)node;
                functions.Add(BuildFunctionDebugInfo(func, sourceMap));
                break;

            case NodeType.SystemDecl:
                var sys = (SystemDecl)node;
                foreach (var method in sys.Methods)
                {
                    functions.Add(BuildFunctionDebugInfo(method, sourceMap));
                }

                break;

            case NodeType.WidgetDecl:
                var widget = (WidgetDecl)node;
                if (widget.RenderMethod is not null)
                {
                    functions.Add(BuildFunctionDebugInfo(widget.RenderMethod, sourceMap));
                }

                break;

            case NodeType.PluginDecl:
                var plugin = (PluginDecl)node;
                foreach (var func in plugin.Functions)
                {
                    functions.Add(BuildFunctionDebugInfo(func, sourceMap));
                }

                break;
        }
    }

    private static FunctionDebugInfo BuildFunctionDebugInfo(
        FunctionDecl func,
        IReadOnlyList<(int Offset, SourceSpan? Span)> sourceMap)
    {
        var definitionSpan = ConvertOakSpan(func.Span);

        var startOffset = FindFunctionStartOffset(func, sourceMap);
        var endOffset = FindFunctionEndOffset(startOffset, sourceMap);

        var parameters = new List<ParameterDebugInfo>();

        for (var i = 0; i < func.Parameters.Count; i++)
        {
            var param = func.Parameters[i];
            var paramSpan = ConvertOakSpan(param.Span);
            parameters.Add(new ParameterDebugInfo(param.Name, i, paramSpan));
        }

        var localVariables = ExtractLocalVariables(func, startOffset, endOffset);

        return new FunctionDebugInfo(
            func.Name,
            startOffset,
            endOffset,
            parameters,
            localVariables,
            definitionSpan);
    }

    private static int FindFunctionStartOffset(
        FunctionDecl func,
        IReadOnlyList<(int Offset, SourceSpan? Span)> sourceMap)
    {
        var funcSpan = func.Span;

        if (funcSpan is null)
        {
            return 0;
        }

        foreach (var (offset, span) in sourceMap)
        {
            if (span is not null && SpansOverlap(span, funcSpan.Value))
            {
                return offset;
            }
        }

        return 0;
    }

    private static int FindFunctionEndOffset(
        int startOffset,
        IReadOnlyList<(int Offset, SourceSpan? Span)> sourceMap)
    {
        var maxOffset = startOffset;

        foreach (var (offset, _) in sourceMap)
        {
            if (offset > maxOffset)
            {
                maxOffset = offset;
            }
        }

        return maxOffset;
    }

    private static List<LocalVariableDebugInfo> ExtractLocalVariables(
        FunctionDecl func,
        int funcStartOffset,
        int funcEndOffset)
    {
        var locals = new List<LocalVariableDebugInfo>();
        var localIndex = func.Parameters.Count;

        CollectLocalVariables(func.Body, funcStartOffset, funcEndOffset, ref localIndex, locals);

        return locals;
    }

    private static void CollectLocalVariables(
        AstNode? node,
        int scopeStart,
        int scopeEnd,
        ref int localIndex,
        List<LocalVariableDebugInfo> locals)
    {
        if (node is null)
        {
            return;
        }

        if (node is BlockStmt block)
        {
            foreach (var stmt in block.Statements)
            {
                if (stmt is VariableDecl varDecl)
                {
                    var varSpan = ConvertOakSpan(varDecl.Span);
                    locals.Add(new LocalVariableDebugInfo(
                        varDecl.Name,
                        localIndex++,
                        scopeStart,
                        scopeEnd,
                        varSpan));
                }
                else
                {
                    CollectLocalVariables(stmt, scopeStart, scopeEnd, ref localIndex, locals);
                }
            }
        }
        else if (node is ForStmt forStmt)
        {
            if (forStmt.Initializer is VariableDecl initVar)
            {
                var varSpan = ConvertOakSpan(initVar.Span);
                locals.Add(new LocalVariableDebugInfo(
                    initVar.Name,
                    localIndex++,
                    scopeStart,
                    scopeEnd,
                    varSpan));
            }

            CollectLocalVariables(forStmt.Body, scopeStart, scopeEnd, ref localIndex, locals);
        }
        else if (node is LoopStmt loopStmt && loopStmt.IteratorName is not null)
        {
            locals.Add(new LocalVariableDebugInfo(
                loopStmt.IteratorName,
                localIndex++,
                scopeStart,
                scopeEnd,
                ConvertOakSpan(loopStmt.Span)));
            CollectLocalVariables(loopStmt.Body, scopeStart, scopeEnd, ref localIndex, locals);
        }
    }

    #endregion

    #region Private Methods - Span Helpers

    private static SourceSpan? ConvertOakSpan(Oak.Diagnostics.SourceSpan? oakSpan)
    {
        if (oakSpan is null)
        {
            return null;
        }

        return new SourceSpan(
            oakSpan.Value.FilePath ?? "",
            oakSpan.Value.StartLine,
            oakSpan.Value.StartColumn,
            oakSpan.Value.EndLine,
            oakSpan.Value.EndColumn);
    }

    private static bool SpansOverlap(SourceSpan a, Oak.Diagnostics.SourceSpan b)
    {
        if (!string.IsNullOrEmpty(a.FilePath) && !string.IsNullOrEmpty(b.FilePath) && a.FilePath != b.FilePath)
        {
            return false;
        }

        return a.StartLine <= b.EndLine && b.StartLine <= a.EndLine;
    }

    #endregion
}

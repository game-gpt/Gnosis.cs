using System.Text;
using Gnosis.Core.Diagnostic;

namespace Gnosis.IR.Debug;

public sealed class DebugInfoSerializer
{
    #region Constants

    private const uint DebugMagicNumber = 0x47474449;

    private const ushort CurrentVersion = 1;

    #endregion

    #region Public Methods

    public byte[] Serialize(DebugInfoUnit debugInfo)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(DebugMagicNumber);
        writer.Write(CurrentVersion);

        var nameBytes = Encoding.UTF8.GetBytes(debugInfo.ModuleName);
        writer.Write((ushort)nameBytes.Length);
        writer.Write(nameBytes);

        WriteStringTable(writer, debugInfo.StringTable);

        WriteSourceMap(writer, debugInfo.SourceMap, debugInfo.StringTable);

        WriteFunctions(writer, debugInfo.Functions, debugInfo.StringTable);

        return ms.ToArray();
    }

    public DebugInfoUnit Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var magic = reader.ReadUInt32();

        if (magic != DebugMagicNumber)
        {
            throw new InvalidDataException($"无效的调试信息魔数: 0x{magic:X8}，期望 0x{DebugMagicNumber:X8}");
        }

        var version = reader.ReadUInt16();

        if (version != CurrentVersion)
        {
            throw new InvalidDataException($"不支持的调试信息版本: {version}，期望 {CurrentVersion}");
        }

        var nameLength = reader.ReadUInt16();
        var moduleName = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));

        var stringTable = ReadStringTable(reader);
        var sourceMap = ReadSourceMap(reader, stringTable);
        var functions = ReadFunctions(reader, stringTable);

        return new DebugInfoUnit(moduleName, sourceMap, functions, stringTable);
    }

    #endregion

    #region Private Methods - String Table

    private static void WriteStringTable(BinaryWriter writer, IReadOnlyList<string> table)
    {
        writer.Write(table.Count);

        foreach (var str in table)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            writer.Write((ushort)bytes.Length);
            writer.Write(bytes);
        }
    }

    private static List<string> ReadStringTable(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        var table = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var length = reader.ReadUInt16();
            var bytes = reader.ReadBytes(length);
            table.Add(Encoding.UTF8.GetString(bytes));
        }

        return table;
    }

    #endregion

    #region Private Methods - Source Map

    private static void WriteSourceMap(BinaryWriter writer, IReadOnlyList<SourceMapEntry> sourceMap, IReadOnlyList<string> stringTable)
    {
        writer.Write(sourceMap.Count);

        foreach (var entry in sourceMap)
        {
            writer.Write(entry.BytecodeOffset);

            if (entry.SourceSpan is not null)
            {
                writer.Write((byte)1);
                var filePathIndex = GetStringIndex(entry.SourceSpan.FilePath, stringTable);
                writer.Write(filePathIndex);
                writer.Write(entry.SourceSpan.StartLine);
                writer.Write(entry.SourceSpan.StartColumn);
                writer.Write(entry.SourceSpan.EndLine);
                writer.Write(entry.SourceSpan.EndColumn);
            }
            else
            {
                writer.Write((byte)0);
            }
        }
    }

    private static List<SourceMapEntry> ReadSourceMap(BinaryReader reader, IReadOnlyList<string> stringTable)
    {
        var count = reader.ReadInt32();
        var sourceMap = new List<SourceMapEntry>(count);

        for (var i = 0; i < count; i++)
        {
            var offset = reader.ReadInt32();
            var hasSpan = reader.ReadByte();

            SourceSpan? span = null;

            if (hasSpan == 1)
            {
                var filePathIndex = reader.ReadInt32();
                var filePath = filePathIndex >= 0 && filePathIndex < stringTable.Count
                    ? stringTable[filePathIndex]
                    : "";
                var startLine = reader.ReadInt32();
                var startColumn = reader.ReadInt32();
                var endLine = reader.ReadInt32();
                var endColumn = reader.ReadInt32();
                span = new SourceSpan(filePath, startLine, startColumn, endLine, endColumn);
            }

            sourceMap.Add(new SourceMapEntry(offset, span));
        }

        return sourceMap;
    }

    #endregion

    #region Private Methods - Functions

    private static void WriteFunctions(BinaryWriter writer, IReadOnlyList<FunctionDebugInfo> functions, IReadOnlyList<string> stringTable)
    {
        writer.Write(functions.Count);

        foreach (var func in functions)
        {
            var nameIndex = GetStringIndex(func.Name, stringTable);
            writer.Write(nameIndex);
            writer.Write(func.StartOffset);
            writer.Write(func.EndOffset);

            WriteSpan(writer, func.DefinitionSpan, stringTable);

            writer.Write(func.Parameters.Count);

            foreach (var param in func.Parameters)
            {
                var paramNameIndex = GetStringIndex(param.Name, stringTable);
                writer.Write(paramNameIndex);
                writer.Write(param.Index);
                WriteSpan(writer, param.DefinitionSpan, stringTable);
            }

            writer.Write(func.LocalVariables.Count);

            foreach (var local in func.LocalVariables)
            {
                var localNameIndex = GetStringIndex(local.Name, stringTable);
                writer.Write(localNameIndex);
                writer.Write(local.Index);
                writer.Write(local.ScopeStartOffset);
                writer.Write(local.ScopeEndOffset);
                WriteSpan(writer, local.DefinitionSpan, stringTable);
            }
        }
    }

    private static List<FunctionDebugInfo> ReadFunctions(BinaryReader reader, IReadOnlyList<string> stringTable)
    {
        var count = reader.ReadInt32();
        var functions = new List<FunctionDebugInfo>(count);

        for (var i = 0; i < count; i++)
        {
            var nameIndex = reader.ReadInt32();
            var name = stringTable[nameIndex];
            var startOffset = reader.ReadInt32();
            var endOffset = reader.ReadInt32();
            var definitionSpan = ReadSpan(reader, stringTable);

            var paramCount = reader.ReadInt32();
            var parameters = new List<ParameterDebugInfo>(paramCount);

            for (var j = 0; j < paramCount; j++)
            {
                var paramNameIndex = reader.ReadInt32();
                var paramName = stringTable[paramNameIndex];
                var paramIndex = reader.ReadInt32();
                var paramSpan = ReadSpan(reader, stringTable);
                parameters.Add(new ParameterDebugInfo(paramName, paramIndex, paramSpan));
            }

            var localCount = reader.ReadInt32();
            var locals = new List<LocalVariableDebugInfo>(localCount);

            for (var j = 0; j < localCount; j++)
            {
                var localNameIndex = reader.ReadInt32();
                var localName = stringTable[localNameIndex];
                var localIndex = reader.ReadInt32();
                var scopeStart = reader.ReadInt32();
                var scopeEnd = reader.ReadInt32();
                var localSpan = ReadSpan(reader, stringTable);
                locals.Add(new LocalVariableDebugInfo(localName, localIndex, scopeStart, scopeEnd, localSpan));
            }

            functions.Add(new FunctionDebugInfo(name, startOffset, endOffset, parameters, locals, definitionSpan));
        }

        return functions;
    }

    #endregion

    #region Private Methods - Span Helpers

    private static void WriteSpan(BinaryWriter writer, SourceSpan? span, IReadOnlyList<string> stringTable)
    {
        if (span is not null)
        {
            writer.Write((byte)1);
            var filePathIndex = GetStringIndex(span.FilePath, stringTable);
            writer.Write(filePathIndex);
            writer.Write(span.StartLine);
            writer.Write(span.StartColumn);
            writer.Write(span.EndLine);
            writer.Write(span.EndColumn);
        }
        else
        {
            writer.Write((byte)0);
        }
    }

    private static SourceSpan? ReadSpan(BinaryReader reader, IReadOnlyList<string> stringTable)
    {
        var hasSpan = reader.ReadByte();

        if (hasSpan == 0)
        {
            return null;
        }

        var filePathIndex = reader.ReadInt32();
        var filePath = filePathIndex >= 0 && filePathIndex < stringTable.Count
            ? stringTable[filePathIndex]
            : "";
        var startLine = reader.ReadInt32();
        var startColumn = reader.ReadInt32();
        var endLine = reader.ReadInt32();
        var endColumn = reader.ReadInt32();

        return new SourceSpan(filePath, startLine, startColumn, endLine, endColumn);
    }

    #endregion

    #region Private Methods - String Table Index

    private static int GetStringIndex(string value, IReadOnlyList<string> table)
    {
        for (var i = 0; i < table.Count; i++)
        {
            if (table[i] == value)
            {
                return i;
            }
        }

        return -1;
    }

    #endregion
}

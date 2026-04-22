using System.Text;
using Gnosis.Compiler.AST;
using Gnosis.Compiler.Diagnostics;
using Gnosis.Interpreter.IR;

namespace Gnosis.Compiler.Backend;

public class BytecodeGenerator : IBytecodeGenerator
{
    #region Fields

    private const uint MagicNumber = 0x47474243;

    private const ushort CurrentVersion = 1;

    private readonly DiagnosticSink _diagnostics;
    private readonly List<byte> _instructions;
    private readonly Dictionary<string, int> _constants;
    private readonly List<object> _constantPool;
    private readonly Dictionary<string, int> _exportedSymbols;
    private readonly List<string> _importedSymbols;
    private readonly List<string> _dependencies;
    private readonly Dictionary<string, int> _localVariables;
    private readonly Dictionary<string, int> _nativeBindings;
    private readonly List<(int Offset, SourceSpan? Span)> _sourceMap;
    private int _localCount;

    #endregion

    #region Constructors

    public BytecodeGenerator(DiagnosticSink diagnostics)
    {
        _diagnostics = diagnostics;
        _instructions = [];
        _constants = new Dictionary<string, int>();
        _constantPool = [];
        _exportedSymbols = new Dictionary<string, int>();
        _importedSymbols = [];
        _dependencies = [];
        _localVariables = new Dictionary<string, int>();
        _nativeBindings = new Dictionary<string, int>();
        _sourceMap = [];
        _localCount = 0;
    }

    #endregion

    #region Public Methods

    public BytecodeModule Generate(AstNode ast, ArchTarget arch, bool isEditorBuild)
    {
        Reset();

        var moduleName = "main";

        if (ast is CompilationUnit unit)
        {
            GenerateCompilationUnit(unit, arch, isEditorBuild);
            moduleName = ExtractModuleName(unit);
        }

        var instructions = _instructions.ToArray();

        return new BytecodeModule(moduleName, instructions, _dependencies);
    }

    public CompilationResult GenerateFull(AstNode ast, ArchTarget arch, bool isEditorBuild)
    {
        var module = Generate(ast, arch, isEditorBuild);

        var bytecode = SerializeModule(module);
        var vmSource = GenerateVmSource(module);

        return new CompilationResult(bytecode, vmSource);
    }

    #endregion

    #region Private Methods - Reset

    private void Reset()
    {
        _instructions.Clear();
        _constants.Clear();
        _constantPool.Clear();
        _exportedSymbols.Clear();
        _importedSymbols.Clear();
        _dependencies.Clear();
        _localVariables.Clear();
        _nativeBindings.Clear();
        _sourceMap.Clear();
        _localCount = 0;
    }

    #endregion

    #region Private Methods - Compilation Unit

    private void GenerateCompilationUnit(CompilationUnit unit, ArchTarget arch, bool isEditorBuild)
    {
        foreach (var decl in unit.Declarations)
        {
            GenerateDeclaration(decl, arch, isEditorBuild);
        }

        Emit(OpCode.Halt);
    }

    private void GenerateDeclaration(AstNode decl, ArchTarget arch, bool isEditorBuild)
    {
        switch (decl.Type)
        {
            case NodeType.ComponentDecl:
                GenerateComponentDecl((ComponentDecl)decl);
                break;
            case NodeType.SystemDecl:
                GenerateSystemDecl((SystemDecl)decl, arch, isEditorBuild);
                break;
            case NodeType.FunctionDecl:
                GenerateFunctionDecl((FunctionDecl)decl);
                break;
            case NodeType.VariableDecl:
                GenerateVariableDecl((VariableDecl)decl);
                break;
            case NodeType.PluginDecl:
                GeneratePluginDecl((PluginDecl)decl);
                break;
            case NodeType.ImportDecl:
                GenerateImportDecl((ImportDecl)decl);
                break;
            case NodeType.WidgetDecl:
                GenerateWidgetDecl((WidgetDecl)decl);
                break;
            case NodeType.ExprStmt:
                GenerateExprStmt((TermExpressionStatement)decl);
                break;
            case NodeType.BlockStmt:
                GenerateBlockStmt((BlockStmt)decl);
                break;
            case NodeType.StructDecl:
            case NodeType.UsingDecl:
            case NodeType.UniformBindingDecl:
            case NodeType.DiscardStmt:
                break;
            case NodeType.ForStmt:
                GenerateForStmt((ForStmt)decl);
                break;
        }
    }

    #endregion

    #region Private Methods - Component

    private void GenerateComponentDecl(ComponentDecl decl)
    {
        var nameIdx = AddConstant(decl.Name);
        _exportedSymbols[decl.Name] = nameIdx;

        foreach (var field in decl.Fields)
        {
            AddConstant($"{decl.Name}.{field.Name}");
        }
    }

    #endregion

    #region Private Methods - System

    private void GenerateSystemDecl(SystemDecl decl, ArchTarget arch, bool isEditorBuild)
    {
        var nameIdx = AddConstant(decl.Name);
        _exportedSymbols[decl.Name] = nameIdx;

        foreach (var query in decl.Queries)
        {
            GenerateQueryRegistration(query);
        }

        foreach (var method in decl.LifecycleMethods)
        {
            GenerateFunctionDecl(method);
        }
    }

    private void GenerateQueryRegistration(QueryExpr query)
    {
        foreach (var compType in query.ComponentTypes)
        {
            AddConstant(compType.Name);
        }

        switch (query.Kind)
        {
            case QueryKind.All:
                Emit(OpCode.QueryAll);
                break;
            case QueryKind.Any:
                Emit(OpCode.QueryAny);
                break;
            case QueryKind.None:
                Emit(OpCode.QueryAll);
                break;
        }

        EmitInt(query.ComponentTypes.Count);
    }

    #endregion

    #region Private Methods - Function

    private void GenerateFunctionDecl(FunctionDecl decl)
    {
        var nameIdx = AddConstant(decl.Name);
        _exportedSymbols[decl.Name] = nameIdx;

        _localVariables.Clear();
        _localCount = 0;

        foreach (var param in decl.Parameters)
        {
            _localVariables[param.Name] = _localCount++;
        }

        if (decl.Body is not null)
        {
            GenerateBlockStmt(decl.Body);
        }

        Emit(OpCode.Return);
    }

    #endregion

    #region Private Methods - Variable

    private void GenerateVariableDecl(VariableDecl decl)
    {
        if (decl.Initializer is not null)
        {
            GenerateExpression(decl.Initializer);
        }
        else
        {
            Emit(OpCode.PushInt32);
            EmitInt(0);
        }

        _localVariables[decl.Name] = _localCount++;
        Emit(OpCode.StoreLocal);
        EmitInt(_localVariables[decl.Name]);
    }

    #endregion

    #region Private Methods - Plugin

    private void GeneratePluginDecl(PluginDecl decl)
    {
        var nameIdx = AddConstant(decl.Name);
        _exportedSymbols[decl.Name] = nameIdx;

        foreach (var macro in decl.ProvidesMacros)
        {
            AddConstant(macro);
        }

        foreach (var cap in decl.ProvidesCapabilities)
        {
            AddConstant(cap);
        }

        foreach (var func in decl.Functions)
        {
            GenerateFunctionDecl(func);
        }
    }

    #endregion

    #region Private Methods - Import

    private void GenerateImportDecl(ImportDecl decl)
    {
        _importedSymbols.Add(decl.ModulePath);
        _dependencies.Add(decl.ModulePath);
    }

    #endregion

    #region Private Methods - Widget

    private void GenerateWidgetDecl(WidgetDecl decl)
    {
        var nameIdx = AddConstant(decl.Name);
        _exportedSymbols[decl.Name] = nameIdx;

        if (decl.RenderMethod is not null)
        {
            GenerateFunctionDecl(decl.RenderMethod);
        }
    }

    #endregion

    #region Private Methods - Statements

    private void GenerateBlockStmt(BlockStmt stmt)
    {
        foreach (var statement in stmt.Statements)
        {
            GenerateStatement(statement);
        }
    }

    private void GenerateStatement(AstNode stmt)
    {
        switch (stmt.Type)
        {
            case NodeType.VariableDecl:
                GenerateVariableDecl((VariableDecl)stmt);
                break;
            case NodeType.ExprStmt:
                GenerateExprStmt((TermExpressionStatement)stmt);
                break;
            case NodeType.ReturnStmt:
                GenerateReturnStmt((ReturnStatement)stmt);
                break;
            case NodeType.IfStmt:
                GenerateIfStmt((IfStatement)stmt);
                break;
            case NodeType.LoopStmt:
                GenerateLoopStmt((LoopStmt)stmt);
                break;
            case NodeType.WhileStmt:
                GenerateWhileStmt((WhileStmt)stmt);
                break;
            case NodeType.BlockStmt:
                GenerateBlockStmt((BlockStmt)stmt);
                break;
            case NodeType.ForStmt:
                GenerateForStmt((ForStmt)stmt);
                break;
            case NodeType.DiscardStmt:
            case NodeType.UsingDecl:
            case NodeType.UniformBindingDecl:
            case NodeType.StructDecl:
                break;
        }
    }

    private void GenerateExprStmt(TermExpressionStatement stmt)
    {
        GenerateExpression(stmt.Expression);

        if (NeedsPop(stmt.Expression))
        {
            Emit(OpCode.Pop);
        }
    }

    private void GenerateReturnStmt(ReturnStatement statement)
    {
        if (statement.Value is not null)
        {
            GenerateExpression(statement.Value);
        }

        Emit(OpCode.Return);
    }

    private void GenerateIfStmt(IfStatement statement)
    {
        GenerateExpression(statement.Condition);
        Emit(OpCode.JumpIfFalse);
        var elseJump = _instructions.Count;
        EmitInt(0);

        GenerateStatement(statement.ThenBlock);

        Emit(OpCode.Jump);
        var endJump = _instructions.Count;
        EmitInt(0);

        PatchJump(elseJump, _instructions.Count);

        if (statement.ElseBlock is not null)
        {
            GenerateStatement(statement.ElseBlock);
        }

        PatchJump(endJump, _instructions.Count);
    }

    private void GenerateLoopStmt(LoopStmt stmt)
    {
        var loopStart = _instructions.Count;

        if (stmt.Iterable is not null && stmt.IteratorName is not null)
        {
            GenerateExpression(stmt.Iterable);
            Emit(OpCode.CallNative);
            var iterNativeIdx = AddConstant("iterator_next");
            EmitInt(iterNativeIdx);

            Emit(OpCode.StoreLocal);
            EmitInt(GetOrAddLocal(stmt.IteratorName));

            Emit(OpCode.Dup);
            Emit(OpCode.JumpIfFalse);
            var exitJump = _instructions.Count;
            EmitInt(0);

            GenerateBlockStmt(stmt.Body);

            Emit(OpCode.Jump);
            EmitInt(loopStart);

            PatchJump(exitJump, _instructions.Count);
            Emit(OpCode.Pop);
        }
        else
        {
            GenerateBlockStmt(stmt.Body);
            Emit(OpCode.Jump);
            EmitInt(loopStart);
        }
    }

    private void GenerateWhileStmt(WhileStmt stmt)
    {
        var loopStart = _instructions.Count;

        GenerateExpression(stmt.Condition);
        Emit(OpCode.JumpIfFalse);
        var exitJump = _instructions.Count;
        EmitInt(0);

        GenerateBlockStmt(stmt.Body);

        Emit(OpCode.Jump);
        EmitInt(loopStart);

        PatchJump(exitJump, _instructions.Count);
    }

    private void GenerateForStmt(ForStmt stmt)
    {
        if (stmt.Initializer is not null)
        {
            GenerateStatement(stmt.Initializer);
        }

        var loopStart = _instructions.Count;

        if (stmt.Condition is not null)
        {
            GenerateExpression(stmt.Condition);
            Emit(OpCode.JumpIfFalse);
            var jumpToEnd = _instructions.Count;
            EmitInt(0);

            GenerateBlockStmt(stmt.Body);

            if (stmt.Update is not null)
            {
                GenerateExpression(stmt.Update);
            }

            Emit(OpCode.Jump);
            EmitInt(loopStart);

            PatchJump(jumpToEnd, _instructions.Count);
        }
        else
        {
            GenerateBlockStmt(stmt.Body);

            if (stmt.Update is not null)
            {
                GenerateExpression(stmt.Update);
            }

            Emit(OpCode.Jump);
            EmitInt(loopStart);
        }
    }

    #endregion

    #region Private Methods - Expressions

    private void GenerateExpression(AstNode expr)
    {
        switch (expr.Type)
        {
            case NodeType.LiteralExpr:
                GenerateLiteralExpr((LiteralExpr)expr);
                break;
            case NodeType.IdentifierExpr:
                GenerateIdentifierExpr((IdentifierNode)expr);
                break;
            case NodeType.BinaryExpr:
                GenerateBinaryExpr((BinaryExpr)expr);
                break;
            case NodeType.UnaryExpr:
                GenerateUnaryExpr((TermUnaryExpression)expr);
                break;
            case NodeType.CallExpr:
                GenerateCallExpr((TermCallExpression)expr);
                break;
            case NodeType.MemberAccessExpr:
                GenerateMemberAccessExpr((MemberAccessExpr)expr);
                break;
            case NodeType.IndexExpr:
                GenerateIndexExpr((TermIndexExpression)expr);
                break;
            case NodeType.AssignmentExpr:
                GenerateAssignmentExpr((AssignmentExpr)expr);
                break;
            case NodeType.LambdaExpr:
                GenerateLambdaExpr((LambdaExpr)expr);
                break;
            case NodeType.QueryExpr:
                GenerateQueryExpr((QueryExpr)expr);
                break;
            case NodeType.MetaBlock:
                GenerateMetaBlock((MetaBlock)expr);
                break;
            case NodeType.SwizzleExpr:
                GenerateExpression(((SwizzleExpr)expr).Object);
                break;
        }
    }

    private void GenerateLiteralExpr(LiteralExpr expr)
    {
        switch (expr.LiteralKind)
        {
            case LiteralType.Number:
                GenerateNumberLiteral(expr.Value?.ToString() ?? "0");
                break;
            case LiteralType.String:
                Emit(OpCode.PushInt32);
                EmitInt(AddConstant(expr.Value?.ToString() ?? ""));
                break;
            case LiteralType.Boolean:
                Emit(OpCode.PushInt8);
                EmitByte(expr.Value?.ToString() == "true" ? (byte)1 : (byte)0);
                break;
            case LiteralType.Null:
                Emit(OpCode.PushInt32);
                EmitInt(0);
                break;
        }
    }

    private void GenerateNumberLiteral(string value)
    {
        if (value.StartsWith("0x") || value.StartsWith("0X"))
        {
            var intVal = Convert.ToInt32(value, 16);
            Emit(OpCode.PushInt32);
            EmitInt(intVal);
            return;
        }

        if (value.EndsWith('f') || value.EndsWith('F'))
        {
            value = value[..^1];
        }

        if (value.Contains('.') || value.Contains('e') || value.Contains('E'))
        {
            if (float.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var floatVal))
            {
                Emit(OpCode.PushFloat32);
                EmitFloat(floatVal);
            }
        }
        else
        {
            if (int.TryParse(value, out var intVal))
            {
                Emit(OpCode.PushInt32);
                EmitInt(intVal);
            }
        }
    }

    private void GenerateIdentifierExpr(IdentifierNode node)
    {
        if (node.Name == "create_entity")
        {
            Emit(OpCode.SpawnEntity);
            return;
        }

        if (_localVariables.TryGetValue(node.Name, out var localIdx))
        {
            Emit(OpCode.LoadLocal);
            EmitInt(localIdx);
            return;
        }

        Emit(OpCode.LoadGlobal);
        EmitInt(AddConstant(node.Name));
    }

    private void GenerateBinaryExpr(BinaryExpr expr)
    {
        GenerateExpression(expr.Left);
        GenerateExpression(expr.Right);

        var op = expr.Operator switch
        {
            "+" => OpCode.AddInt,
            "-" => OpCode.SubInt,
            "*" => OpCode.MulInt,
            "/" => OpCode.DivInt,
            "==" => OpCode.SubInt,
            "!=" => OpCode.SubInt,
            "<" => OpCode.SubInt,
            ">" => OpCode.SubInt,
            "<=" => OpCode.SubInt,
            ">=" => OpCode.SubInt,
            _ => OpCode.AddInt
        };

        Emit(op);
    }

    private void GenerateUnaryExpr(TermUnaryExpression expression)
    {
        GenerateExpression(expression.Operand);

        if (expression.Operator == "-")
        {
            Emit(OpCode.NegInt);
        }
        else if (expression.Operator == "!")
        {
            Emit(OpCode.PushInt8);
            EmitByte(1);
            Emit(OpCode.SubInt);
        }
    }

    private void GenerateCallExpr(TermCallExpression expression)
    {
        if (expression.Callee is IdentifierNode idExpr)
        {
            switch (idExpr.Name)
            {
                case "create_entity":
                    Emit(OpCode.SpawnEntity);
                    return;
                case "destroy_entity":
                    GenerateExpression(expression.Arguments[0]);
                    Emit(OpCode.DestroyEntity);
                    return;
                default:
                    if (idExpr.Name.StartsWith("new_"))
                    {
                        var typeName = idExpr.Name[4..];
                        foreach (var arg in expression.Arguments)
                        {
                            GenerateExpression(arg);
                        }

                        Emit(OpCode.NewObject);
                        EmitInt(AddConstant(typeName));
                        return;
                    }

                    break;
            }
        }

        if (expression.Callee is MemberAccessExpr memberExpr)
        {
            GenerateMemberCall(memberExpr, expression.Arguments);
            return;
        }

        foreach (var arg in expression.Arguments)
        {
            GenerateExpression(arg);
        }

        if (_nativeBindings.TryGetValue(expression.Callee.ToString() ?? "", out var nativeIdx) ||
            expression.Callee is IdentifierNode calleeId && _nativeBindings.TryGetValue(calleeId.Name, out nativeIdx))
        {
            Emit(OpCode.CallNative);
            EmitInt(nativeIdx);
        }
        else
        {
            Emit(OpCode.Call);
            EmitInt(AddConstant(expression.Callee.ToString() ?? ""));
        }
    }

    private void GenerateMemberCall(MemberAccessExpr memberExpr, IReadOnlyList<AstNode> arguments)
    {
        GenerateExpression(memberExpr.Object);

        foreach (var arg in arguments)
        {
            GenerateExpression(arg);
        }

        var methodName = memberExpr.MemberName;

        switch (methodName)
        {
            case "add":
                Emit(OpCode.AddComponent);
                EmitInt(AddConstant("component_type"));
                break;
            case "get":
                Emit(OpCode.GetComponent);
                EmitInt(AddConstant("component_type"));
                break;
            case "has":
                Emit(OpCode.GetComponent);
                EmitInt(AddConstant("component_type"));
                break;
            case "remove":
                Emit(OpCode.RemoveComponent);
                EmitInt(AddConstant("component_type"));
                break;
            default:
                Emit(OpCode.GetField);
                EmitInt(AddConstant(methodName));
                break;
        }
    }

    private void GenerateMemberAccessExpr(MemberAccessExpr expr)
    {
        GenerateExpression(expr.Object);
        Emit(OpCode.GetField);
        EmitInt(AddConstant(expr.MemberName));
    }

    private void GenerateIndexExpr(TermIndexExpression expression)
    {
        GenerateExpression(expression.Object);
        GenerateExpression(expression.Index);
        Emit(OpCode.GetField);
        EmitInt(AddConstant("index"));
    }

    private void GenerateAssignmentExpr(AssignmentExpr expr)
    {
        if (expr.Operator == "=")
        {
            GenerateExpression(expr.Value);
        }
        else
        {
            GenerateExpression(expr.Target);
            GenerateExpression(expr.Value);

            var op = expr.Operator switch
            {
                "+=" => OpCode.AddInt,
                "-=" => OpCode.SubInt,
                "*=" => OpCode.MulInt,
                "/=" => OpCode.DivInt,
                _ => OpCode.AddInt
            };

            Emit(op);
        }

        if (expr.Target is IdentifierNode idExpr)
        {
            if (_localVariables.TryGetValue(idExpr.Name, out var localIdx))
            {
                Emit(OpCode.StoreLocal);
                EmitInt(localIdx);
            }
            else
            {
                Emit(OpCode.StoreGlobal);
                EmitInt(AddConstant(idExpr.Name));
            }
        }
        else if (expr.Target is MemberAccessExpr memberExpr)
        {
            GenerateExpression(memberExpr.Object);
            Emit(OpCode.SetField);
            EmitInt(AddConstant(memberExpr.MemberName));
        }
    }

    private void GenerateLambdaExpr(LambdaExpr expr)
    {
        Emit(OpCode.PushInt32);
        EmitInt(AddConstant("lambda"));
    }

    private void GenerateQueryExpr(QueryExpr expr)
    {
        switch (expr.Kind)
        {
            case QueryKind.All:
                Emit(OpCode.QueryAll);
                break;
            case QueryKind.Any:
                Emit(OpCode.QueryAny);
                break;
            case QueryKind.None:
                Emit(OpCode.QueryAll);
                break;
        }

        EmitInt(expr.ComponentTypes.Count);

        foreach (var compType in expr.ComponentTypes)
        {
            EmitInt(AddConstant(compType.Name));
        }
    }

    private void GenerateMetaBlock(MetaBlock expr)
    {
        Emit(OpCode.Nop);
    }

    #endregion

    #region Private Methods - Emit Helpers

    private void Emit(OpCode op)
    {
        _instructions.Add((byte)op);
    }

    private void EmitByte(byte value)
    {
        _instructions.Add(value);
    }

    private void EmitInt(int value)
    {
        var bytes = BitConverter.GetBytes(value);
        _instructions.AddRange(bytes);
    }

    private void EmitFloat(float value)
    {
        var bytes = BitConverter.GetBytes(value);
        _instructions.AddRange(bytes);
    }

    private void PatchJump(int offset, int target)
    {
        var bytes = BitConverter.GetBytes(target);
        _instructions[offset] = bytes[0];
        _instructions[offset + 1] = bytes[1];
        _instructions[offset + 2] = bytes[2];
        _instructions[offset + 3] = bytes[3];
    }

    private int AddConstant(object value)
    {
        var key = value.ToString() ?? "";

        if (_constants.TryGetValue(key, out var idx))
        {
            return idx;
        }

        idx = _constantPool.Count;
        _constantPool.Add(value);
        _constants[key] = idx;
        return idx;
    }

    private int GetOrAddLocal(string name)
    {
        if (_localVariables.TryGetValue(name, out var idx))
        {
            return idx;
        }

        idx = _localCount++;
        _localVariables[name] = idx;
        return idx;
    }

    private static bool NeedsPop(AstNode expr)
    {
        return expr.Type != NodeType.AssignmentExpr;
    }

    #endregion

    #region Private Methods - Serialization

    private byte[] SerializeModule(BytecodeModule module)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(MagicNumber);
        writer.Write(CurrentVersion);

        var nameBytes = Encoding.UTF8.GetBytes(module.ModuleName);
        writer.Write((ushort)nameBytes.Length);
        writer.Write(nameBytes);

        writer.Write(_constantPool.Count);
        foreach (var constant in _constantPool)
        {
            switch (constant)
            {
                case string s:
                    writer.Write((byte)0x01);
                    writer.Write(s);
                    break;
                case int i:
                    writer.Write((byte)0x02);
                    writer.Write(i);
                    break;
                case float f:
                    writer.Write((byte)0x03);
                    writer.Write(f);
                    break;
                default:
                    writer.Write((byte)0x01);
                    writer.Write(constant.ToString() ?? "");
                    break;
            }
        }

        writer.Write((ushort)_importedSymbols.Count);
        foreach (var symbol in _importedSymbols)
        {
            writer.Write(symbol);
        }

        writer.Write((ushort)_exportedSymbols.Count);
        foreach (var (name, _) in _exportedSymbols)
        {
            writer.Write(name);
        }

        writer.Write((ushort)module.Dependencies.Count);
        foreach (var dep in module.Dependencies)
        {
            writer.Write(dep);
        }

        writer.Write(module.Instructions.Length);
        writer.Write(module.Instructions);

        return ms.ToArray();
    }

    private static string ExtractModuleName(CompilationUnit unit)
    {
        if (!string.IsNullOrEmpty(unit.FilePath))
        {
            return Path.GetFileNameWithoutExtension(unit.FilePath);
        }

        return "main";
    }

    #endregion

    #region Private Methods - VM Source Generation

    private string GenerateVmSource(BytecodeModule module)
    {
        var sb = new StringBuilder();

        sb.AppendLine("#include <stdint.h>");
        sb.AppendLine("#include <stdlib.h>");
        sb.AppendLine("#include <string.h>");
        sb.AppendLine();

        sb.AppendLine("typedef struct {");
        sb.AppendLine("    int32_t ip;");
        sb.AppendLine("    int32_t sp;");
        sb.AppendLine("    int64_t stack[1024];");
        sb.AppendLine("    const uint8_t* code;");
        sb.AppendLine("    int32_t code_size;");
        sb.AppendLine("} VMState;");
        sb.AppendLine();

        sb.AppendLine("int32_t vm_run(VMState* vm) {");
        sb.AppendLine("    while (vm->ip < vm->code_size) {");
        sb.AppendLine("        uint8_t opcode = vm->code[vm->ip++];");
        sb.AppendLine("        switch (opcode) {");

        var usedOpCodes = AnalyzeUsedOpCodes(module.Instructions);

        foreach (var op in usedOpCodes)
        {
            sb.AppendLine($"            case 0x{(byte)op:X2}: /* {op} */");
            sb.AppendLine(GenerateOpCodeHandler(op));
        }

        sb.AppendLine("            default: return -1;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("    return 0;");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static HashSet<OpCode> AnalyzeUsedOpCodes(byte[] instructions)
    {
        var used = new HashSet<OpCode>();

        for (var i = 0; i < instructions.Length; i++)
        {
            if (Enum.IsDefined(typeof(OpCode), instructions[i]))
            {
                used.Add((OpCode)instructions[i]);
            }
        }

        return used;
    }

    private static string GenerateOpCodeHandler(OpCode op)
    {
        return op switch
        {
            OpCode.Halt => "                return 0;\n",
            OpCode.Nop => "                break;\n",
            OpCode.PushInt8 => "                { int8_t v = vm->code[vm->ip++]; vm->stack[vm->sp++] = v; break; }\n",
            OpCode.PushInt32 => "                { int32_t v = *(int32_t*)(vm->code + vm->ip); vm->ip += 4; vm->stack[vm->sp++] = v; break; }\n",
            OpCode.PushFloat32 => "                { float v = *(float*)(vm->code + vm->ip); vm->ip += 4; vm->stack[vm->sp++] = *(int32_t*)&v; break; }\n",
            OpCode.Pop => "                vm->sp--; break;\n",
            OpCode.Dup => "                vm->stack[vm->sp] = vm->stack[vm->sp-1]; vm->sp++; break;\n",
            OpCode.AddInt => "                { int64_t b = vm->stack[--vm->sp]; int64_t a = vm->stack[--vm->sp]; vm->stack[vm->sp++] = a + b; break; }\n",
            OpCode.SubInt => "                { int64_t b = vm->stack[--vm->sp]; int64_t a = vm->stack[--vm->sp]; vm->stack[vm->sp++] = a - b; break; }\n",
            OpCode.MulInt => "                { int64_t b = vm->stack[--vm->sp]; int64_t a = vm->stack[--vm->sp]; vm->stack[vm->sp++] = a * b; break; }\n",
            OpCode.DivInt => "                { int64_t b = vm->stack[--vm->sp]; int64_t a = vm->stack[--vm->sp]; vm->stack[vm->sp++] = a / b; break; }\n",
            OpCode.AddFloat => "                { double b = *(double*)&vm->stack[--vm->sp]; double a = *(double*)&vm->stack[--vm->sp]; double r = a + b; vm->stack[vm->sp++] = *(int64_t*)&r; break; }\n",
            OpCode.SubFloat => "                { double b = *(double*)&vm->stack[--vm->sp]; double a = *(double*)&vm->stack[--vm->sp]; double r = a - b; vm->stack[vm->sp++] = *(int64_t*)&r; break; }\n",
            OpCode.MulFloat => "                { double b = *(double*)&vm->stack[--vm->sp]; double a = *(double*)&vm->stack[--vm->sp]; double r = a * b; vm->stack[vm->sp++] = *(int64_t*)&r; break; }\n",
            OpCode.DivFloat => "                { double b = *(double*)&vm->stack[--vm->sp]; double a = *(double*)&vm->stack[--vm->sp]; double r = a / b; vm->stack[vm->sp++] = *(int64_t*)&r; break; }\n",
            OpCode.NegInt => "                vm->stack[vm->sp-1] = -vm->stack[vm->sp-1]; break;\n",
            OpCode.Jump => "                { int32_t target = *(int32_t*)(vm->code + vm->ip); vm->ip = target; break; }\n",
            OpCode.JumpIfTrue => "                { int32_t target = *(int32_t*)(vm->code + vm->ip); vm->ip += 4; if (vm->stack[--vm->sp]) vm->ip = target; break; }\n",
            OpCode.JumpIfFalse => "                { int32_t target = *(int32_t*)(vm->code + vm->ip); vm->ip += 4; if (!vm->stack[--vm->sp]) vm->ip = target; break; }\n",
            OpCode.Call => "                { int32_t addr = *(int32_t*)(vm->code + vm->ip); vm->ip += 4; vm->stack[vm->sp++] = vm->ip; vm->ip = addr; break; }\n",
            OpCode.CallNative => "                vm->ip += 4; break;\n",
            OpCode.Return => "                { vm->ip = (int32_t)vm->stack[--vm->sp]; break; }\n",
            OpCode.LoadLocal => "                { int32_t idx = *(int32_t*)(vm->code + vm->ip); vm->ip += 4; vm->stack[vm->sp++] = vm->stack[idx]; break; }\n",
            OpCode.StoreLocal => "                { int32_t idx = *(int32_t*)(vm->code + vm->ip); vm->ip += 4; vm->stack[idx] = vm->stack[--vm->sp]; break; }\n",
            OpCode.LoadGlobal => "                vm->ip += 4; break;\n",
            OpCode.StoreGlobal => "                vm->ip += 4; break;\n",
            OpCode.SpawnEntity => "                vm->stack[vm->sp++] = 0; break;\n",
            OpCode.DestroyEntity => "                vm->sp--; break;\n",
            OpCode.AddComponent => "                vm->ip += 4; break;\n",
            OpCode.GetComponent => "                vm->ip += 4; vm->stack[vm->sp++] = 0; break;\n",
            OpCode.RemoveComponent => "                vm->ip += 4; break;\n",
            OpCode.QueryAll => "                vm->ip += 4; vm->stack[vm->sp++] = 0; break;\n",
            OpCode.QueryAny => "                vm->ip += 4; vm->stack[vm->sp++] = 0; break;\n",
            _ => "                break;\n"
        };
    }

    #endregion
}

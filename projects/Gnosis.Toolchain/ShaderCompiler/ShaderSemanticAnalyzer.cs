using Gnosis.Toolchain.ScriptCompiler;
using Oak.Core.Diagnostics;
using Oak.GGScript.AST;
using Gnosis.Toolchain.ScriptCompiler.ScriptFrontend;

namespace Gnosis.Toolchain.ShaderCompiler;

public sealed record ShaderTypeInfo(
    string Name,
    int VectorSize,
    int MatrixRows,
    int MatrixCols);

public class ShaderSemanticAnalyzer : BaseSemanticAnalyzer
{
    #region Fields

    private readonly Dictionary<string, ComponentDecl> _cbuffers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StructDecl> _structs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UniformBindingDecl> _uniformBindings = new(StringComparer.Ordinal);

    private static readonly HashSet<string> ValidSwizzleSets = new(StringComparer.Ordinal)
    {
        "xyzw", "rgba", "stpq"
    };

    private static readonly HashSet<string> ShaderBuiltins = new(StringComparer.Ordinal)
    {
        "textureSample", "textureLoad", "textureStore",
        "dot", "cross", "normalize", "length", "distance", "reflect", "refract",
        "clamp", "mix", "step", "smoothstep", "min", "max", "abs", "sqrt", "pow",
        "trace_ray", "acceleration_structure"
    };

    private static readonly HashSet<string> ShaderScalarTypes = new(StringComparer.Ordinal)
    {
        "f32", "i32", "u32", "bool", "f64"
    };

    #endregion

    #region Constructors

    public ShaderSemanticAnalyzer(DiagnosticSink diagnostics) : base(diagnostics)
    {
    }

    #endregion

    #region Public Methods

    public static ShaderTypeInfo? ResolveShaderType(TypeAnnotation? type)
    {
        if (type is null)
        {
            return null;
        }

        var name = type.Name;

        if (ShaderScalarTypes.Contains(name))
        {
            return new ShaderTypeInfo(name, 0, 0, 0);
        }

        if (name is "vec2" or "vec3" or "vec4")
        {
            var size = name[^1] - '0';
            return new ShaderTypeInfo(name, size, 0, 0);
        }

        if (name is "mat2" or "mat3" or "mat4")
        {
            var dim = name[^1] - '0';
            return new ShaderTypeInfo(name, -1, dim, dim);
        }

        return null;
    }

    #endregion

    #region Protected Methods

    protected override void BuildSymbolTable(CompilationUnit unit)
    {
        foreach (var decl in unit.Declarations)
        {
            switch (decl.Type)
            {
                case NodeType.FunctionDecl:
                    var func = (FunctionDecl)decl;
                    if (_functions.ContainsKey(func.Name))
                    {
                        _diagnostics.AddError(
                            string.Empty,
                            func.Span,
                            "GG5015",
                            $"重复的函数定义: {func.Name}");
                    }
                    else
                    {
                        _functions[func.Name] = func;
                    }
                    break;

                case NodeType.ComponentDecl:
                    var comp = (ComponentDecl)decl;
                    if (_cbuffers.ContainsKey(comp.Name))
                    {
                        _diagnostics.AddError(
                            string.Empty,
                            comp.Span,
                            "GG5016",
                            $"重复的 cbuffer 定义: {comp.Name}");
                    }
                    else
                    {
                        _cbuffers[comp.Name] = comp;
                    }
                    break;

                case NodeType.StructDecl:
                    var structDecl = (StructDecl)decl;
                    if (_structs.ContainsKey(structDecl.Name))
                    {
                        _diagnostics.AddError(
                            string.Empty,
                            structDecl.Span,
                            "GG5017",
                            $"重复的结构体定义: {structDecl.Name}");
                    }
                    else
                    {
                        _structs[structDecl.Name] = structDecl;
                    }
                    break;

                case NodeType.UniformBindingDecl:
                    var uniform = (UniformBindingDecl)decl;
                    _uniformBindings[uniform.Name] = uniform;
                    break;
            }
        }
    }

    protected override void ValidateDeclarations(CompilationUnit unit)
    {
        DetectUniformBindingConflicts(unit);

        foreach (var decl in unit.Declarations)
        {
            ValidateDeclaration(decl);
        }
    }

    protected override bool IsKnownIdentifier(string name)
    {
        return base.IsKnownIdentifier(name) ||
               _cbuffers.ContainsKey(name) ||
               _structs.ContainsKey(name) ||
               ShaderBuiltins.Contains(name);
    }

    protected new void ValidateExpression(AstNode expr)
    {
        switch (expr.Type)
        {
            case NodeType.IdentifierExpr:
                var idExpr = (IdentifierNode)expr;
                if (!IsVariableInScope(idExpr.Name) && !IsKnownIdentifier(idExpr.Name))
                {
                    _diagnostics.AddWarning(
                        string.Empty,
                        idExpr.Span,
                        "GG5018",
                        $"未定义的标识符: {idExpr.Name}");
                }
                break;

            case NodeType.BinaryExpr:
                var binExpr = (BinaryExpr)expr;
                ValidateExpression(binExpr.Left);
                ValidateExpression(binExpr.Right);
                ValidateBinaryTypeCompatibility(binExpr);
                break;

            case NodeType.UnaryExpr:
                ValidateExpression(((TermUnaryExpression)expr).Operand);
                break;

            case NodeType.CallExpr:
                var callExpr = (TermCallExpression)expr;
                ValidateExpression(callExpr.Callee);
                foreach (var arg in callExpr.Arguments)
                {
                    ValidateExpression(arg);
                }
                break;

            case NodeType.MemberAccessExpr:
                var memberExpr = (MemberAccessExpr)expr;
                ValidateExpression(memberExpr.Target);
                ValidateSwizzleAccess(memberExpr);
                break;

            case NodeType.SwizzleExpr:
                var swizzleExpr = (SwizzleExpr)expr;
                ValidateExpression(swizzleExpr.Target);
                ValidateSwizzleComponents(swizzleExpr);
                break;

            case NodeType.IndexExpr:
                var indexExpr = (TermIndexExpression)expr;
                ValidateExpression(indexExpr.Target);
                ValidateExpression(indexExpr.Index);
                break;

            case NodeType.AssignmentExpr:
                var assignExpr = (AssignmentExpr)expr;
                ValidateExpression(assignExpr.Target);
                ValidateExpression(assignExpr.Value);
                break;

            case NodeType.LambdaExpr:
                var lambdaExpr = (LambdaExpr)expr;
                PushScope();
                foreach (var param in lambdaExpr.Parameters)
                {
                    AddVariable(param.Name, param.ParamType);
                }
                ValidateExpression(lambdaExpr.Body);
                PopScope();
                break;
        }
    }

    protected new void ValidateBlockStmt(BlockStmt block)
    {
        PushScope();

        foreach (var stmt in block.Statements)
        {
            ValidateStatement(stmt);
        }

        PopScope();
    }

    protected new void ValidateStatement(AstNode stmt)
    {
        switch (stmt.Type)
        {
            case NodeType.VariableDecl:
                var varDecl = (VariableDecl)stmt;
                AddVariable(varDecl.Name, varDecl.VarType);
                if (varDecl.Initializer is not null)
                {
                    ValidateExpression(varDecl.Initializer);
                }
                break;

            case NodeType.IfStmt:
                var ifStmt = (IfStatement)stmt;
                ValidateExpression(ifStmt.Condition);
                ValidateStatement(ifStmt.ThenBlock);
                if (ifStmt.ElseBlock is not null)
                {
                    ValidateStatement(ifStmt.ElseBlock);
                }
                break;

            case NodeType.LoopStmt:
                var loopStmt = (LoopStmt)stmt;
                if (loopStmt.Iterable is not null)
                {
                    ValidateExpression(loopStmt.Iterable);
                }
                ValidateBlockStmt(loopStmt.Body);
                break;

            case NodeType.WhileStmt:
                var whileStmt = (WhileStmt)stmt;
                ValidateExpression(whileStmt.Condition);
                ValidateBlockStmt(whileStmt.Body);
                break;

            case NodeType.ForStmt:
                var forStmt = (ForStmt)stmt;
                if (forStmt.Initializer is not null)
                {
                    ValidateStatement(forStmt.Initializer);
                }
                if (forStmt.Condition is not null)
                {
                    ValidateExpression(forStmt.Condition);
                }
                if (forStmt.Update is not null)
                {
                    ValidateExpression(forStmt.Update);
                }
                ValidateBlockStmt(forStmt.Body);
                break;

            case NodeType.ReturnStmt:
                var returnStmt = (ReturnStatement)stmt;
                if (returnStmt.Value is not null)
                {
                    ValidateExpression(returnStmt.Value);
                }
                break;

            case NodeType.DiscardStmt:
                break;

            case NodeType.ExprStmt:
                ValidateExpression(((TermExpressionStatement)stmt).Expression);
                break;

            case NodeType.BlockStmt:
                ValidateBlockStmt((BlockStmt)stmt);
                break;
        }
    }

    #endregion

    #region Private Methods

    private void ValidateDeclaration(AstNode decl)
    {
        switch (decl.Type)
        {
            case NodeType.FunctionDecl:
                ValidateShaderFunctionDecl((FunctionDecl)decl);
                break;

            case NodeType.ComponentDecl:
                ValidateCbufferDecl((ComponentDecl)decl);
                break;

            case NodeType.StructDecl:
                ValidateStructDecl((StructDecl)decl);
                break;
        }
    }

    private void ValidateShaderFunctionDecl(FunctionDecl func)
    {
        foreach (var attr in func.Attributes)
        {
            switch (attr.Name)
            {
                case "Vertex":
                    ValidateVertexEntryPoint(func);
                    break;

                case "Fragment":
                    ValidateFragmentEntryPoint(func);
                    break;

                case "Compute":
                    ValidateComputeEntryPoint(func);
                    break;

                case "RayGen":
                    ValidateRayGenEntryPoint(func);
                    break;

                case "ClosestHit":
                    ValidateClosestHitEntryPoint(func);
                    break;

                case "Miss":
                    break;

                case "External":
                    ValidateExternalFunction(func);
                    break;
            }
        }

        var isExternal = func.Attributes.Any(a => a.Name == "External");
        if (!isExternal)
        {
            ValidateFunctionBody(func);
        }
    }

    private void ValidateVertexEntryPoint(FunctionDecl func)
    {
        if (func.ReturnType is null)
        {
            _diagnostics.AddError(
                string.Empty,
                func.Span,
                "GG5001",
                "顶点着色器入口必须有返回类型",
                "返回类型应为 vec4<f32> 或结构体");
        }
        else
        {
            var returnType = ResolveShaderType(func.ReturnType);
            if (returnType is null || (returnType.Name != "vec4" && !_structs.ContainsKey(func.ReturnType.Name)))
            {
                _diagnostics.AddError(
                    string.Empty,
                    func.Span,
                    "GG5001",
                    $"顶点着色器入口返回类型无效: {func.ReturnType.Name}",
                    "返回类型应为 vec4<f32> 或结构体");
            }
        }

        if (func.Parameters.Count == 0)
        {
            _diagnostics.AddError(
                string.Empty,
                func.Span,
                "GG5002",
                "顶点着色器入口必须有输入参数",
                "添加顶点输入参数");
        }
    }

    private void ValidateFragmentEntryPoint(FunctionDecl func)
    {
        if (func.ReturnType is null || func.ReturnType.Name != "vec4")
        {
            _diagnostics.AddError(
                string.Empty,
                func.Span,
                "GG5003",
                "片段着色器入口返回类型必须为 vec4<f32>",
                "将返回类型改为 vec4<f32>");
        }

        if (func.Parameters.Count == 0)
        {
            _diagnostics.AddError(
                string.Empty,
                func.Span,
                "GG5004",
                "片段着色器入口必须有输入参数",
                "添加片段输入参数");
        }
    }

    private void ValidateComputeEntryPoint(FunctionDecl func)
    {
        if (!func.Attributes.Any(a => a.Name == "WorkgroupSize"))
        {
            _diagnostics.AddError(
                string.Empty,
                func.Span,
                "GG5005",
                "计算着色器入口必须有 [WorkgroupSize] GGShader 特性标注",
                "添加 [WorkgroupSize] GGShader 特性标注");
        }

        var hasGlobalInvocationId = func.Parameters.Any(p =>
            p.Attributes.Any(a =>
                a.Name == "Builtin" &&
                a.Arguments.Any(kv =>
                    kv.Value == "global_invocation_id" ||
                    kv.Key == "global_invocation_id")));

        if (!hasGlobalInvocationId)
        {
            _diagnostics.AddError(
                string.Empty,
                func.Span,
                "GG5006",
                "计算着色器入口必须有 [Builtin(global_invocation_id)] 参数",
                "添加带有 [Builtin(global_invocation_id)] GGShader 特性标注的参数");
        }
    }

    private void ValidateRayGenEntryPoint(FunctionDecl func)
    {
        if (func.Parameters.Count == 0)
        {
            _diagnostics.AddError(
                string.Empty,
                func.Span,
                "GG5007",
                "光线生成着色器入口必须有启动配置参数",
                "添加启动配置参数");
        }
    }

    private void ValidateClosestHitEntryPoint(FunctionDecl func)
    {
        var hasHitAttr = func.Parameters.Any(p =>
            p.Attributes.Any(a => a.Name == "HitAttr"));

        if (!hasHitAttr)
        {
            _diagnostics.AddError(
                string.Empty,
                func.Span,
                "GG5008",
                "最近命中着色器入口必须有 [HitAttr] 参数",
                "添加带有 [HitAttr] GGShader 特性标注的参数");
        }
    }

    private void ValidateExternalFunction(FunctionDecl func)
    {
        if (func.Body is not null)
        {
            _diagnostics.AddError(
                string.Empty,
                func.Span,
                "GG5009",
                "外部函数不能有函数体",
                "移除函数体");
        }

        foreach (var param in func.Parameters)
        {
            if (!IsShaderValidType(param.ParamType))
            {
                _diagnostics.AddError(
                    string.Empty,
                    param.Span,
                    "GG5010",
                    $"外部函数参数类型无效: {param.ParamType.Name}",
                    "参数类型必须为着色器有效类型（标量、向量、矩阵、纹理、采样器或结构体）");
            }
        }

        if (func.ReturnType is not null && !IsShaderValidType(func.ReturnType))
        {
            _diagnostics.AddError(
                string.Empty,
                func.Span,
                "GG5011",
                $"外部函数返回类型无效: {func.ReturnType.Name}",
                "返回类型必须为着色器有效类型（标量、向量、矩阵、纹理、采样器或结构体）");
        }
    }

    private void ValidateFunctionBody(FunctionDecl func)
    {
        if (func.Body is null)
        {
            return;
        }

        PushScope();

        foreach (var param in func.Parameters)
        {
            AddVariable(param.Name, param.ParamType);
        }

        ValidateBlockStmt(func.Body);

        PopScope();
    }

    private void ValidateCbufferDecl(ComponentDecl decl)
    {
        foreach (var field in decl.Fields)
        {
            var fieldType = ResolveShaderType(field.FieldType);
            if (fieldType is null && !_structs.ContainsKey(field.FieldType.Name))
            {
                _diagnostics.AddWarning(
                    string.Empty,
                    field.Span,
                    "GG5021",
                    $"cbuffer 字段类型可能不是着色器有效类型: {field.FieldType.Name}");
            }
        }
    }

    private void ValidateStructDecl(StructDecl decl)
    {
        foreach (var field in decl.Fields)
        {
            var fieldType = ResolveShaderType(field.FieldType);
            if (fieldType is null && !_structs.ContainsKey(field.FieldType.Name))
            {
                _diagnostics.AddWarning(
                    string.Empty,
                    field.Span,
                    "GG5022",
                    $"结构体字段类型可能不是着色器有效类型: {field.FieldType.Name}");
            }
        }
    }

    private void ValidateBinaryTypeCompatibility(BinaryExpr expr)
    {
        var leftType = ResolveExpressionType(expr.Left);
        var rightType = ResolveExpressionType(expr.Right);

        if (leftType is null || rightType is null)
        {
            return;
        }

        if (IsScalar(leftType) && IsScalar(rightType))
        {
            return;
        }

        if (IsScalar(leftType) && IsVector(rightType))
        {
            return;
        }

        if (IsVector(leftType) && IsScalar(rightType))
        {
            return;
        }

        if (IsVector(leftType) && IsVector(rightType))
        {
            if (leftType.VectorSize == rightType.VectorSize)
            {
                return;
            }

            _diagnostics.AddError(
                string.Empty,
                expr.Span,
                "GG5012",
                $"向量大小不匹配: {leftType.Name} 与 {rightType.Name} 无法进行二元运算",
                $"向量大小必须相同，当前为 {leftType.VectorSize} 与 {rightType.VectorSize}");
            return;
        }

        if (IsMatrix(leftType) && IsVector(rightType))
        {
            if (leftType.MatrixCols == rightType.VectorSize)
            {
                return;
            }

            _diagnostics.AddError(
                string.Empty,
                expr.Span,
                "GG5012",
                $"矩阵与向量维度不匹配: {leftType.Name} 的列数 {leftType.MatrixCols} 与 {rightType.Name} 的大小 {rightType.VectorSize} 不一致");
            return;
        }

        if (IsVector(leftType) && IsMatrix(rightType))
        {
            if (leftType.VectorSize == rightType.MatrixRows)
            {
                return;
            }

            _diagnostics.AddError(
                string.Empty,
                expr.Span,
                "GG5012",
                $"向量与矩阵维度不匹配: {leftType.Name} 的大小 {leftType.VectorSize} 与 {rightType.Name} 的行数 {rightType.MatrixRows} 不一致");
            return;
        }

        if (IsMatrix(leftType) && IsMatrix(rightType))
        {
            if (leftType.MatrixCols == rightType.MatrixRows)
            {
                return;
            }

            _diagnostics.AddError(
                string.Empty,
                expr.Span,
                "GG5012",
                $"矩阵维度不匹配: 左矩阵列数 {leftType.MatrixCols} 与右矩阵行数 {rightType.MatrixRows} 不一致");
            return;
        }
    }

    private void ValidateSwizzleAccess(MemberAccessExpr expr)
    {
        var objectType = ResolveExpressionType(expr.Target);
        if (objectType is null || !IsVector(objectType))
        {
            return;
        }

        if (!IsValidSwizzle(expr.MemberName))
        {
            _diagnostics.AddError(
                string.Empty,
                expr.Span,
                "GG5013",
                $"无效的 swizzle 访问: .{expr.MemberName}",
                new[] { "有效的 swizzle 集合: xyzw, rgba, stpq，长度 1-4，不可混用集合" });
        }
    }

    private void ValidateSwizzleComponents(SwizzleExpr expr)
    {
        var objectType = ResolveExpressionType(expr.Object);
        if (objectType is null || !IsVector(objectType))
        {
            return;
        }

        if (!IsValidSwizzle(expr.Components))
        {
            _diagnostics.AddError(
                string.Empty,
                expr.Span,
                "GG5019",
                $"无效的 swizzle 分量: .{expr.Components}",
                "有效的 swizzle 集合: xyzw, rgba, stpq，长度 1-4，不可混用集合");
        }
    }

    private void DetectUniformBindingConflicts(CompilationUnit unit)
    {
        var bindingMap = new Dictionary<(int Binding, int Set), string>();

        foreach (var decl in unit.Declarations)
        {
            if (decl.Type == NodeType.ComponentDecl)
            {
                var comp = (ComponentDecl)decl;
                var bindingInfo = ExtractBindingInfo(comp);
                if (bindingInfo is null)
                {
                    continue;
                }

                var key = (bindingInfo.Value.Binding, bindingInfo.Value.Set);
                if (bindingMap.TryGetValue(key, out var existingName))
                {
                    _diagnostics.AddError(
                        string.Empty,
                        comp.Span,
                        "GG5014",
                        $"Uniform 绑定冲突: (binding={key.Item1}, set={key.Item2}) 已被 '{existingName}' 使用",
                        $"修改 '{comp.Name}' 的绑定或集合以避免冲突");
                }
                else
                {
                    bindingMap[key] = comp.Name;
                }
            }
            else if (decl.Type == NodeType.UniformBindingDecl)
            {
                var uniform = (UniformBindingDecl)decl;
                if (uniform.Binding is null || uniform.Group is null)
                {
                    continue;
                }

                (int Binding, int Set) key = (uniform.Binding.Value, uniform.Group.Value);
                if (bindingMap.TryGetValue(key, out var existingName))
                {
                    _diagnostics.AddError(
                        string.Empty,
                        uniform.Span,
                        "GG5014",
                        $"Uniform 绑定冲突: (binding={key.Item1}, set={key.Item2}) 已被 '{existingName}' 使用",
                        $"修改 '{uniform.Name}' 的绑定或集合以避免冲突");
                }
                else
                {
                    bindingMap[key] = uniform.Name;
                }
            }
        }
    }

    private ShaderTypeInfo? ResolveExpressionType(AstNode expr)
    {
        switch (expr.Type)
        {
            case NodeType.IdentifierExpr:
                var idExpr = (IdentifierNode)expr;
                foreach (var scope in _scopes)
                {
                    if (scope.TryGetValue(idExpr.Name, out var type) && type is not null)
                    {
                        return ResolveShaderType(type);
                    }
                }
                if (_functions.TryGetValue(idExpr.Name, out var func))
                {
                    return ResolveShaderType(func.ReturnType);
                }
                return null;

            case NodeType.LiteralExpr:
                var litExpr = (LiteralExpr)expr;
                return litExpr.LiteralKind switch
                {
                    LiteralType.Number => new ShaderTypeInfo("f32", 0, 0, 0),
                    LiteralType.Boolean => new ShaderTypeInfo("bool", 0, 0, 0),
                    _ => null
                };

            case NodeType.BinaryExpr:
                var binExpr = (BinaryExpr)expr;
                var leftType = ResolveExpressionType(binExpr.Left);
                var rightType = ResolveExpressionType(binExpr.Right);
                return ResolveBinaryResultType(leftType, rightType, binExpr.Operator);

            case NodeType.CallExpr:
                var callExpr = (TermCallExpression)expr;
                if (callExpr.Callee is IdentifierNode calleeId &&
                    _functions.TryGetValue(calleeId.Name, out var calledFunc))
                {
                    return ResolveShaderType(calledFunc.ReturnType);
                }
                return null;

            case NodeType.MemberAccessExpr:
                var memberExpr = (MemberAccessExpr)expr;
                var objType = ResolveExpressionType(memberExpr.Object);
                if (objType is not null && IsVector(objType))
                {
                    var len = memberExpr.MemberName.Length;
                    if (len == 1)
                    {
                        return new ShaderTypeInfo("f32", 0, 0, 0);
                    }
                    if (len is >= 2 and <= 4 && IsValidSwizzle(memberExpr.MemberName))
                    {
                        return new ShaderTypeInfo($"vec{len}", len, 0, 0);
                    }
                }
                return null;

            case NodeType.SwizzleExpr:
                var swizzleExpr = (SwizzleExpr)expr;
                var swizzleObjType = ResolveExpressionType(swizzleExpr.Object);
                if (swizzleObjType is not null && IsVector(swizzleObjType))
                {
                    var len = swizzleExpr.Components.Length;
                    if (len == 1)
                    {
                        return new ShaderTypeInfo("f32", 0, 0, 0);
                    }
                    if (len is >= 2 and <= 4 && IsValidSwizzle(swizzleExpr.Components))
                    {
                        return new ShaderTypeInfo($"vec{len}", len, 0, 0);
                    }
                }
                return null;

            case NodeType.UnaryExpr:
                return ResolveExpressionType(((TermUnaryExpression)expr).Operand);

            case NodeType.AssignmentExpr:
                return ResolveExpressionType(((AssignmentExpr)expr).Target);

            default:
                return null;
        }
    }

    private static ShaderTypeInfo? ResolveBinaryResultType(
        ShaderTypeInfo? left, ShaderTypeInfo? right, string op)
    {
        if (left is null)
        {
            return right;
        }

        if (right is null)
        {
            return left;
        }

        if (IsScalar(left) && IsScalar(right))
        {
            return left;
        }

        if (IsScalar(left) && IsVector(right))
        {
            return right;
        }

        if (IsVector(left) && IsScalar(right))
        {
            return left;
        }

        if (IsVector(left) && IsVector(right) && left.VectorSize == right.VectorSize)
        {
            return left;
        }

        if (IsMatrix(left) && IsVector(right) && left.MatrixCols == right.VectorSize)
        {
            return new ShaderTypeInfo($"vec{left.MatrixRows}", left.MatrixRows, 0, 0);
        }

        if (IsVector(left) && IsMatrix(right) && left.VectorSize == right.MatrixRows)
        {
            return new ShaderTypeInfo($"vec{right.MatrixCols}", right.MatrixCols, 0, 0);
        }

        if (IsMatrix(left) && IsMatrix(right) && left.MatrixCols == right.MatrixRows)
        {
            return new ShaderTypeInfo($"mat{left.MatrixRows}", -1, left.MatrixRows, right.MatrixCols);
        }

        return left;
    }

    private static bool IsScalar(ShaderTypeInfo type)
    {
        return type is { VectorSize: 0, MatrixRows: 0 };
    }

    private static bool IsVector(ShaderTypeInfo type)
    {
        return type.VectorSize > 0;
    }

    private static bool IsMatrix(ShaderTypeInfo type)
    {
        return type.MatrixRows > 0;
    }

    private static bool IsValidSwizzle(string swizzle)
    {
        if (swizzle.Length is < 1 or > 4)
        {
            return false;
        }

        string? matchedSet = null;

        foreach (var c in swizzle)
        {
            string? charSet = null;

            foreach (var set in ValidSwizzleSets)
            {
                if (set.Contains(c))
                {
                    charSet = set;
                    break;
                }
            }

            if (charSet is null)
            {
                return false;
            }

            if (matchedSet is null)
            {
                matchedSet = charSet;
            }
            else if (matchedSet != charSet)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsShaderValidType(TypeAnnotation type)
    {
        var name = type.Name;

        if (ShaderScalarTypes.Contains(name))
        {
            return true;
        }

        if (name is "vec2" or "vec3" or "vec4")
        {
            return true;
        }

        if (name is "mat2" or "mat3" or "mat4")
        {
            return true;
        }

        if (name is "texture_2d" or "texture_3d" or "texture_cube" or
            "texture_storage_2d" or "sampler" or "sampler_comparison" or
            "acceleration_structure")
        {
            return true;
        }

        if (_structs.ContainsKey(name))
        {
            return true;
        }

        return false;
    }

    private static (int Binding, int Set)? ExtractBindingInfo(ComponentDecl decl)
    {
        var bindingAttr = decl.Attributes.FirstOrDefault(a => a.Name == "Binding");
        if (bindingAttr is null)
        {
            return null;
        }

        int? binding = null;
        int? set = 0;

        foreach (var arg in bindingAttr.Arguments)
        {
            if (arg.Key == "binding" && int.TryParse(arg.Value, out var b))
            {
                binding = b;
            }
            else if (arg.Key == "set" && int.TryParse(arg.Value, out var s))
            {
                set = s;
            }
        }

        if (binding is null && bindingAttr.Arguments.Count > 0)
        {
            if (int.TryParse(bindingAttr.Arguments[0].Value, out var b))
            {
                binding = b;
            }

            if (bindingAttr.Arguments.Count > 1 &&
                int.TryParse(bindingAttr.Arguments[1].Value, out var s))
            {
                set = s;
            }
        }

        if (binding is null)
        {
            return null;
        }

        return (binding.Value, set ?? 0);
    }

    #endregion
}

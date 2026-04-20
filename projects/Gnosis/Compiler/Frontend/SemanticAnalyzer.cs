using Gnosis.Compiler.Diagnostics;
using Gnosis.Compiler.ValueObjects;
using Gnosis.Compiler.ValueObjects.AST;

namespace Gnosis.Compiler.Frontend;

public class SemanticAnalyzer
{
    #region Fields

    private readonly DiagnosticSink _diagnostics;
    private readonly Dictionary<string, ComponentDecl> _components = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SystemDecl> _systems = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FunctionDecl> _functions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, VariableDecl> _variables = new(StringComparer.Ordinal);
    private readonly Dictionary<string, WidgetDecl> _widgets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SceneDecl> _scenes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PluginDecl> _plugins = new(StringComparer.Ordinal);
    private readonly Stack<Dictionary<string, TypeAnnotation?>> _scopes = new();

    #endregion

    #region Constructors

    public SemanticAnalyzer(DiagnosticSink diagnostics)
    {
        _diagnostics = diagnostics;
    }

    #endregion

    #region Public Methods

    public CompilationUnit Analyze(CompilationUnit unit)
    {
        BuildSymbolTable(unit);
        ValidateDeclarations(unit);
        return unit;
    }

    #endregion

    #region Private Methods - Symbol Table

    private void BuildSymbolTable(CompilationUnit unit)
    {
        foreach (var decl in unit.Declarations)
        {
            switch (decl.Type)
            {
                case NodeType.ComponentDecl:
                    var comp = (ComponentDecl)decl;
                    if (_components.ContainsKey(comp.Name))
                    {
                        _diagnostics.AddError(
                            string.Empty,
                            comp.Span,
                            "GG0401",
                            $"重复的组件定义: {comp.Name}",
                            $"移除或重命名重复的组件 '{comp.Name}'");
                    }
                    else
                    {
                        _components[comp.Name] = comp;
                    }
                    break;

                case NodeType.SystemDecl:
                    var sys = (SystemDecl)decl;
                    if (_systems.ContainsKey(sys.Name))
                    {
                        _diagnostics.AddError(
                            string.Empty,
                            sys.Span,
                            "GG0402",
                            $"重复的系统定义: {sys.Name}");
                    }
                    else
                    {
                        _systems[sys.Name] = sys;
                    }
                    break;

                case NodeType.FunctionDecl:
                    var func = (FunctionDecl)decl;
                    if (_functions.ContainsKey(func.Name))
                    {
                        _diagnostics.AddWarning(
                            string.Empty,
                            func.Span,
                            "GG0403",
                            $"重复的函数定义: {func.Name}");
                    }
                    else
                    {
                        _functions[func.Name] = func;
                    }
                    break;

                case NodeType.WidgetDecl:
                    var widget = (WidgetDecl)decl;
                    _widgets[widget.Name] = widget;
                    break;

                case NodeType.SceneDecl:
                    var scene = (SceneDecl)decl;
                    _scenes[scene.Name] = scene;
                    break;

                case NodeType.PluginDecl:
                    var plugin = (PluginDecl)decl;
                    _plugins[plugin.Name] = plugin;
                    break;
            }
        }
    }

    #endregion

    #region Private Methods - Validation

    private void ValidateDeclarations(CompilationUnit unit)
    {
        foreach (var decl in unit.Declarations)
        {
            ValidateDeclaration(decl);
        }
    }

    private void ValidateDeclaration(AstNode decl)
    {
        switch (decl.Type)
        {
            case NodeType.ComponentDecl:
                ValidateComponentDecl((ComponentDecl)decl);
                break;
            case NodeType.SystemDecl:
                ValidateSystemDecl((SystemDecl)decl);
                break;
            case NodeType.FunctionDecl:
                ValidateFunctionDecl((FunctionDecl)decl);
                break;
            case NodeType.SceneDecl:
                ValidateSceneDecl((SceneDecl)decl);
                break;
            case NodeType.PluginDecl:
                ValidatePluginDecl((PluginDecl)decl);
                break;
        }
    }

    private void ValidateComponentDecl(ComponentDecl decl)
    {
        foreach (var attr in decl.Attributes)
        {
            if (attr.Name is not ("Encrypted" or "Replicated"))
            {
                _diagnostics.AddWarning(
                    string.Empty,
                    attr.Span,
                    "GG0410",
                    $"组件不支持属性标注 '{attr.Name}'",
                    "组件支持的属性标注: Encrypted, Replicated");
            }
        }

        foreach (var field in decl.Fields)
        {
            ValidateFieldDecl(field, decl.Name);

            foreach (var attr in field.Attributes)
            {
                if (attr.Name == "Honeypot" && decl.Attributes.All(a => a.Name != "Encrypted"))
                {
                    _diagnostics.AddWarning(
                        string.Empty,
                        attr.Span,
                        "GG0411",
                        $"[Honeypot] 属性标注应在 [Encrypted] 组件内使用",
                        "在组件上添加 [Encrypted] 属性标注");
                }
            }
        }
    }

    private void ValidateFieldDecl(FieldDecl field, string parentName)
    {
        if (field.FieldType.Name == parentName)
        {
            _diagnostics.AddError(
                string.Empty,
                field.Span,
                "GG0412",
                $"字段类型不能与组件名相同: {field.Name}");
        }
    }

    private void ValidateSystemDecl(SystemDecl decl)
    {
        foreach (var query in decl.Queries)
        {
            ValidateQueryExpr(query);
        }

        foreach (var method in decl.LifecycleMethods)
        {
            ValidateLifecycleMethod(method);
        }
    }

    private void ValidateQueryExpr(QueryExpr query)
    {
        foreach (var compType in query.ComponentTypes)
        {
            if (!_components.ContainsKey(compType.Name) &&
                compType.Name != "Entity" &&
                !compType.Name.StartsWith("I"))
            {
                _diagnostics.AddError(
                    string.Empty,
                    compType.Span,
                    "GG0420",
                    $"查询引用了未定义的组件: {compType.Name}",
                    $"定义组件 '{compType.Name}' 或检查拼写");
            }
        }
    }

    private void ValidateLifecycleMethod(FunctionDecl method)
    {
        var validNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "on_load", "on_update", "on_unload",
            "on_server_update", "on_client_update",
            "on_receive_server_state", "on_lockstep_update"
        };

        if (!validNames.Contains(method.Name))
        {
            _diagnostics.AddWarning(
                string.Empty,
                method.Span,
                "GG0421",
                $"未知的生命周期方法: {method.Name}",
                $"有效的生命周期方法: {string.Join(", ", validNames)}");
        }

        if (method.Name == "on_update" && method.Parameters.Count > 1)
        {
            _diagnostics.AddError(
                string.Empty,
                method.Span,
                "GG0422",
                "on_update 方法最多接受一个参数 (delta: f32)");
        }
    }

    private void ValidateFunctionDecl(FunctionDecl decl)
    {
        PushScope();

        foreach (var param in decl.Parameters)
        {
            AddVariable(param.Name, param.ParamType);
        }

        if (decl.Body is not null)
        {
            ValidateBlockStmt(decl.Body);
        }

        PopScope();
    }

    private void ValidateSceneDecl(SceneDecl decl)
    {
        foreach (var method in decl.LifecycleMethods)
        {
            ValidateLifecycleMethod(method);
        }
    }

    private void ValidatePluginDecl(PluginDecl decl)
    {
        foreach (var func in decl.Functions)
        {
            ValidateFunctionDecl(func);
        }
    }

    private void ValidateBlockStmt(BlockStmt block)
    {
        PushScope();

        foreach (var stmt in block.Statements)
        {
            ValidateStatement(stmt);
        }

        PopScope();
    }

    private void ValidateStatement(AstNode stmt)
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
                var ifStmt = (IfStmt)stmt;
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

            case NodeType.ReturnStmt:
                var returnStmt = (ReturnStmt)stmt;
                if (returnStmt.Value is not null)
                {
                    ValidateExpression(returnStmt.Value);
                }
                break;

            case NodeType.ExprStmt:
                ValidateExpression(((ExprStmt)stmt).Expression);
                break;

            case NodeType.BlockStmt:
                ValidateBlockStmt((BlockStmt)stmt);
                break;
        }
    }

    private void ValidateExpression(AstNode expr)
    {
        switch (expr.Type)
        {
            case NodeType.IdentifierExpr:
                var idExpr = (IdentifierExpr)expr;
                if (!IsVariableInScope(idExpr.Name) &&
                    !_functions.ContainsKey(idExpr.Name) &&
                    !_components.ContainsKey(idExpr.Name) &&
                    idExpr.Name != "create_entity" &&
                    idExpr.Name != "destroy_entity" &&
                    idExpr.Name != "Query")
                {
                    _diagnostics.AddWarning(
                        string.Empty,
                        idExpr.Span,
                        "GG0430",
                        $"未定义的标识符: {idExpr.Name}");
                }
                break;

            case NodeType.BinaryExpr:
                var binExpr = (BinaryExpr)expr;
                ValidateExpression(binExpr.Left);
                ValidateExpression(binExpr.Right);
                break;

            case NodeType.UnaryExpr:
                ValidateExpression(((UnaryExpr)expr).Operand);
                break;

            case NodeType.CallExpr:
                var callExpr = (CallExpr)expr;
                ValidateExpression(callExpr.Callee);
                foreach (var arg in callExpr.Arguments)
                {
                    ValidateExpression(arg);
                }
                break;

            case NodeType.MemberAccessExpr:
                var memberExpr = (MemberAccessExpr)expr;
                ValidateExpression(memberExpr.Object);
                break;

            case NodeType.IndexExpr:
                var indexExpr = (IndexExpr)expr;
                ValidateExpression(indexExpr.Object);
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

    #endregion

    #region Private Methods - Scope Management

    private void PushScope()
    {
        _scopes.Push(new Dictionary<string, TypeAnnotation?>(StringComparer.Ordinal));
    }

    private void PopScope()
    {
        if (_scopes.Count > 0)
        {
            _scopes.Pop();
        }
    }

    private void AddVariable(string name, TypeAnnotation? type)
    {
        if (_scopes.Count > 0)
        {
            _scopes.Peek()[name] = type;
        }
    }

    private bool IsVariableInScope(string name)
    {
        foreach (var scope in _scopes)
        {
            if (scope.ContainsKey(name))
            {
                return true;
            }
        }

        return false;
    }

    #endregion
}

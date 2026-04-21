using Gnosis.Compiler.AST;
using Gnosis.Compiler.Diagnostics;

namespace Gnosis.Compiler.ScriptFrontend;

public class SemanticAnalyzer : BaseSemanticAnalyzer
{
    #region Fields

    private readonly Dictionary<string, ComponentDecl> _components = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SystemDecl> _systems = new(StringComparer.Ordinal);
    private readonly Dictionary<string, WidgetDecl> _widgets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SceneDecl> _scenes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PluginDecl> _plugins = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StructDecl> _structs = new(StringComparer.Ordinal);

    #endregion

    #region Constructors

    public SemanticAnalyzer(DiagnosticSink diagnostics) : base(diagnostics)
    {
    }

    #endregion

    #region Protected Methods

    protected override void BuildSymbolTable(CompilationUnit unit)
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

                case NodeType.StructDecl:
                    var structDecl = (StructDecl)decl;
                    if (_structs.ContainsKey(structDecl.Name))
                    {
                        _diagnostics.AddError(
                            string.Empty,
                            structDecl.Span,
                            "GG0450",
                            $"重复的结构体定义: {structDecl.Name}");
                    }
                    else
                    {
                        _structs[structDecl.Name] = structDecl;
                    }
                    break;

                case NodeType.UsingDecl:
                case NodeType.UniformBindingDecl:
                    break;
            }
        }
    }

    protected override void ValidateDeclarations(CompilationUnit unit)
    {
        foreach (var decl in unit.Declarations)
        {
            ValidateDeclaration(decl);
        }
    }

    protected override bool IsKnownIdentifier(string name)
    {
        return base.IsKnownIdentifier(name) ||
               _components.ContainsKey(name) ||
               name == "create_entity" ||
               name == "destroy_entity" ||
               name == "Query";
    }

    #endregion

    #region Private Methods

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
            case NodeType.StructDecl:
                ValidateStructDecl((StructDecl)decl);
                break;
            case NodeType.UniformBindingDecl:
                ValidateUniformBindingDecl((UniformBindingDecl)decl);
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

        if (method is { Name: "on_update", Parameters.Count: > 1 })
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

    private void ValidateStructDecl(StructDecl decl)
    {
        foreach (var field in decl.Fields)
        {
            if (field.FieldType.Name == decl.Name)
            {
                _diagnostics.AddError(
                    string.Empty,
                    field.Span,
                    "GG0451",
                    $"结构体字段类型不能与结构体名相同: {field.Name}");
            }
        }
    }

    private void ValidateUniformBindingDecl(UniformBindingDecl decl)
    {
        if (decl.Group is < 0)
        {
            _diagnostics.AddError(
                string.Empty,
                decl.Span,
                "GG0452",
                $"Group 值不能为负数: {decl.Group.Value}");
        }

        if (decl.Binding is < 0)
        {
            _diagnostics.AddError(
                string.Empty,
                decl.Span,
                "GG0453",
                $"Binding 值不能为负数: {decl.Binding.Value}");
        }
    }

    private static bool IsValidSwizzle(string components)
    {
        if (components.Length is < 1 or > 4)
        {
            return false;
        }

        var isXyzw = components.All(c => c is 'x' or 'y' or 'z' or 'w');
        var isRgba = components.All(c => c is 'r' or 'g' or 'b' or 'a');
        var isStpq = components.All(c => c is 's' or 't' or 'p' or 'q');

        return isXyzw || isRgba || isStpq;
    }

    #endregion
}

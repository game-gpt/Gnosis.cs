using Oak.Diagnostics;
using Oak.GGScript.AST;
using Oak.Widget;

namespace Gnosis.Toolchain.ScriptCompiler.ScriptFrontend;

public partial class WidgetCompiler
{
    private readonly DiagnosticSink _diagnostics;
    private readonly WidgetParser _parser;

    public WidgetCompiler(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new DiagnosticSink();
        _parser = new WidgetParser(_diagnostics);
    }

    public WidgetDecl Compile(string source, string filePath = "")
    {
        var result = _parser.Parse(source, filePath);

        var properties = ConvertProperties(result.Properties);
        var renderMethod = ConvertTemplate(result.TemplateNodes);

        return new WidgetDecl(result.Name, properties, renderMethod);
    }

    private static List<FieldDecl> ConvertProperties(IReadOnlyList<WidgetProperty> widgetProperties)
    {
        var properties = new List<FieldDecl>();

        foreach (var prop in widgetProperties)
        {
            var type = new TypeAnnotation(prop.TypeName, []);
            AstNode? defaultValue = ConvertValueKind(prop.DefaultValue, prop.DefaultValueKind);

            var attrs = new List<AttributeDecl>();
            if (prop.IsReadonly)
            {
                attrs.Add(new AttributeDecl("Readonly", []));
            }

            properties.Add(new FieldDecl(prop.Name, type, defaultValue, attrs));
        }

        return properties;
    }

    private static AstNode? ConvertValueKind(string? value, WidgetValueKind kind)
    {
        if (value is null || kind == WidgetValueKind.None)
        {
            return null;
        }

        return kind switch
        {
            WidgetValueKind.Boolean => new LiteralExpr(LiteralType.Boolean, value == "true"),
            WidgetValueKind.Number => new LiteralExpr(LiteralType.Number, value),
            WidgetValueKind.String => new LiteralExpr(LiteralType.String, value),
            WidgetValueKind.Identifier => new IdentifierNode(value),
            WidgetValueKind.Array => new IdentifierNode(value),
            WidgetValueKind.Object => new IdentifierNode(value),
            _ => new IdentifierNode(value)
        };
    }

    private static FunctionDecl? ConvertTemplate(IReadOnlyList<WidgetTemplateNode> nodes)
    {
        if (nodes.Count == 0)
        {
            return null;
        }

        var statements = new List<AstNode>();
        ConvertTemplateNodes(nodes, statements);

        var body = new BlockStmt(statements);
        return new FunctionDecl("render", [], null, body, []);
    }

    private static void ConvertTemplateNodes(IReadOnlyList<WidgetTemplateNode> nodes, List<AstNode> statements)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case WidgetTextNode textNode:
                    statements.Add(new TermExpressionStatement(new LiteralExpr(LiteralType.String, textNode.Text)));
                    break;

                case WidgetElementNode elementNode:
                    var widgetCall = CreateWidgetCall(elementNode.TagName, elementNode.Attributes);
                    statements.Add(new TermExpressionStatement(widgetCall));

                    if (elementNode.Children.Count > 0)
                    {
                        ConvertTemplateNodes(elementNode.Children, statements);
                    }
                    break;

                case WidgetIfNode ifNode:
                    var ifBody = ConvertToBlock(ifNode.Children);
                    statements.Add(new IfStatement(new IdentifierNode(ifNode.Condition), ifBody, null));
                    break;

                case WidgetForNode forNode:
                    var forBody = ConvertToBlock(forNode.Children);
                    statements.Add(new LoopStmt(forNode.Iterator, new IdentifierNode(forNode.Iterable), forBody));
                    break;
            }
        }
    }

    private static BlockStmt ConvertToBlock(IReadOnlyList<WidgetTemplateNode> children)
    {
        var statements = new List<AstNode>();
        ConvertTemplateNodes(children, statements);
        return new BlockStmt(statements);
    }

    private static TermCallExpression CreateWidgetCall(string tagName, IReadOnlyDictionary<string, string> attributes)
    {
        var args = new List<AstNode>();

        foreach (var (key, value) in attributes)
        {
            var propKey = key.TrimStart(':', '@');
            args.Add(new BinaryExpr(
                new IdentifierNode(propKey),
                "=",
                ParseTemplateValue(value)));
        }

        return new TermCallExpression(new IdentifierNode($"widget_{tagName}"), args);
    }

    private static AstNode ParseTemplateValue(string value)
    {
        if (value.StartsWith("{{") && value.EndsWith("}}"))
        {
            var expr = value[2..^2].Trim();
            return new IdentifierNode(expr);
        }

        if (value.StartsWith("\"") && value.EndsWith("\""))
        {
            return new LiteralExpr(LiteralType.String, value[1..^1]);
        }

        if (value.StartsWith("'") && value.EndsWith("'"))
        {
            return new LiteralExpr(LiteralType.String, value[1..^1]);
        }

        return new IdentifierNode(value);
    }
}

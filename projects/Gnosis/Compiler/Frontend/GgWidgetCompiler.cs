using System.Text;
using System.Text.RegularExpressions;
using Gnosis.Compiler.Diagnostics;
using Gnosis.Compiler.ValueObjects;
using Gnosis.Compiler.ValueObjects.AST;

namespace Gnosis.Compiler.Frontend;

public partial class GgWidgetCompiler
{
    #region Fields

    private readonly DiagnosticSink _diagnostics;

    #endregion

    #region Constructors

    public GgWidgetCompiler(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new DiagnosticSink();
    }

    #endregion

    #region Public Methods

    public WidgetDecl Compile(string source, string filePath = "")
    {
        var scriptBlock = ExtractBlock(source, "script");
        var templateBlock = ExtractBlock(source, "template");
        var styleBlock = ExtractBlock(source, "style");

        var properties = ParseScriptBlock(scriptBlock);
        var renderMethod = ParseTemplateBlock(templateBlock);
        var styles = ParseStyleBlock(styleBlock);

        var widgetName = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrEmpty(widgetName))
        {
            widgetName = "AnonymousWidget";
        }

        return new WidgetDecl(null, widgetName, properties, renderMethod);
    }

    #endregion

    #region Private Methods - Block Extraction

    private static string ExtractBlock(string source, string tagName)
    {
        var pattern = $@"<(?:script\s+setup|{tagName})(?:\s[^>]*)?>([\s\S]*?)</(?:script|{tagName})>";
        var match = Regex.Match(source, pattern, RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return string.Empty;
    }

    #endregion

    #region Private Methods - Script Parsing

    private List<FieldDecl> ParseScriptBlock(string scriptContent)
    {
        var properties = new List<FieldDecl>();

        if (string.IsNullOrWhiteSpace(scriptContent))
        {
            return properties;
        }

        var lines = scriptContent.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("let ") || trimmed.StartsWith("const "))
            {
                var prop = ParsePropertyDeclaration(trimmed);
                if (prop is not null)
                {
                    properties.Add(prop);
                }
            }
            else if (trimmed.StartsWith("micro "))
            {
                // 微函数声明，暂不处理
            }
        }

        return properties;
    }

    private FieldDecl? ParsePropertyDeclaration(string line)
    {
        var match = PropertyDeclRegex().Match(line);

        if (!match.Success)
        {
            return null;
        }

        var isMutable = match.Groups[1].Value == "let";
        var name = match.Groups[2].Value;
        var typeStr = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null;
        var defaultStr = match.Groups[4].Success ? match.Groups[4].Value.Trim() : null;

        var type = typeStr is not null
            ? new TypeAnnotation(null, MapJsTypeToGgType(typeStr), Array.Empty<TypeAnnotation>())
            : new TypeAnnotation(null, InferTypeFromDefault(defaultStr), Array.Empty<TypeAnnotation>());

        AstNode? defaultValue = null;
        if (defaultStr is not null)
        {
            defaultValue = ParseDefaultValue(defaultStr);
        }

        var attrs = new List<AttributeDecl>();
        if (!isMutable)
        {
            attrs.Add(new AttributeDecl(null, "Readonly", Array.Empty<KeyValuePair<string, string>>()));
        }

        return new FieldDecl(null, name, type, defaultValue, attrs);
    }

    private static string MapJsTypeToGgType(string jsType)
    {
        return jsType.Trim() switch
        {
            "number" => "f64",
            "string" => "string",
            "boolean" => "bool",
            "object" => "Map",
            "array" => "Array",
            _ => jsType.Trim()
        };
    }

    private static string InferTypeFromDefault(string? defaultStr)
    {
        if (defaultStr is null)
        {
            return "auto";
        }

        defaultStr = defaultStr.Trim();

        if (defaultStr == "true" || defaultStr == "false")
        {
            return "bool";
        }

        if (defaultStr.StartsWith("\"") || defaultStr.StartsWith("'"))
        {
            return "string";
        }

        if (double.TryParse(defaultStr, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            return "f64";
        }

        if (defaultStr.StartsWith("["))
        {
            return "Array";
        }

        if (defaultStr.StartsWith("{"))
        {
            return "Map";
        }

        return "auto";
    }

    private static AstNode ParseDefaultValue(string defaultStr)
    {
        defaultStr = defaultStr.Trim();

        if (defaultStr == "true")
        {
            return new LiteralExpr(null, LiteralType.Boolean, true);
        }

        if (defaultStr == "false")
        {
            return new LiteralExpr(null, LiteralType.Boolean, false);
        }

        if (defaultStr.StartsWith("\"") && defaultStr.EndsWith("\""))
        {
            return new LiteralExpr(null, LiteralType.String, defaultStr[1..^1]);
        }

        if (defaultStr.StartsWith("'") && defaultStr.EndsWith("'"))
        {
            return new LiteralExpr(null, LiteralType.String, defaultStr[1..^1]);
        }

        if (int.TryParse(defaultStr, out var intVal))
        {
            return new LiteralExpr(null, LiteralType.Number, intVal.ToString());
        }

        if (float.TryParse(defaultStr, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var floatVal))
        {
            return new LiteralExpr(null, LiteralType.Number, floatVal.ToString());
        }

        return new IdentifierExpr(null, defaultStr);
    }

    #endregion

    #region Private Methods - Template Parsing

    private FunctionDecl? ParseTemplateBlock(string templateContent)
    {
        if (string.IsNullOrWhiteSpace(templateContent))
        {
            return null;
        }

        var statements = new List<AstNode>();
        ParseTemplateNodes(templateContent, statements);

        var body = new BlockStmt(null, statements);

        return new FunctionDecl(null, "render", Array.Empty<ParameterDecl>(), null, body, Array.Empty<AttributeDecl>());
    }

    private void ParseTemplateNodes(string template, List<AstNode> statements)
    {
        var pos = 0;

        while (pos < template.Length)
        {
            var textEnd = template.IndexOf('<', pos);

            if (textEnd < 0)
            {
                var text = template[pos..].Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    statements.Add(new ExprStmt(null, new LiteralExpr(null, LiteralType.String, text)));
                }
                break;
            }

            if (textEnd > pos)
            {
                var text = template[pos..textEnd].Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    statements.Add(new ExprStmt(null, new LiteralExpr(null, LiteralType.String, text)));
                }
            }

            var tagEnd = template.IndexOf('>', textEnd);
            if (tagEnd < 0)
            {
                break;
            }

            var tagContent = template[(textEnd + 1)..tagEnd].Trim();
            pos = tagEnd + 1;

            if (tagContent.StartsWith("!--"))
            {
                var commentEnd = template.IndexOf("-->", pos);
                pos = commentEnd >= 0 ? commentEnd + 3 : template.Length;
                continue;
            }

            if (tagContent.StartsWith("/"))
            {
                continue;
            }

            var isSelfClosing = tagContent.EndsWith("/");
            if (isSelfClosing)
            {
                tagContent = tagContent[..^1].Trim();
            }

            var (tagName, attributes) = ParseTag(tagContent);

            if (tagName == "if" || tagName.StartsWith("if "))
            {
                var condition = tagName.Length > 3 ? tagName[3..].Trim() : "true";
                var (ifBody, endPos) = ParseConditionalBlock(template, pos, "if");
                pos = endPos;
                statements.Add(new IfStmt(null, new IdentifierExpr(null, condition), ifBody, null));
            }
            else if (tagName == "for" || tagName.StartsWith("for "))
            {
                var (forBody, endPos) = ParseForBlock(template, pos);
                pos = endPos;
                statements.Add(forBody);
            }
            else
            {
                var widgetCall = CreateWidgetCall(tagName, attributes);

                if (!isSelfClosing)
                {
                    var (children, endPos) = ParseChildContent(template, pos, tagName);
                    pos = endPos;

                    if (children.Statements.Count > 0)
                    {
                        statements.Add(new ExprStmt(null, widgetCall));
                        statements.AddRange(children.Statements);
                    }
                    else
                    {
                        statements.Add(new ExprStmt(null, widgetCall));
                    }
                }
                else
                {
                    statements.Add(new ExprStmt(null, widgetCall));
                }
            }
        }
    }

    private static (string TagName, Dictionary<string, string> Attributes) ParseTag(string tagContent)
    {
        var parts = tagContent.Split(' ', 2);
        var tagName = parts[0];
        var attributes = new Dictionary<string, string>();

        if (parts.Length > 1)
        {
            var attrString = parts[1];
            var attrMatches = AttributeRegex().Matches(attrString);

            foreach (Match match in attrMatches)
            {
                var key = match.Groups[1].Value;
                var value = match.Groups[2].Success ? match.Groups[2].Value : "true";
                attributes[key] = value;
            }
        }

        return (tagName, attributes);
    }

    private static CallExpr CreateWidgetCall(string tagName, Dictionary<string, string> attributes)
    {
        var args = new List<AstNode>();

        foreach (var (key, value) in attributes)
        {
            var propKey = key.TrimStart(':', '@');
            args.Add(new BinaryExpr(null,
                new IdentifierExpr(null, propKey),
                "=",
                ParseTemplateValue(value)));
        }

        return new CallExpr(null, new IdentifierExpr(null, $"widget_{tagName}"), args);
    }

    private static AstNode ParseTemplateValue(string value)
    {
        if (value.StartsWith("{{") && value.EndsWith("}}"))
        {
            var expr = value[2..^2].Trim();
            return new IdentifierExpr(null, expr);
        }

        if (value.StartsWith("\"") && value.EndsWith("\""))
        {
            return new LiteralExpr(null, LiteralType.String, value[1..^1]);
        }

        if (value.StartsWith("'") && value.EndsWith("'"))
        {
            return new LiteralExpr(null, LiteralType.String, value[1..^1]);
        }

        return new IdentifierExpr(null, value);
    }

    private (BlockStmt body, int endPos) ParseConditionalBlock(string template, int startPos, string blockType)
    {
        var statements = new List<AstNode>();
        var depth = 1;
        var pos = startPos;
        var blockStart = startPos;

        while (pos < template.Length && depth > 0)
        {
            var nextOpen = template.IndexOf('<', pos);
            if (nextOpen < 0)
            {
                break;
            }

            var nextClose = template.IndexOf('>', nextOpen);
            if (nextClose < 0)
            {
                break;
            }

            var tag = template[(nextOpen + 1)..nextClose].Trim();

            if (tag == blockType || tag.StartsWith(blockType + " "))
            {
                depth++;
            }
            else if (tag == "/" + blockType)
            {
                depth--;
                if (depth == 0)
                {
                    var blockContent = template[blockStart..nextOpen].Trim();
                    ParseTemplateNodes(blockContent, statements);
                    return (new BlockStmt(null, statements), nextClose + 1);
                }
            }

            pos = nextClose + 1;
        }

        return (new BlockStmt(null, statements), pos);
    }

    private (AstNode stmt, int endPos) ParseForBlock(string template, int startPos)
    {
        var pos = startPos;
        var depth = 1;
        var blockStart = startPos;

        while (pos < template.Length && depth > 0)
        {
            var nextOpen = template.IndexOf('<', pos);
            if (nextOpen < 0)
            {
                break;
            }

            var nextClose = template.IndexOf('>', nextOpen);
            if (nextClose < 0)
            {
                break;
            }

            var tag = template[(nextOpen + 1)..nextClose].Trim();

            if (tag.StartsWith("for ") || tag == "for")
            {
                depth++;
            }
            else if (tag == "/for")
            {
                depth--;
                if (depth == 0)
                {
                    var blockContent = template[blockStart..nextOpen].Trim();
                    var body = new BlockStmt(null, ParseTemplateStatements(blockContent));
                    return (new LoopStmt(null, "item", new IdentifierExpr(null, "items"), body), nextClose + 1);
                }
            }

            pos = nextClose + 1;
        }

        return (new BlockStmt(null, Array.Empty<AstNode>()), pos);
    }

    private (BlockStmt children, int endPos) ParseChildContent(string template, int startPos, string parentTag)
    {
        var statements = new List<AstNode>();
        var pos = startPos;
        var depth = 1;
        var blockStart = startPos;

        while (pos < template.Length && depth > 0)
        {
            var nextOpen = template.IndexOf('<', pos);
            if (nextOpen < 0)
            {
                break;
            }

            var nextClose = template.IndexOf('>', nextOpen);
            if (nextClose < 0)
            {
                break;
            }

            var tag = template[(nextOpen + 1)..nextClose].Trim();
            var tagName = tag.Split(' ')[0];

            if (tagName == parentTag)
            {
                depth++;
            }
            else if (tagName == "/" + parentTag)
            {
                depth--;
                if (depth == 0)
                {
                    var blockContent = template[blockStart..nextOpen].Trim();
                    if (!string.IsNullOrEmpty(blockContent))
                    {
                        ParseTemplateNodes(blockContent, statements);
                    }
                    return (new BlockStmt(null, statements), nextClose + 1);
                }
            }

            pos = nextClose + 1;
        }

        return (new BlockStmt(null, statements), pos);
    }

    private List<AstNode> ParseTemplateStatements(string content)
    {
        var statements = new List<AstNode>();
        ParseTemplateNodes(content, statements);
        return statements;
    }

    #endregion

    #region Private Methods - Style Parsing

    private Dictionary<string, string> ParseStyleBlock(string styleContent)
    {
        var styles = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(styleContent))
        {
            return styles;
        }

        var classPattern = @"\.([a-zA-Z_][\w-]*)\s*\{([^}]*)\}";
        var matches = Regex.Matches(styleContent, classPattern);

        foreach (Match match in matches)
        {
            var className = match.Groups[1].Value;
            var properties = match.Groups[2].Value.Trim();
            styles[className] = properties;
        }

        return styles;
    }

    #endregion

    #region Generated Regex

    [GeneratedRegex(@"(?:let|const)\s+(\w+)(?::\s*(\w+))?(?:\s*=\s*(.+?))?;?\s*$")]
    private static partial Regex PropertyDeclRegex();

    [GeneratedRegex(@"([@:]?[\w-]+)(?:=""([^""]*)""|='([^']*)')?")]
    private static partial Regex AttributeRegex();

    #endregion
}

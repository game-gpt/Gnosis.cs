namespace Gnosis.Widget.Style;

internal sealed class ScssParser
{
    private readonly string _source;
    private int _pos;
    private readonly Dictionary<string, ScssMixin> _mixins = new();
    private readonly ScssVariableScope _globalScope;
    private readonly List<StyleRule> _outputRules = [];

    public ScssParser(string source)
    {
        _source = source;
        _pos = 0;
        _globalScope = new ScssVariableScope();
    }

    public void Parse(StyleSheet sheet)
    {
        ParseBlock(_globalScope, null);

        foreach (var rule in _outputRules)
        {
            sheet.AddRule(rule);
        }
    }

    private void ParseBlock(ScssVariableScope scope, List<StyleSelector>? parentSelectors)
    {
        while (_pos < _source.Length)
        {
            SkipWhitespaceAndComments();
            if (_pos >= _source.Length)
            {
                break;
            }

            if (_source[_pos] == '}')
            {
                break;
            }

            if (TryParseVariableDeclaration(scope))
            {
                continue;
            }

            if (TryParseMixinDefinition(scope))
            {
                continue;
            }

            if (TryParseInclude(scope, parentSelectors))
            {
                continue;
            }

            if (TryParseAtRule(scope, parentSelectors))
            {
                continue;
            }

            var selectors = ParseSelectors();
            if (selectors.Count == 0)
            {
                _pos++;
                continue;
            }

            SkipWhitespace();
            if (_pos >= _source.Length)
            {
                break;
            }

            if (_source[_pos] != '{')
            {
                continue;
            }

            _pos++;

            var resolvedSelectors = ResolveNestedSelectors(parentSelectors, selectors);

            var childScope = scope.Push();
            var declarations = new List<StyleDeclaration>();
            var hasNestedContent = false;

            ParseBlockContent(childScope, resolvedSelectors, declarations, ref hasNestedContent);

            SkipWhitespace();
            if (_pos < _source.Length && _source[_pos] == '}')
            {
                _pos++;
            }

            if (declarations.Count > 0)
            {
                var evaluator = new ScssEvaluator(childScope);
                var evaluatedDecls = declarations.Select(d =>
                    new StyleDeclaration(d.Property, evaluator.Evaluate(d.Value), d.Specificity, d.Important)).ToList();

                _outputRules.Add(new StyleRule(resolvedSelectors, evaluatedDecls));
            }
        }
    }

    private void ParseBlockContent(ScssVariableScope scope, List<StyleSelector> currentSelectors,
        List<StyleDeclaration> declarations, ref bool hasNestedContent)
    {
        while (_pos < _source.Length)
        {
            SkipWhitespaceAndComments();
            if (_pos >= _source.Length || _source[_pos] == '}')
            {
                break;
            }

            if (TryParseVariableDeclaration(scope))
            {
                continue;
            }

            if (TryParseIncludeInDeclarations(scope, declarations))
            {
                continue;
            }

            var peekSelectors = PeekSelectors();
            if (peekSelectors != null && peekSelectors.Count > 0)
            {
                hasNestedContent = true;
                ParseBlock(scope, currentSelectors);
                continue;
            }

            var decl = ParseSingleDeclaration(scope);
            if (decl != null)
            {
                declarations.Add(decl);
            }
            else
            {
                _pos++;
            }
        }
    }

    private bool TryParseVariableDeclaration(ScssVariableScope scope)
    {
        if (_pos >= _source.Length || _source[_pos] != '$')
        {
            return false;
        }

        var savePos = _pos;
        _pos++;

        var name = ReadIdent();
        if (name.Length == 0)
        {
            _pos = savePos;
            return false;
        }

        SkipWhitespace();
        if (_pos >= _source.Length || _source[_pos] != ':')
        {
            _pos = savePos;
            return false;
        }

        _pos++;
        SkipWhitespace();

        var value = ReadUntilSemicolon();
        if (_pos < _source.Length && _source[_pos] == ';')
        {
            _pos++;
        }

        scope.Define(name, value.Trim());
        return true;
    }

    private bool TryParseMixinDefinition(ScssVariableScope scope)
    {
        if (!PeekKeyword("@mixin"))
        {
            return false;
        }

        _pos += 6;
        SkipWhitespace();

        var name = ReadIdent();
        if (name.Length == 0)
        {
            return true;
        }

        SkipWhitespace();

        var parameters = new List<string>();
        if (_pos < _source.Length && _source[_pos] == '(')
        {
            _pos++;
            parameters = ParseMixinParameters();
            SkipWhitespace();
            if (_pos < _source.Length && _source[_pos] == ')')
            {
                _pos++;
            }
        }

        SkipWhitespace();
        if (_pos >= _source.Length || _source[_pos] != '{')
        {
            return true;
        }

        _pos++;
        var bodyStart = _pos;
        var depth = 1;

        while (_pos < _source.Length && depth > 0)
        {
            if (_source[_pos] == '{')
            {
                depth++;
            }
            else if (_source[_pos] == '}')
            {
                depth--;
            }

            if (depth > 0)
            {
                _pos++;
            }
        }

        var body = _source.Substring(bodyStart, _pos - bodyStart);

        if (_pos < _source.Length && _source[_pos] == '}')
        {
            _pos++;
        }

        _mixins[name] = new ScssMixin(name, parameters, body);
        return true;
    }

    private bool TryParseInclude(ScssVariableScope scope, List<StyleSelector>? parentSelectors)
    {
        return TryParseIncludeCore(scope, parentSelectors, null);
    }

    private bool TryParseIncludeInDeclarations(ScssVariableScope scope, List<StyleDeclaration> declarations)
    {
        return TryParseIncludeCore(scope, null, declarations);
    }

    private bool TryParseIncludeCore(ScssVariableScope scope, List<StyleSelector>? parentSelectors,
        List<StyleDeclaration>? declarations)
    {
        if (!PeekKeyword("@include"))
        {
            return false;
        }

        _pos += 8;
        SkipWhitespace();

        var name = ReadIdent();
        if (name.Length == 0)
        {
            return true;
        }

        SkipWhitespace();

        var args = new List<string>();
        if (_pos < _source.Length && _source[_pos] == '(')
        {
            _pos++;
            args = ParseMixinArguments();
            SkipWhitespace();
            if (_pos < _source.Length && _source[_pos] == ')')
            {
                _pos++;
            }
        }

        SkipWhitespace();
        if (_pos < _source.Length && _source[_pos] == ';')
        {
            _pos++;
        }

        if (!_mixins.TryGetValue(name, out var mixin))
        {
            return true;
        }

        var mixinScope = scope.Push();
        for (var i = 0; i < mixin.Parameters.Count && i < args.Count; i++)
        {
            mixinScope.Define(mixin.Parameters[i], args[i]);
        }

        var subParser = new ScssParserSub(mixin.Body, mixinScope, _mixins);
        var mixinDecls = subParser.ParseDeclarations();

        var evaluator = new ScssEvaluator(mixinScope);
        foreach (var decl in mixinDecls)
        {
            var evaluated = new StyleDeclaration(decl.Property, evaluator.Evaluate(decl.Value), decl.Specificity, decl.Important);
            declarations?.Add(evaluated);
        }

        return true;
    }

    private bool TryParseAtRule(ScssVariableScope scope, List<StyleSelector>? parentSelectors)
    {
        if (_pos >= _source.Length || _source[_pos] != '@')
        {
            return false;
        }

        if (PeekKeyword("@mixin") || PeekKeyword("@include"))
        {
            return false;
        }

        var ruleName = ReadAtRuleName();

        if (ruleName == "extend")
        {
            SkipWhitespace();
            var extendSelector = ReadUntilSemicolon().Trim();
            if (_pos < _source.Length && _source[_pos] == ';')
            {
                _pos++;
            }

            return true;
        }

        if (ruleName == "if" || ruleName == "each" || ruleName == "for")
        {
            SkipBlock();
            return true;
        }

        SkipWhitespace();
        if (_pos < _source.Length && _source[_pos] == '{')
        {
            _pos++;
            SkipBlock();
            if (_pos < _source.Length && _source[_pos] == '}')
            {
                _pos++;
            }
        }
        else
        {
            ReadUntilSemicolon();
            if (_pos < _source.Length && _source[_pos] == ';')
            {
                _pos++;
            }
        }

        return true;
    }

    private List<StyleSelector> ResolveNestedSelectors(List<StyleSelector>? parent, List<StyleSelector> child)
    {
        if (parent == null || parent.Count == 0)
        {
            return child;
        }

        var result = new List<StyleSelector>();

        foreach (var p in parent)
        {
            foreach (var c in child)
            {
                if (c.Value == "&")
                {
                    result.Add(p);
                }
                else if (c.Value.StartsWith("&"))
                {
                    var suffix = c.Value.Substring(1);
                    result.Add(new StyleSelector(p.Type, p.Value + suffix));
                }
                else
                {
                    result.Add(p);
                    result.Add(c);
                }
            }
        }

        return result;
    }

    private List<StyleSelector> ParseSelectors()
    {
        var selectors = new List<StyleSelector>();

        while (_pos < _source.Length)
        {
            SkipWhitespace();
            if (_pos >= _source.Length || _source[_pos] == '{' || _source[_pos] == '}')
            {
                break;
            }

            if (_source[_pos] == ';')
            {
                _pos++;
                break;
            }

            if (_source[_pos] == ',')
            {
                _pos++;
                continue;
            }

            var selector = ParseSingleSelector();
            if (selector != null)
            {
                selectors.Add(selector.Value);
            }
            else
            {
                break;
            }
        }

        return selectors;
    }

    private List<StyleSelector>? PeekSelectors()
    {
        var savePos = _pos;
        var selectors = new List<StyleSelector>();
        var foundBrace = false;

        while (_pos < _source.Length)
        {
            SkipWhitespace();
            if (_pos >= _source.Length)
            {
                break;
            }

            if (_source[_pos] == '{')
            {
                foundBrace = true;
                break;
            }

            if (_source[_pos] == ':' || _source[_pos] == ';')
            {
                break;
            }

            var selector = ParseSingleSelector();
            if (selector != null)
            {
                selectors.Add(selector.Value);
            }
            else
            {
                break;
            }
        }

        _pos = savePos;
        return foundBrace && selectors.Count > 0 ? selectors : null;
    }

    private StyleSelector? ParseSingleSelector()
    {
        if (_pos >= _source.Length)
        {
            return null;
        }

        var ch = _source[_pos];

        if (ch == '&')
        {
            _pos++;
            var suffix = ReadIdent();
            return new StyleSelector(StyleSelectorType.ParentRef, "&" + suffix);
        }

        if (ch == '#')
        {
            _pos++;
            var id = ReadIdent();
            return StyleSelector.ById(id);
        }

        if (ch == '.')
        {
            _pos++;
            var className = ReadIdent();
            return StyleSelector.ByClass(className);
        }

        if (ch == ':')
        {
            _pos++;
            var name = ReadIdent();
            return StyleSelector.ByPseudoClass(name);
        }

        if (IsIdentStart(ch))
        {
            var typeName = ReadIdent();
            return StyleSelector.ByType(typeName);
        }

        return null;
    }

    private StyleDeclaration? ParseSingleDeclaration(ScssVariableScope scope)
    {
        var property = ReadIdent();
        if (property.Length == 0)
        {
            return null;
        }

        SkipWhitespace();
        if (_pos >= _source.Length || _source[_pos] != ':')
        {
            return null;
        }

        _pos++;
        SkipWhitespace();

        var value = ReadUntilSemicolonOrBrace();
        SkipWhitespace();

        string? important = null;
        var trimmedValue = value.Trim();
        if (trimmedValue.EndsWith("!important", StringComparison.OrdinalIgnoreCase))
        {
            important = "!important";
            trimmedValue = trimmedValue[..^"!important".Length].Trim();
        }

        if (_pos < _source.Length && _source[_pos] == ';')
        {
            _pos++;
        }

        return new StyleDeclaration(property, trimmedValue, 0, important);
    }

    private List<string> ParseMixinParameters()
    {
        var parameters = new List<string>();

        while (_pos < _source.Length)
        {
            SkipWhitespace();
            if (_pos >= _source.Length || _source[_pos] == ')')
            {
                break;
            }

            if (_source[_pos] == ',')
            {
                _pos++;
                continue;
            }

            if (_source[_pos] == '$')
            {
                _pos++;
                var name = ReadIdent();
                if (name.Length > 0)
                {
                    parameters.Add(name);
                }
            }
            else
            {
                _pos++;
            }
        }

        return parameters;
    }

    private List<string> ParseMixinArguments()
    {
        var args = new List<string>();
        var depth = 0;
        var start = _pos;

        while (_pos < _source.Length)
        {
            var ch = _source[_pos];

            if (ch == '(')
            {
                depth++;
                _pos++;
                continue;
            }

            if (ch == ')')
            {
                if (depth == 0)
                {
                    break;
                }

                depth--;
                _pos++;
                continue;
            }

            if (ch == ',' && depth == 0)
            {
                args.Add(_source.Substring(start, _pos - start).Trim());
                _pos++;
                start = _pos;
                continue;
            }

            _pos++;
        }

        if (_pos > start)
        {
            args.Add(_source.Substring(start, _pos - start).Trim());
        }

        return args;
    }

    private string ReadAtRuleName()
    {
        if (_pos < _source.Length && _source[_pos] == '@')
        {
            _pos++;
        }

        return ReadIdent();
    }

    private string ReadIdent()
    {
        var start = _pos;

        while (_pos < _source.Length && IsIdentChar(_source[_pos]))
        {
            _pos++;
        }

        return _source.Substring(start, _pos - start);
    }

    private string ReadUntilSemicolon()
    {
        var start = _pos;
        var depth = 0;

        while (_pos < _source.Length)
        {
            if (_source[_pos] == '(')
            {
                depth++;
            }
            else if (_source[_pos] == ')')
            {
                depth--;
            }
            else if (_source[_pos] == ';' && depth == 0)
            {
                break;
            }

            _pos++;
        }

        return _source.Substring(start, _pos - start);
    }

    private string ReadUntilSemicolonOrBrace()
    {
        var start = _pos;
        var depth = 0;

        while (_pos < _source.Length)
        {
            if (_source[_pos] == '(')
            {
                depth++;
            }
            else if (_source[_pos] == ')')
            {
                depth--;
            }
            else if ((_source[_pos] == ';' || _source[_pos] == '}') && depth == 0)
            {
                break;
            }

            _pos++;
        }

        return _source.Substring(start, _pos - start);
    }

    private void SkipBlock()
    {
        var depth = 0;

        while (_pos < _source.Length)
        {
            if (_source[_pos] == '{')
            {
                depth++;
            }
            else if (_source[_pos] == '}')
            {
                depth--;
                if (depth <= 0)
                {
                    return;
                }
            }

            _pos++;
        }
    }

    private bool PeekKeyword(string keyword)
    {
        if (_pos + keyword.Length > _source.Length)
        {
            return false;
        }

        if (_source.Substring(_pos, keyword.Length) != keyword)
        {
            return false;
        }

        if (_pos + keyword.Length < _source.Length && IsIdentChar(_source[_pos + keyword.Length]))
        {
            return false;
        }

        return true;
    }

    private void SkipWhitespaceAndComments()
    {
        while (_pos < _source.Length)
        {
            if (char.IsWhiteSpace(_source[_pos]))
            {
                _pos++;
                continue;
            }

            if (_pos + 1 < _source.Length && _source[_pos] == '/' && _source[_pos + 1] == '/')
            {
                while (_pos < _source.Length && _source[_pos] != '\n')
                {
                    _pos++;
                }

                continue;
            }

            if (_pos + 1 < _source.Length && _source[_pos] == '/' && _source[_pos + 1] == '*')
            {
                _pos += 2;
                while (_pos + 1 < _source.Length && !(_source[_pos] == '*' && _source[_pos + 1] == '/'))
                {
                    _pos++;
                }

                if (_pos + 1 < _source.Length)
                {
                    _pos += 2;
                }

                continue;
            }

            break;
        }
    }

    private void SkipWhitespace()
    {
        while (_pos < _source.Length && char.IsWhiteSpace(_source[_pos]))
        {
            _pos++;
        }
    }

    private static bool IsIdentStart(char ch) => char.IsLetter(ch) || ch == '_' || ch == '-';

    private static bool IsIdentChar(char ch) => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-';
}

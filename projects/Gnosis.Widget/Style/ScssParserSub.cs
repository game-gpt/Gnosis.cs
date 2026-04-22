namespace Gnosis.Widget.Style;

internal sealed class ScssParserSub
{
    private readonly string _source;
    private int _pos;
    private readonly ScssVariableScope _scope;
    private readonly Dictionary<string, ScssMixin> _mixins;

    public ScssParserSub(string source, ScssVariableScope scope, Dictionary<string, ScssMixin> mixins)
    {
        _source = source;
        _pos = 0;
        _scope = scope;
        _mixins = mixins;
    }

    public List<StyleDeclaration> ParseDeclarations()
    {
        var declarations = new List<StyleDeclaration>();

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

            if (TryParseVariableDeclaration(_scope))
            {
                continue;
            }

            if (TryParseInclude(declarations))
            {
                continue;
            }

            var decl = ParseSingleDeclaration();
            if (decl != null)
            {
                declarations.Add(decl);
            }
            else
            {
                _pos++;
            }
        }

        return declarations;
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

    private bool TryParseInclude(List<StyleDeclaration> declarations)
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

        var mixinScope = _scope.Push();
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
            declarations.Add(evaluated);
        }

        return true;
    }

    private StyleDeclaration? ParseSingleDeclaration()
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
            if (_source[_pos] == '(') depth++;
            else if (_source[_pos] == ')') depth--;
            else if (_source[_pos] == ';' && depth == 0) break;
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
            if (_source[_pos] == '(') depth++;
            else if (_source[_pos] == ')') depth--;
            else if ((_source[_pos] == ';' || _source[_pos] == '}') && depth == 0) break;
            _pos++;
        }

        return _source.Substring(start, _pos - start);
    }

    private bool PeekKeyword(string keyword)
    {
        if (_pos + keyword.Length > _source.Length) return false;
        if (_source.Substring(_pos, keyword.Length) != keyword) return false;
        if (_pos + keyword.Length < _source.Length && IsIdentChar(_source[_pos + keyword.Length])) return false;
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
                while (_pos < _source.Length && _source[_pos] != '\n') _pos++;
                continue;
            }

            if (_pos + 1 < _source.Length && _source[_pos] == '/' && _source[_pos + 1] == '*')
            {
                _pos += 2;
                while (_pos + 1 < _source.Length && !(_source[_pos] == '*' && _source[_pos + 1] == '/')) _pos++;
                if (_pos + 1 < _source.Length) _pos += 2;
                continue;
            }

            break;
        }
    }

    private void SkipWhitespace()
    {
        while (_pos < _source.Length && char.IsWhiteSpace(_source[_pos])) _pos++;
    }

    private static bool IsIdentChar(char ch) => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-';
}

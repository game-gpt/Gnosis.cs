namespace Gnosis.Widget.Style;

internal sealed class ScssEvaluator
{
    private readonly ScssVariableScope _scope;

    public ScssEvaluator(ScssVariableScope scope)
    {
        _scope = scope;
    }

    public string Evaluate(string value)
    {
        value = InterpolateVariables(value);
        value = EvaluateExpressions(value);
        return value.Trim();
    }

    private string InterpolateVariables(string value)
    {
        var result = new System.Text.StringBuilder(value.Length);
        var i = 0;

        while (i < value.Length)
        {
            if (value[i] == '#' && i + 1 < value.Length && value[i + 1] == '{')
            {
                var end = value.IndexOf('}', i + 2);
                if (end >= 0)
                {
                    var varName = value.Substring(i + 2, end - i - 2).Trim();
                    var resolved = ResolveVariableReference(varName);
                    result.Append(resolved);
                    i = end + 1;
                    continue;
                }
            }

            if (value[i] == '$')
            {
                var (varName, consumed) = ReadVariableName(value, i + 1);
                if (varName.Length > 0)
                {
                    var resolved = ResolveVariableReference(varName);
                    result.Append(resolved);
                    i += consumed + 1;
                    continue;
                }
            }

            result.Append(value[i]);
            i++;
        }

        return result.ToString();
    }

    private string ResolveVariableReference(string name)
    {
        var resolved = _scope.Resolve(name);
        if (resolved != null)
        {
            return Evaluate(resolved);
        }

        return $"${name}";
    }

    private string EvaluateExpressions(string value)
    {
        return TryEvaluateArithmetic(value, out var result) ? result : value;
    }

    private bool TryEvaluateArithmetic(string value, out string result)
    {
        result = value;

        var unitSuffix = ExtractUnit(value, out var numericPart);
        if (numericPart.Length == 0)
        {
            return false;
        }

        if (!TryParseArithmetic(numericPart, out var number))
        {
            return false;
        }

        if (number == (int)number)
        {
            result = $"{(int)number}{unitSuffix}";
        }
        else
        {
            result = $"{number}{unitSuffix}";
        }

        return true;
    }

    private string ExtractUnit(string value, out string numericPart)
    {
        var i = value.Length - 1;
        while (i >= 0 && char.IsLetter(value[i]))
        {
            i--;
        }

        if (i < value.Length - 1)
        {
            numericPart = value.Substring(0, i + 1);
            return value.Substring(i + 1);
        }

        numericPart = value;
        return "";
    }

    private bool TryParseArithmetic(string expr, out float result)
    {
        result = 0;

        expr = expr.Trim();
        if (float.TryParse(expr, out result))
        {
            return true;
        }

        var opIndex = FindLowestPrecedenceOp(expr);
        if (opIndex < 0)
        {
            return false;
        }

        var leftExpr = expr.Substring(0, opIndex).Trim();
        var op = expr[opIndex];
        var rightExpr = expr.Substring(opIndex + 1).Trim();

        if (!TryParseArithmetic(leftExpr, out var left) || !TryParseArithmetic(rightExpr, out var right))
        {
            return false;
        }

        result = op switch
        {
            '+' => left + right,
            '-' => left - right,
            '*' => left * right,
            '/' when right != 0 => left / right,
            '%' when right != 0 => left % right,
            _ => 0
        };

        return true;
    }

    private static int FindLowestPrecedenceOp(string expr)
    {
        var depth = 0;
        var addSubIndex = -1;
        var mulDivIndex = -1;

        for (var i = 0; i < expr.Length; i++)
        {
            var ch = expr[i];

            if (ch == '(')
            {
                depth++;
                continue;
            }

            if (ch == ')')
            {
                depth--;
                continue;
            }

            if (depth > 0)
            {
                continue;
            }

            if ((ch == '+' || ch == '-') && i > 0 && !IsOperatorContext(expr, i))
            {
                if (addSubIndex < 0)
                {
                    addSubIndex = i;
                }
            }
            else if (ch == '*' || ch == '/' || ch == '%')
            {
                if (mulDivIndex < 0)
                {
                    mulDivIndex = i;
                }
            }
        }

        return addSubIndex >= 0 ? addSubIndex : mulDivIndex;
    }

    private static bool IsOperatorContext(string expr, int index)
    {
        if (index == 0)
        {
            return true;
        }

        var prev = expr[index - 1];
        return prev == '+' || prev == '-' || prev == '*' || prev == '/' || prev == '%' || prev == '(' || char.IsWhiteSpace(prev) && index >= 2 && IsOperatorContext(expr, index - 1);
    }

    private static (string name, int consumed) ReadVariableName(string source, int start)
    {
        var i = start;
        while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '-' || source[i] == '_'))
        {
            i++;
        }

        return (source.Substring(start, i - start), i - start);
    }
}

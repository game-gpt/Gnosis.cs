namespace Gnosis.Graphic.Shader;

public sealed record ShaderAttributeIr(
    string Name,
    IReadOnlyList<KeyValuePair<string, string>> Arguments)
{
    public bool HasArgument(string key)
    {
        foreach (var arg in Arguments)
        {
            if (arg.Key == key)
            {
                return true;
            }
        }
        return false;
    }

    public string? GetArgument(string key)
    {
        foreach (var arg in Arguments)
        {
            if (arg.Key == key)
            {
                return arg.Value;
            }
        }
        return null;
    }
}

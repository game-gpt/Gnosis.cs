namespace Gnosis.Widget.Style;

public sealed class ScssMixin
{
    public string Name { get; }

    public IReadOnlyList<string> Parameters { get; }

    public string Body { get; }

    public ScssMixin(string name, IReadOnlyList<string> parameters, string body)
    {
        Name = name;
        Parameters = parameters;
        Body = body;
    }
}

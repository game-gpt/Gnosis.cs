namespace Gnosis.Widget.Style;

public enum StyleSelectorType
{
    Type,
    Id,
    Class,
    PseudoClass,
    ParentRef
}

public readonly struct StyleSelector : IEquatable<StyleSelector>
{
    public readonly StyleSelectorType Type;
    public readonly string Value;

    public StyleSelector(StyleSelectorType type, string value)
    {
        Type = type;
        Value = value;
    }

    public bool Equals(StyleSelector other) => Type == other.Type && Value == other.Value;

    public override bool Equals(object? obj) => obj is StyleSelector other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Type, Value);

    public static bool operator ==(StyleSelector left, StyleSelector right) => left.Equals(right);

    public static bool operator !=(StyleSelector left, StyleSelector right) => !left.Equals(right);

    public static StyleSelector ByType(string typeName) => new(StyleSelectorType.Type, typeName);

    public static StyleSelector ById(string id) => new(StyleSelectorType.Id, id);

    public static StyleSelector ByClass(string className) => new(StyleSelectorType.Class, className);

    public static StyleSelector ByPseudoClass(string name) => new(StyleSelectorType.PseudoClass, name);

    public static StyleSelector ParentRef(string suffix = "") => new(StyleSelectorType.ParentRef, "&" + suffix);
}

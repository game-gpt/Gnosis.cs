namespace Gnosis.Widget.Layout;

public sealed class GridRowDefinition
{
    public GridLength Height { get; }

    public GridRowDefinition(GridLength height)
    {
        Height = height;
    }

    public GridRowDefinition(float pixelHeight) : this(new GridLength(pixelHeight)) { }
}

public sealed class GridColumnDefinition
{
    public GridLength Width { get; }

    public GridColumnDefinition(GridLength width)
    {
        Width = width;
    }

    public GridColumnDefinition(float pixelWidth) : this(new GridLength(pixelWidth)) { }
}

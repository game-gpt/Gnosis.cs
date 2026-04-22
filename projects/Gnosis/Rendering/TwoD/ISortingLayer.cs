namespace Gnosis.Rendering.TwoD;

public interface ISortingLayer
{
    string Name { get; }
    int Id { get; }
    int Order { get; set; }
}

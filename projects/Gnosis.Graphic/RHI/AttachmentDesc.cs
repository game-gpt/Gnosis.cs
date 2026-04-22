namespace Gnosis.Graphic.RHI;

public record AttachmentDesc
{
    public required ResourceFormat Format { get; init; }
    public uint SampleCount { get; init; } = 1;
    public LoadAction LoadAction { get; init; }
    public StoreAction StoreAction { get; init; }
    public LoadAction StencilLoadAction { get; init; }
    public StoreAction StencilStoreAction { get; init; }
    public TextureLayout InitialLayout { get; init; } = TextureLayout.Undefined;
    public TextureLayout FinalLayout { get; init; } = TextureLayout.PresentSrc;
}

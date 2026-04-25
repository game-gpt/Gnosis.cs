namespace Gnosis.Platform;

public sealed record WindowCreateInfo
{
    public string Title { get; init; } = "Gnosis Engine";
    public uint Width { get; init; } = 1280;
    public uint Height { get; init; } = 720;
    public bool VSync { get; init; } = true;
    public bool Fullscreen { get; init; } = false;
    public bool Resizable { get; init; } = true;
    public bool Visible { get; init; } = true;
}

namespace Gnosis.Editor.Widget;

public sealed class UiRenderer : IWidgetRenderer
{
    #region Properties

    public int Width { get; }
    public int Height { get; }

    #endregion

    #region Constructors

    public UiRenderer(int width, int height)
    {
        Width = width;
        Height = height;
    }

    #endregion

    #region Public Methods

    public void Begin()
    {
    }

    public void End()
    {
    }

    public void Clear(float r, float g, float b, float a = 1.0f)
    {
    }

    public void DrawRect(float x, float y, float width, float height, float r, float g, float b, float a = 1.0f)
    {
    }

    public void DrawText(string text, float x, float y, float size, float r, float g, float b)
    {
    }

    public void PushClip(float x, float y, float width, float height)
    {
    }

    public void PopClip()
    {
    }

    public byte[] GetFramebufferData()
    {
        return Array.Empty<byte>();
    }

    #endregion
}

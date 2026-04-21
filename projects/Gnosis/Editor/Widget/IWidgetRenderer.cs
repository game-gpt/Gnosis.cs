namespace Gnosis.Editor.Widget;

public interface IWidgetRenderer
{
    int Width { get; }
    int Height { get; }

    void Begin();
    void End();
    void Clear(float r, float g, float b, float a = 1.0f);
    void DrawRect(float x, float y, float width, float height, float r, float g, float b, float a = 1.0f);
    void DrawText(string text, float x, float y, float size, float r, float g, float b);
    void PushClip(float x, float y, float width, float height);
    void PopClip();
    byte[] GetFramebufferData();
}

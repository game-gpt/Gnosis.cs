namespace Gnosis.GameUI.ValueObjects;

public readonly struct UIVertex
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;
    public readonly float U;
    public readonly float V;
    public readonly float R;
    public readonly float G;
    public readonly float B;
    public readonly float A;

    public UIVertex(float x, float y, float z, float u, float v, float r, float g, float b, float a)
    {
        X = x;
        Y = y;
        Z = z;
        U = u;
        V = v;
        R = r;
        G = g;
        B = b;
        A = a;
    }
}

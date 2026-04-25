using System.Runtime.InteropServices;
using System.Text;

namespace Gnosis.Platform.GL;

public sealed unsafe class GLContext : IDisposable
{
    #region 常量

    public const uint ColorBufferBit = 0x00004000;
    public const uint DepthBufferBit = 0x00000100;
    public const uint StencilBufferBit = 0x00000400;

    #endregion

    #region 委托声明

    private delegate void ClearColorProc(float r, float g, float b, float a);
    private delegate void ClearProc(uint mask);
    private delegate void ViewportProc(int x, int y, int width, int height);
    private delegate void EnableProc(uint cap);
    private delegate void DisableProc(uint cap);
    private delegate void BlendFuncProc(uint sfactor, uint dfactor);
    private delegate void DepthFuncProc(uint func);
    private delegate void DepthMaskProc(bool flag);
    private delegate void CullFaceProc(uint mode);
    private delegate void FrontFaceProc(uint mode);
    private delegate void PolygonModeProc(uint face, uint mode);
    private delegate void ScissorProc(int x, int y, int width, int height);
    private delegate void FlushProc();

    #endregion

    #region 字段

    private readonly Func<string, nint> _getProcAddress;
    private ClearColorProc? _clearColor;
    private ClearProc? _clear;
    private ViewportProc? _viewport;
    private EnableProc? _enable;
    private DisableProc? _disable;
    private BlendFuncProc? _blendFunc;
    private DepthFuncProc? _depthFunc;
    private DepthMaskProc? _depthMask;
    private CullFaceProc? _cullFace;
    private FrontFaceProc? _frontFace;
    private PolygonModeProc? _polygonMode;
    private ScissorProc? _scissor;
    private FlushProc? _flush;

    #endregion

    #region 构造函数

    public GLContext(Func<string, nint> getProcAddress)
    {
        _getProcAddress = getProcAddress;
        LoadFunctions();
    }

    #endregion

    #region 公开方法

    public void ClearColor(float r, float g, float b, float a)
    {
        _clearColor!(r, g, b, a);
    }

    public void Clear(uint mask)
    {
        _clear!(mask);
    }

    public void Viewport(int x, int y, int width, int height)
    {
        _viewport!(x, y, width, height);
    }

    public void Enable(uint cap)
    {
        _enable!(cap);
    }

    public void Disable(uint cap)
    {
        _disable!(cap);
    }

    public void BlendFunc(uint sfactor, uint dfactor)
    {
        _blendFunc!(sfactor, dfactor);
    }

    public void DepthFunc(uint func)
    {
        _depthFunc!(func);
    }

    public void DepthMask(bool flag)
    {
        _depthMask!(flag);
    }

    public void CullFace(uint mode)
    {
        _cullFace!(mode);
    }

    public void FrontFace(uint mode)
    {
        _frontFace!(mode);
    }

    public void PolygonMode(uint face, uint mode)
    {
        _polygonMode!(face, mode);
    }

    public void Scissor(int x, int y, int width, int height)
    {
        _scissor!(x, y, width, height);
    }

    public void Flush()
    {
        _flush!();
    }

    public nint GetProcAddress(string name)
    {
        return _getProcAddress(name);
    }

    #endregion

    #region 私有方法

    private void LoadFunctions()
    {
        _clearColor = Load<ClearColorProc>("glClearColor");
        _clear = Load<ClearProc>("glClear");
        _viewport = Load<ViewportProc>("glViewport");
        _enable = Load<EnableProc>("glEnable");
        _disable = Load<DisableProc>("glDisable");
        _blendFunc = Load<BlendFuncProc>("glBlendFunc");
        _depthFunc = Load<DepthFuncProc>("glDepthFunc");
        _depthMask = Load<DepthMaskProc>("glDepthMask");
        _cullFace = Load<CullFaceProc>("glCullFace");
        _frontFace = Load<FrontFaceProc>("glFrontFace");
        _polygonMode = Load<PolygonModeProc>("glPolygonMode");
        _scissor = Load<ScissorProc>("glScissor");
        _flush = Load<FlushProc>("glFlush");
    }

    private T Load<T>(string name) where T : Delegate
    {
        var ptr = _getProcAddress(name);
        if (ptr == nint.Zero)
        {
            Console.WriteLine($"[GLContext] 警告: 无法加载 GL 函数 '{name}'");
            return null!;
        }

        return Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
    }

    #endregion
}

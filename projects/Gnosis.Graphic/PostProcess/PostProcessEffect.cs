using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.PostProcess;

/// <summary>
/// 后处理效果基类
/// </summary>
public abstract class PostProcessEffect
{
    #region 属性

    public string Name { get; }
    public bool Enabled { get; set; }
    public int Order { get; set; }

    /// <summary>
    /// 关联的图形设备
    /// </summary>
    protected IDevice? Device { get; private set; }

    /// <summary>
    /// 是否已初始化
    /// </summary>
    protected bool IsInitialized { get; private set; }

    #endregion

    #region 构造函数

    protected PostProcessEffect(string name, int order = 0)
    {
        Name = name;
        Enabled = true;
        Order = order;
    }

    #endregion

    #region 内部方法

    /// <summary>
    /// 设置图形设备，由 PostProcessStack 调用
    /// </summary>
    internal void SetDevice(IDevice device)
    {
        Device = device;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 初始化效果资源
    /// </summary>
    public virtual void Initialize()
    {
        IsInitialized = true;
    }

    /// <summary>
    /// 释放效果资源
    /// </summary>
    public virtual void Dispose()
    {
        IsInitialized = false;
    }

    /// <summary>
    /// 设置渲染状态
    /// </summary>
    public abstract void Setup(ICommandTable commandTable, uint width, uint height);

    /// <summary>
    /// 执行后处理效果
    /// </summary>
    public abstract void Execute(ICommandTable commandTable, IResource inputTexture, IResource outputTexture);

    #endregion
}

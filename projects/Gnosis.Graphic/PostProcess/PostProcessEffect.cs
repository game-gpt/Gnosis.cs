using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.PostProcess;

public abstract class PostProcessEffect
{
    #region 属性

    public string Name { get; }
    public bool Enabled { get; set; }
    public int Order { get; set; }

    #endregion

    #region 构造函数

    protected PostProcessEffect(string name, int order = 0)
    {
        Name = name;
        Enabled = true;
        Order = order;
    }

    #endregion

    #region 公开方法

    public abstract void Setup(ICommandTable commandTable, uint width, uint height);

    public abstract void Execute(ICommandTable commandTable, IResource inputTexture, IResource outputTexture);

    #endregion
}

using System.Numerics;
using Gnosis.Graphic.RHI;
using Gnosis.Graphic.Sprite2D;

namespace Gnosis.Graphic.Pipeline;

public sealed class Sprite2DRenderPass : IRenderPass
{
    #region 字段

    private readonly SpriteBatch _spriteBatch;
    private readonly Camera2D _camera;
    private bool _isDisposed;

    #endregion

    #region 属性

    public string Name { get; }
    public bool Enabled { get; set; }

    #endregion

    #region 构造函数

    public Sprite2DRenderPass(SpriteBatch spriteBatch, Camera2D camera)
    {
        _spriteBatch = spriteBatch;
        _camera = camera;
        Name = "Sprite2D";
        Enabled = true;
    }

    #endregion

    #region IRenderPass 实现

    public void Execute(RenderContext context, ICommandTable commandTable)
    {
        if (!Enabled)
        {
            return;
        }

        var viewProj = _camera.ViewMatrix * _camera.ProjectionMatrix;
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendMode.Alpha, viewProj);
        _spriteBatch.Render(commandTable);
        _spriteBatch.End();
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
    }

    #endregion
}

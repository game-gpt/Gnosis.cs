using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Graphic.RHI;

/// <summary>
///     视图接口，描述渲染视角
/// </summary>
public interface IView
{
    /// <summary>
    ///     视图矩阵
    /// </summary>
    Matrix4x4 ViewMatrix { get; }

    /// <summary>
    ///     投影矩阵
    /// </summary>
    Matrix4x4 ProjectionMatrix { get; }

    /// <summary>
    ///     渲染路径标志
    /// </summary>
    RenderPathFlag RenderPathFlags { get; }
}

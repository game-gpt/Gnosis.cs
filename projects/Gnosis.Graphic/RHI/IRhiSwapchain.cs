namespace Gnosis.Graphic.RHI;

/// <summary>
/// 交换链接口，管理双缓冲/三缓冲图像呈现
/// </summary>
public interface IRhiSwapchain : IDisposable
{
    /// <summary>
    /// 交换链宽度
    /// </summary>
    uint Width { get; }

    /// <summary>
    /// 交换链高度
    /// </summary>
    uint Height { get; }

    /// <summary>
    /// 交换链图像格式
    /// </summary>
    ResourceFormat Format { get; }

    /// <summary>
    /// 交换链图像数量
    /// </summary>
    uint ImageCount { get; }

    /// <summary>
    /// 获取下一帧可呈现图像的索引
    /// </summary>
    /// <param name="semaphore">信号量，图像可用时触发</param>
    /// <param name="fence">围栏，图像可用时触发</param>
    /// <returns>下一帧图像索引</returns>
    uint AcquireNextImage(IRhiSemaphore? semaphore, IRhiFence? fence);

    /// <summary>
    /// 呈现当前帧图像
    /// </summary>
    /// <param name="waitSemaphores">等待的信号量列表</param>
    void Present(IReadOnlyList<IRhiSemaphore> waitSemaphores);

    /// <summary>
    /// 调整交换链尺寸
    /// </summary>
    /// <param name="width">新宽度</param>
    /// <param name="height">新高度</param>
    void Resize(uint width, uint height);
}

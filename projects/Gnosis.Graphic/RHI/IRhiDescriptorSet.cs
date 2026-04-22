namespace Gnosis.Graphic.RHI;

/// <summary>
/// 描述符集接口，绑定着色器资源（缓冲区、纹理、采样器）
/// </summary>
public unsafe interface IRhiDescriptorSet : IDisposable
{
    /// <summary>
    /// 绑定缓冲区到描述符集
    /// </summary>
    /// <param name="binding">绑定槽位</param>
    /// <param name="buffer">缓冲区资源</param>
    /// <param name="offset">偏移量</param>
    /// <param name="range">范围</param>
    void BindBuffer(uint binding, IResource buffer, ulong offset = 0, ulong range = ulong.MaxValue);

    /// <summary>
    /// 绑定纹理到描述符集
    /// </summary>
    /// <param name="binding">绑定槽位</param>
    /// <param name="texture">纹理资源</param>
    void BindTexture(uint binding, IResource texture);

    /// <summary>
    /// 绑定采样器到描述符集
    /// </summary>
    /// <param name="binding">绑定槽位</param>
    /// <param name="sampler">采样器资源</param>
    void BindSampler(uint binding, IResource sampler);

    /// <summary>
    /// 绑定 Uniform 数据到描述符集
    /// </summary>
    /// <param name="binding">绑定槽位</param>
    /// <param name="data">数据指针</param>
    /// <param name="size">数据大小</param>
    void BindUniformData(uint binding, void* data, ulong size);
}

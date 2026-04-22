namespace Gnosis.Graphic.RHI;

/// <summary>
/// 命令表接口，录制 GPU 命令
/// </summary>
public interface ICommandTable : IDisposable
{
    /// <summary>
    /// 开始录制命令
    /// </summary>
    void Begin();

    /// <summary>
    /// 结束录制命令
    /// </summary>
    void End();

    /// <summary>
    /// 开始渲染通道
    /// </summary>
    /// <param name="renderPass">渲染通道</param>
    /// <param name="framebuffer">帧缓冲</param>
    /// <param name="clearColors">清除颜色列表</param>
    /// <param name="clearDepth">清除深度值</param>
    /// <param name="clearStencil">清除模板值</param>
    void BeginRenderPass(IRhiRenderPass renderPass, IRhiFramebuffer framebuffer, IReadOnlyList<(float r, float g, float b, float a)>? clearColors = null, float clearDepth = 1.0f, byte clearStencil = 0);

    /// <summary>
    /// 结束渲染通道
    /// </summary>
    void EndRenderPass();

    /// <summary>
    /// 绑定管线状态
    /// </summary>
    /// <param name="pipelineState">管线状态对象</param>
    void SetPipelineState(IPipelineState pipelineState);

    /// <summary>
    /// 设置视口
    /// </summary>
    void SetViewport(float x, float y, float width, float height, float minDepth = 0.0f, float maxDepth = 1.0f);

    /// <summary>
    /// 设置裁剪矩形
    /// </summary>
    void SetScissor(int x, int y, uint width, uint height);

    /// <summary>
    /// 绑定顶点缓冲区
    /// </summary>
    void SetVertexBuffer(IResource buffer, ulong offset = 0);

    /// <summary>
    /// 绑定索引缓冲区
    /// </summary>
    void SetIndexBuffer(IResource buffer, ulong offset = 0);

    /// <summary>
    /// 清除渲染目标
    /// </summary>
    /// <param name="attachmentIndex">附件索引</param>
    /// <param name="r">红色分量</param>
    /// <param name="g">绿色分量</param>
    /// <param name="b">蓝色分量</param>
    /// <param name="a">透明度分量</param>
    void ClearRenderTarget(uint attachmentIndex, float r, float g, float b, float a);

    /// <summary>
    /// 清除深度模板
    /// </summary>
    /// <param name="depth">深度值</param>
    /// <param name="stencil">模板值</param>
    void ClearDepthStencil(float depth, byte stencil);

    /// <summary>
    /// 绑定描述符集
    /// </summary>
    /// <param name="descriptorSet">描述符集</param>
    /// <param name="setIndex">描述符集索引</param>
    void BindDescriptorSet(IRhiDescriptorSet descriptorSet, uint setIndex);

    /// <summary>
    /// 非索引绘制
    /// </summary>
    void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0);

    /// <summary>
    /// 索引绘制
    /// </summary>
    void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0);

    /// <summary>
    /// 计算调度
    /// </summary>
    void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ);

    /// <summary>
    /// 管线屏障，确保内存访问顺序
    /// </summary>
    void PipelineBarrier(PipelineStageFlag srcStage, PipelineStageFlag dstStage, AccessFlag srcAccess, AccessFlag dstAccess);

    /// <summary>
    /// 复制资源
    /// </summary>
    void CopyResource(IResource src, IResource dst);
}

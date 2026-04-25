namespace Gnosis.Graphic.RHI.Vulkan;

/// <summary>
/// Vulkan 命令表实现，封装命令缓冲区录制
/// </summary>
internal sealed unsafe class VulkanCommandTable : RHI.ICommandTable
{
    /// <summary>
    /// Vulkan 命令缓冲区句柄
    /// </summary>
    public VkCommandBuffer CommandBuffer { get; }

    /// <summary>
    /// 关联的逻辑设备
    /// </summary>
    private readonly VkDevice _device;

    /// <summary>
    /// 关联的命令池
    /// </summary>
    private readonly VkCommandPool _commandPool;

    /// <summary>
    /// 当前绑定的管线状态
    /// </summary>
    private VulkanPipelineState? _currentPipeline;

    /// <summary>
    /// 是否已释放
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    /// 创建 Vulkan 命令表
    /// </summary>
    /// <param name="device">逻辑设备</param>
    /// <param name="commandPool">命令池</param>
    /// <param name="commandBuffer">命令缓冲区</param>
    public VulkanCommandTable(VkDevice device, VkCommandPool commandPool, VkCommandBuffer commandBuffer)
    {
        _device = device;
        _commandPool = commandPool;
        CommandBuffer = commandBuffer;
    }

    #region ICommandTable 实现

    /// <summary>
    /// 开始录制命令
    /// </summary>
    public void Begin()
    {
        var beginInfo = new VkCommandBufferBeginInfo
        {
            SType = VkStructureType.CommandBufferBeginInfo,
            PNext = null,
            Flags = VkCommandBufferUsageFlagBits.OneTimeSubmit,
            PInheritanceInfo = null
        };

        VulkanNative.CheckResult(
            VulkanNative.vkBeginCommandBuffer(CommandBuffer, &beginInfo),
            "开始命令缓冲区录制");
    }

    /// <summary>
    /// 结束录制命令
    /// </summary>
    public void End()
    {
        VulkanNative.CheckResult(
            VulkanNative.vkEndCommandBuffer(CommandBuffer),
            "结束命令缓冲区录制");
    }

    /// <summary>
    /// 开始渲染通道
    /// </summary>
    /// <param name="renderPass">渲染通道</param>
    /// <param name="framebuffer">帧缓冲</param>
    /// <param name="clearColors">清除颜色列表</param>
    /// <param name="clearDepth">清除深度值</param>
    /// <param name="clearStencil">清除模板值</param>
    public void BeginRenderPass(RHI.IRhiRenderPass renderPass, RHI.IRhiFramebuffer framebuffer, IReadOnlyList<(float r, float g, float b, float a)>? clearColors = null, float clearDepth = 1.0f, byte clearStencil = 0)
    {
        var vkRenderPass = ((VulkanRenderPass)renderPass).Handle;
        var vkFramebuffer = ((VulkanFramebuffer)framebuffer).Handle;

        var clearValues = stackalloc VkClearValue[Math.Max(clearColors?.Count ?? 0, 1) + 1];
        var clearCount = 0;

        if (clearColors != null)
        {
            for (var i = 0; i < clearColors.Count; i++)
            {
                var (r, g, b, a) = clearColors[i];
                clearValues[clearCount] = new VkClearValue
                {
                    Color = new VkClearColorValue()
                };
                clearValues[clearCount].Color.Float32[0] = r;
                clearValues[clearCount].Color.Float32[1] = g;
                clearValues[clearCount].Color.Float32[2] = b;
                clearValues[clearCount].Color.Float32[3] = a;
                clearCount++;
            }
        }

        var renderArea = new VkRect2D
        {
            Offset = new VkOffset2D { X = 0, Y = 0 },
            Extent = new VkExtent2D { Width = framebuffer.Width, Height = framebuffer.Height }
        };

        var beginInfo = new VkRenderPassBeginInfo
        {
            SType = VkStructureType.RenderPassBeginInfo,
            PNext = null,
            RenderPass = vkRenderPass,
            Framebuffer = vkFramebuffer,
            RenderArea = renderArea,
            ClearValueCount = (uint)clearCount,
            PClearValues = clearValues
        };

        VulkanNative.vkCmdBeginRenderPass(CommandBuffer, &beginInfo, VkSubpassContents.Inline);
    }

    /// <summary>
    /// 结束渲染通道
    /// </summary>
    public void EndRenderPass()
    {
        VulkanNative.vkCmdEndRenderPass(CommandBuffer);
    }

    /// <summary>
    /// 绑定管线状态
    /// </summary>
    /// <param name="pipelineState">管线状态对象</param>
    public void SetPipelineState(RHI.IPipelineState pipelineState)
    {
        var vkPipelineState = (VulkanPipelineState)pipelineState;
        vkPipelineState.EnsurePipelineCreated();
        _currentPipeline = vkPipelineState;
        VulkanNative.vkCmdBindPipeline(CommandBuffer, vkPipelineState.BindPoint, vkPipelineState.Pipeline);
    }

    /// <summary>
    /// 设置视口
    /// </summary>
    public void SetViewport(float x, float y, float width, float height, float minDepth = 0.0f, float maxDepth = 1.0f)
    {
        var viewport = new VkViewport
        {
            X = x,
            Y = y + height,
            Width = width,
            Height = -height,
            MinDepth = minDepth,
            MaxDepth = maxDepth
        };

        VulkanNative.vkCmdSetViewport(CommandBuffer, 0, 1, &viewport);
    }

    /// <summary>
    /// 设置裁剪矩形
    /// </summary>
    public void SetScissor(int x, int y, uint width, uint height)
    {
        var scissor = new VkRect2D
        {
            Offset = new VkOffset2D { X = x, Y = y },
            Extent = new VkExtent2D { Width = width, Height = height }
        };

        VulkanNative.vkCmdSetScissor(CommandBuffer, 0, 1, &scissor);
    }

    /// <summary>
    /// 绑定顶点缓冲区
    /// </summary>
    /// <param name="buffer">缓冲区资源</param>
    /// <param name="offset">偏移量</param>
    public void SetVertexBuffer(RHI.IResource buffer, ulong offset = 0)
    {
        var vkResource = (VulkanResource)buffer;
        var bufferHandle = vkResource.BufferHandle;
        var offsetLocal = offset;
        VulkanNative.vkCmdBindVertexBuffers(CommandBuffer, 0, 1, &bufferHandle, &offsetLocal);
    }

    /// <summary>
    /// 绑定索引缓冲区
    /// </summary>
    /// <param name="buffer">缓冲区资源</param>
    /// <param name="offset">偏移量</param>
    public void SetIndexBuffer(RHI.IResource buffer, ulong offset = 0)
    {
        var vkResource = (VulkanResource)buffer;
        VulkanNative.vkCmdBindIndexBuffer(CommandBuffer, vkResource.BufferHandle, offset, VkIndexType.Uint32);
    }

    /// <summary>
    /// 清除渲染目标（Vulkan 中清除操作通过 BeginRenderPass 完成，此方法为空操作）
    /// </summary>
    /// <param name="attachmentIndex">附件索引</param>
    /// <param name="r">红色分量</param>
    /// <param name="g">绿色分量</param>
    /// <param name="b">蓝色分量</param>
    /// <param name="a">透明度分量</param>
    public void ClearRenderTarget(uint attachmentIndex, float r, float g, float b, float a)
    {
    }

    /// <summary>
    /// 清除深度模板（Vulkan 中清除操作通过 BeginRenderPass 完成，此方法为空操作）
    /// </summary>
    /// <param name="depth">深度值</param>
    /// <param name="stencil">模板值</param>
    public void ClearDepthStencil(float depth, byte stencil)
    {
    }

    /// <summary>
    /// 绑定描述符集
    /// </summary>
    /// <param name="descriptorSet">描述符集</param>
    /// <param name="setIndex">描述符集索引</param>
    public void BindDescriptorSet(RHI.IRhiDescriptorSet descriptorSet, uint setIndex)
    {
        var vkDescriptorSet = (VulkanDescriptorSet)descriptorSet;
        vkDescriptorSet.Flush();
        var descriptorSetHandle = vkDescriptorSet.Handle;
        var pipelineLayout = vkDescriptorSet.BoundPipelineLayout;
        var bindPoint = _currentPipeline?.BindPoint ?? VkPipelineBindPoint.Graphics;

        VulkanNative.vkCmdBindDescriptorSets(CommandBuffer, bindPoint, pipelineLayout, setIndex, 1, &descriptorSetHandle, 0, null);
    }

    /// <summary>
    /// 非索引绘制
    /// </summary>
    public void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
    {
        VulkanNative.vkCmdDraw(CommandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);
    }

    /// <summary>
    /// 索引绘制
    /// </summary>
    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
    {
        VulkanNative.vkCmdDrawIndexed(CommandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }

    /// <summary>
    /// 计算调度
    /// </summary>
    public void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
    {
        VulkanNative.vkCmdDispatch(CommandBuffer, groupCountX, groupCountY, groupCountZ);
    }

    /// <summary>
    /// 管线屏障，确保内存访问顺序
    /// </summary>
    public void PipelineBarrier(RHI.PipelineStageFlag srcStage, RHI.PipelineStageFlag dstStage, RHI.AccessFlag srcAccess, RHI.AccessFlag dstAccess)
    {
        VulkanNative.vkCmdPipelineBarrier(
            CommandBuffer,
            VulkanConversions.ToVkPipelineStage(srcStage),
            VulkanConversions.ToVkPipelineStage(dstStage),
            0,
            0, null,
            0, null,
            0, null);
    }

    /// <summary>
    /// 复制资源
    /// </summary>
    public void CopyResource(RHI.IResource src, RHI.IResource dst)
    {
        var vkSrc = (VulkanResource)src;
        var vkDst = (VulkanResource)dst;

        if (vkSrc.ResourceType == RHI.ResourceType.Buffer && vkDst.ResourceType == RHI.ResourceType.Buffer)
        {
            var copyRegion = new VkBufferCopy
            {
                SrcOffset = 0,
                DstOffset = 0,
                Size = Math.Min(vkSrc.Size, vkDst.Size)
            };

            VulkanNative.vkCmdCopyBuffer(CommandBuffer, vkSrc.BufferHandle, vkDst.BufferHandle, 1, &copyRegion);
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// 释放命令表
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        var cmdBuf = CommandBuffer;
        VulkanNative.vkFreeCommandBuffers(_device, _commandPool, 1, &cmdBuf);
    }

    #endregion
}

using System.Runtime.InteropServices;

namespace Gnosis.Rendering.Backends.Vulkan;

/// <summary>
/// Vulkan 原生 API 绑定
/// </summary>
public static unsafe partial class VulkanNative
{
    private const string LibraryName = "vulkan";

    #region 实例管理

    /// <summary>
    /// 创建 Vulkan 实例
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateInstance(VkInstanceCreateInfo* pCreateInfo, void* pAllocator, out VkInstance pInstance);

    /// <summary>
    /// 销毁 Vulkan 实例
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroyInstance(VkInstance instance, void* pAllocator);

    /// <summary>
    /// 枚举物理设备
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkEnumeratePhysicalDevices(VkInstance instance, uint* pPhysicalDeviceCount, VkPhysicalDevice* pPhysicalDevices);

    #endregion

    #region 设备管理

    /// <summary>
    /// 创建逻辑设备
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateDevice(VkPhysicalDevice physicalDevice, VkDeviceCreateInfo* pCreateInfo, void* pAllocator, out VkDevice pDevice);

    /// <summary>
    /// 销毁逻辑设备
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroyDevice(VkDevice device, void* pAllocator);

    /// <summary>
    /// 获取设备队列
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkGetDeviceQueue(VkDevice device, uint queueFamilyIndex, uint queueIndex, out VkQueue pQueue);

    /// <summary>
    /// 等待设备空闲
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkDeviceWaitIdle(VkDevice device);

    #endregion

    #region 内存管理

    /// <summary>
    /// 分配设备内存
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkAllocateMemory(VkDevice device, VkMemoryAllocateInfo* pAllocateInfo, void* pAllocator, out VkDeviceMemory pMemory);

    /// <summary>
    /// 释放设备内存
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkFreeMemory(VkDevice device, VkDeviceMemory memory, void* pAllocator);

    /// <summary>
    /// 映射设备内存
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkMapMemory(VkDevice device, VkDeviceMemory memory, ulong offset, ulong size, uint flags, out void* ppData);

    /// <summary>
    /// 取消映射设备内存
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkUnmapMemory(VkDevice device, VkDeviceMemory memory);

    /// <summary>
    /// 绑定缓冲区内存
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkBindBufferMemory(VkDevice device, VkBuffer buffer, VkDeviceMemory memory, ulong memoryOffset);

    /// <summary>
    /// 绑定图像内存
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkBindImageMemory(VkDevice device, VkImage image, VkDeviceMemory memory, ulong memoryOffset);

    #endregion

    #region 缓冲区

    /// <summary>
    /// 创建缓冲区
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateBuffer(VkDevice device, VkBufferCreateInfo* pCreateInfo, void* pAllocator, out VkBuffer pBuffer);

    /// <summary>
    /// 销毁缓冲区
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroyBuffer(VkDevice device, VkBuffer buffer, void* pAllocator);

    #endregion

    #region 图像

    /// <summary>
    /// 创建图像
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateImage(VkDevice device, VkImageCreateInfo* pCreateInfo, void* pAllocator, out VkImage pImage);

    /// <summary>
    /// 销毁图像
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroyImage(VkDevice device, VkImage image, void* pAllocator);

    /// <summary>
    /// 创建图像视图
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateImageView(VkDevice device, VkImageViewCreateInfo* pCreateInfo, void* pAllocator, out VkImageView pView);

    /// <summary>
    /// 销毁图像视图
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroyImageView(VkDevice device, VkImageView imageView, void* pAllocator);

    #endregion

    #region 采样器

    /// <summary>
    /// 创建采样器
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateSampler(VkDevice device, void* pCreateInfo, void* pAllocator, out VkSampler pSampler);

    /// <summary>
    /// 销毁采样器
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroySampler(VkDevice device, VkSampler sampler, void* pAllocator);

    #endregion

    #region 着色器

    /// <summary>
    /// 创建着色器模块
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateShaderModule(VkDevice device, VkShaderModuleCreateInfo* pCreateInfo, void* pAllocator, out VkShaderModule pShaderModule);

    /// <summary>
    /// 销毁着色器模块
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroyShaderModule(VkDevice device, VkShaderModule shaderModule, void* pAllocator);

    #endregion

    #region 描述符

    /// <summary>
    /// 创建描述符集布局
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateDescriptorSetLayout(VkDevice device, VkDescriptorSetLayoutCreateInfo* pCreateInfo, void* pAllocator, out VkDescriptorSetLayout pDescriptorSetLayout);

    /// <summary>
    /// 销毁描述符集布局
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroyDescriptorSetLayout(VkDevice device, VkDescriptorSetLayout descriptorSetLayout, void* pAllocator);

    /// <summary>
    /// 分配描述符集
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkAllocateDescriptorSets(VkDevice device, void* pAllocateInfo, out VkDescriptorSet pDescriptorSets);

    /// <summary>
    /// 更新描述符集
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkUpdateDescriptorSets(VkDevice device, uint descriptorWriteCount, VkWriteDescriptorSet* pDescriptorWrites, uint descriptorCopyCount, void* pDescriptorCopies);

    #endregion

    #region 管线

    /// <summary>
    /// 创建管线布局
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreatePipelineLayout(VkDevice device, VkPipelineLayoutCreateInfo* pCreateInfo, void* pAllocator, out VkPipelineLayout pPipelineLayout);

    /// <summary>
    /// 销毁管线布局
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroyPipelineLayout(VkDevice device, VkPipelineLayout pipelineLayout, void* pAllocator);

    /// <summary>
    /// 创建图形管线
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateGraphicsPipelines(VkDevice device, VkPipelineCache pipelineCache, uint createInfoCount, VkGraphicsPipelineCreateInfo* pCreateInfos, void* pAllocator, out VkPipeline pPipelines);

    /// <summary>
    /// 销毁管线
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroyPipeline(VkDevice device, VkPipeline pipeline, void* pAllocator);

    #endregion

    #region 渲染通道

    /// <summary>
    /// 创建渲染通道
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateRenderPass(VkDevice device, VkRenderPassCreateInfo* pCreateInfo, void* pAllocator, out VkRenderPass pRenderPass);

    /// <summary>
    /// 销毁渲染通道
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroyRenderPass(VkDevice device, VkRenderPass renderPass, void* pAllocator);

    /// <summary>
    /// 创建帧缓冲
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateFramebuffer(VkDevice device, VkFramebufferCreateInfo* pCreateInfo, void* pAllocator, out VkFramebuffer pFramebuffer);

    /// <summary>
    /// 销毁帧缓冲
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroyFramebuffer(VkDevice device, VkFramebuffer framebuffer, void* pAllocator);

    #endregion

    #region 命令缓冲

    /// <summary>
    /// 创建命令池
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateCommandPool(VkDevice device, VkCommandPoolCreateInfo* pCreateInfo, void* pAllocator, out VkCommandPool pCommandPool);

    /// <summary>
    /// 销毁命令池
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroyCommandPool(VkDevice device, VkCommandPool commandPool, void* pAllocator);

    /// <summary>
    /// 分配命令缓冲区
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkAllocateCommandBuffers(VkDevice device, VkCommandBufferAllocateInfo* pAllocateInfo, out VkCommandBuffer pCommandBuffers);

    /// <summary>
    /// 释放命令缓冲区
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkFreeCommandBuffers(VkDevice device, VkCommandPool commandPool, uint commandBufferCount, VkCommandBuffer* pCommandBuffers);

    /// <summary>
    /// 开始命令缓冲区录制
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkBeginCommandBuffer(VkCommandBuffer commandBuffer, VkCommandBufferBeginInfo* pBeginInfo);

    /// <summary>
    /// 结束命令缓冲区录制
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkEndCommandBuffer(VkCommandBuffer commandBuffer);

    /// <summary>
    /// 重置命令缓冲区
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkResetCommandBuffer(VkCommandBuffer commandBuffer, uint flags);

    #endregion

    #region 同步

    /// <summary>
    /// 创建栅栏
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateFence(VkDevice device, VkFenceCreateInfo* pCreateInfo, void* pAllocator, out VkFence pFence);

    /// <summary>
    /// 销毁栅栏
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroyFence(VkDevice device, VkFence fence, void* pAllocator);

    /// <summary>
    /// 等待栅栏
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkWaitForFences(VkDevice device, uint fenceCount, VkFence* pFences, bool waitAll, ulong timeout);

    /// <summary>
    /// 重置栅栏
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkResetFences(VkDevice device, uint fenceCount, VkFence* pFences);

    /// <summary>
    /// 创建信号量
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateSemaphore(VkDevice device, VkSemaphoreCreateInfo* pCreateInfo, void* pAllocator, out VkSemaphore pSemaphore);

    /// <summary>
    /// 销毁信号量
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroySemaphore(VkDevice device, VkSemaphore semaphore, void* pAllocator);

    #endregion

    #region 队列提交

    /// <summary>
    /// 提交队列
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkQueueSubmit(VkQueue queue, uint submitCount, VkSubmitInfo* pSubmits, VkFence fence);

    /// <summary>
    /// 等待队列空闲
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkQueueWaitIdle(VkQueue queue);

    #endregion

    #region 交换链

    /// <summary>
    /// 创建交换链
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateSwapchainKHR(VkDevice device, VkSwapchainCreateInfoKHR* pCreateInfo, void* pAllocator, out VkSwapchainKHR pSwapchain);

    /// <summary>
    /// 销毁交换链
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroySwapchainKHR(VkDevice device, VkSwapchainKHR swapchain, void* pAllocator);

    /// <summary>
    /// 获取交换链图像
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkGetSwapchainImagesKHR(VkDevice device, VkSwapchainKHR swapchain, uint* pSwapchainImageCount, VkImage* pSwapchainImages);

    /// <summary>
    /// 获取下一帧图像索引
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkAcquireNextImageKHR(VkDevice device, VkSwapchainKHR swapchain, ulong timeout, VkSemaphore semaphore, VkFence fence, out uint pImageIndex);

    /// <summary>
    /// 呈现队列
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkQueuePresentKHR(VkQueue queue, VkPresentInfoKHR* pPresentInfo);

    #endregion

    #region 命令录制

    /// <summary>
    /// 开始渲染通道
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkCmdBeginRenderPass(VkCommandBuffer commandBuffer, VkRenderPassBeginInfo* pRenderPassBegin, VkSubpassContents contents);

    /// <summary>
    /// 结束渲染通道
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkCmdEndRenderPass(VkCommandBuffer commandBuffer);

    /// <summary>
    /// 绑定管线
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkCmdBindPipeline(VkCommandBuffer commandBuffer, VkPipelineBindPoint pipelineBindPoint, VkPipeline pipeline);

    /// <summary>
    /// 绑定顶点缓冲区
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkCmdBindVertexBuffers(VkCommandBuffer commandBuffer, uint firstBinding, uint bindingCount, VkBuffer* pBuffers, ulong* pOffsets);

    /// <summary>
    /// 绑定索引缓冲区
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkCmdBindIndexBuffer(VkCommandBuffer commandBuffer, VkBuffer buffer, ulong offset, VkIndexType indexType);

    /// <summary>
    /// 绑定描述符集
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkCmdBindDescriptorSets(VkCommandBuffer commandBuffer, VkPipelineBindPoint pipelineBindPoint, VkPipelineLayout layout, uint firstSet, uint descriptorSetCount, VkDescriptorSet* pDescriptorSets, uint dynamicOffsetCount, uint* pDynamicOffsets);

    /// <summary>
    /// 绘制命令
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkCmdDraw(VkCommandBuffer commandBuffer, uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance);

    /// <summary>
    /// 索引绘制命令
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkCmdDrawIndexed(VkCommandBuffer commandBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance);

    /// <summary>
    /// 设置视口
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkCmdSetViewport(VkCommandBuffer commandBuffer, uint firstViewport, uint viewportCount, VkViewport* pViewports);

    /// <summary>
    /// 设置裁剪矩形
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkCmdSetScissor(VkCommandBuffer commandBuffer, uint firstScissor, uint scissorCount, VkRect2D* pScissors);

    /// <summary>
    /// 管线屏障
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkCmdPipelineBarrier(VkCommandBuffer commandBuffer, VkPipelineStageFlagBits srcStageMask, VkPipelineStageFlagBits dstStageMask, VkDependencyFlags dependencyFlags, uint memoryBarrierCount, void* pMemoryBarriers, uint bufferMemoryBarrierCount, void* pBufferMemoryBarriers, uint imageMemoryBarrierCount, void* pImageMemoryBarriers);

    /// <summary>
    /// 复制缓冲区
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkCmdCopyBuffer(VkCommandBuffer commandBuffer, VkBuffer srcBuffer, VkBuffer dstBuffer, uint regionCount, void* pRegions);

    /// <summary>
    /// 复制缓冲区到图像
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkCmdCopyBufferToImage(VkCommandBuffer commandBuffer, VkBuffer srcBuffer, VkImage dstImage, VkImageLayout dstImageLayout, uint regionCount, void* pRegions);

    #endregion

    #region 物理设备查询

    /// <summary>
    /// 获取物理设备属性
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkGetPhysicalDeviceProperties(VkPhysicalDevice physicalDevice, void* pProperties);

    /// <summary>
    /// 获取物理设备内存属性
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkGetPhysicalDeviceMemoryProperties(VkPhysicalDevice physicalDevice, void* pMemoryProperties);

    /// <summary>
    /// 获取物理设备队列族属性
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkGetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice physicalDevice, uint* pQueueFamilyPropertyCount, void* pQueueFamilyProperties);

    /// <summary>
    /// 获取缓冲区内存需求
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkGetBufferMemoryRequirements(VkDevice device, VkBuffer buffer, void* pMemoryRequirements);

    /// <summary>
    /// 获取图像内存需求
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkGetImageMemoryRequirements(VkDevice device, VkImage image, void* pMemoryRequirements);

    #endregion

    #region 辅助方法

    /// <summary>
    /// 检查 Vulkan 操作结果是否成功
    /// </summary>
    /// <param name="result">操作结果</param>
    /// <param name="operation">操作名称</param>
    /// <exception cref="InvalidOperationException">操作失败时抛出</exception>
    public static void CheckResult(VkResult result, string operation)
    {
        if (result != VkResult.Success && result != VkResult.SuboptimalKHR)
        {
            throw new InvalidOperationException($"Vulkan 操作失败：{operation}，结果：{result}");
        }
    }

    #endregion
}

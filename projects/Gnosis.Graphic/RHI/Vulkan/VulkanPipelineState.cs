using System.Runtime.InteropServices;
using Gnosis.Graphic.Shader;

namespace Gnosis.Graphic.RHI.Vulkan;

/// <summary>
/// Vulkan 管线状态实现
/// </summary>
internal sealed unsafe class VulkanPipelineState : RHI.IPipelineState
{
    #region IPipelineState 属性

    /// <summary>
    /// 混合模式
    /// </summary>
    public RHI.BlendMode BlendMode => _desc.BlendMode;

    /// <summary>
    /// 深度测试启用
    /// </summary>
    public bool DepthTest => _desc.DepthTest;

    /// <summary>
    /// 深度写入启用
    /// </summary>
    public bool DepthWrite => _desc.DepthWrite;

    /// <summary>
    /// 深度比较函数
    /// </summary>
    public RHI.CompareFunction DepthCompare => _desc.DepthCompare;

    /// <summary>
    /// 剔除模式
    /// </summary>
    public RHI.CullMode CullMode => _desc.CullMode;

    /// <summary>
    /// 正面朝向
    /// </summary>
    public RHI.FrontFace FrontFace => _desc.FrontFace;

    /// <summary>
    /// 多边形模式
    /// </summary>
    public RHI.PolygonMode PolygonMode => _desc.PolygonMode;

    /// <summary>
    /// 拓扑类型
    /// </summary>
    public RHI.PrimitiveTopology Topology => _desc.Topology;

    /// <summary>
    /// 着色器句柄
    /// </summary>
    public ulong ShaderHandle => _desc.ShaderHandle;

    #endregion

    #region Vulkan 句柄

    /// <summary>
    /// Vulkan 管线句柄
    /// </summary>
    public VkPipeline Pipeline => _pipeline;

    /// <summary>
    /// Vulkan 管线布局句柄
    /// </summary>
    public VkPipelineLayout PipelineLayout => _pipelineLayout;

    #endregion

    #region 内部状态

    private readonly RHI.PipelineStateDesc _desc;
    private readonly VulkanDevice _device;
    private VkPipeline _pipeline;
    private VkPipelineLayout _pipelineLayout;
    private bool _isDisposed;
    private bool _pipelineCreated;

    #endregion

    /// <summary>
    /// 创建 Vulkan 管线状态
    /// </summary>
    /// <param name="device">Vulkan 设备</param>
    /// <param name="desc">管线状态描述</param>
    public VulkanPipelineState(VulkanDevice device, in RHI.PipelineStateDesc desc)
    {
        _device = device;
        _desc = desc;
        _pipeline = VkPipeline.Null;
        _pipelineLayout = VkPipelineLayout.Null;
    }

    /// <summary>
    /// 确保管线已创建，延迟创建管线对象
    /// </summary>
    public void EnsurePipelineCreated()
    {
        if (_pipelineCreated)
        {
            return;
        }

        _pipelineCreated = true;
        CreatePipeline();
    }

    #region 管线创建

    /// <summary>
    /// 创建 Vulkan 管线
    /// </summary>
    private void CreatePipeline()
    {
        var vkDevice = _device.LogicalDevice;

        CreatePipelineLayout(vkDevice);
        CreateGraphicsPipeline(vkDevice);
    }

    /// <summary>
    /// 创建管线布局
    /// </summary>
    private void CreatePipelineLayout(VkDevice vkDevice)
    {
        var layoutCreateInfo = new VkPipelineLayoutCreateInfo
        {
            SType = VkStructureType.PipelineLayoutCreateInfo,
            PNext = null,
            Flags = 0,
            SetLayoutCount = 0,
            PSetLayouts = null,
            PushConstantRangeCount = 0,
            PPushConstantRanges = null
        };

        VulkanNative.CheckResult(
            VulkanNative.vkCreatePipelineLayout(vkDevice, &layoutCreateInfo, null, out _pipelineLayout),
            "创建管线布局");
    }

    /// <summary>
    /// 创建图形管线
    /// </summary>
    private void CreateGraphicsPipeline(VkDevice vkDevice)
    {
        var shaderResource = _device.GetResource(_desc.ShaderHandle) as VulkanResource;
        if (shaderResource == null || shaderResource.ShaderModuleHandle.IsNull)
        {
            throw new InvalidOperationException($"未找到着色器资源：{_desc.ShaderHandle}");
        }

        var pEntryPoint = (byte*)Marshal.StringToHGlobalAnsi(shaderResource.EntryPoint);

        var stageCreateInfo = new VkPipelineShaderStageCreateInfo
        {
            SType = VkStructureType.PipelineShaderStageCreateInfo,
            PNext = null,
            Flags = 0,
            Stage = VulkanConversions.ToVkShaderStage(shaderResource.ShaderStage),
            Module = shaderResource.ShaderModuleHandle,
            PName = pEntryPoint,
            PSpecializationInfo = null
        };

        var vertexInputState = new VkPipelineVertexInputStateCreateInfo
        {
            SType = VkStructureType.PipelineVertexInputStateCreateInfo,
            PNext = null,
            Flags = 0,
            VertexBindingDescriptionCount = 0,
            PVertexBindingDescriptions = null,
            VertexAttributeDescriptionCount = 0,
            PVertexAttributeDescriptions = null
        };

        var inputAssemblyState = new VkPipelineInputAssemblyStateCreateInfo
        {
            SType = VkStructureType.PipelineInputAssemblyStateCreateInfo,
            PNext = null,
            Flags = 0,
            Topology = VulkanConversions.ToVkPrimitiveTopology(_desc.Topology),
            PrimitiveRestartEnable = 0
        };

        var viewport = new VkViewport
        {
            X = 0.0f,
            Y = 0.0f,
            Width = 1.0f,
            Height = 1.0f,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };

        var scissor = new VkRect2D
        {
            Offset = new VkOffset2D { X = 0, Y = 0 },
            Extent = new VkExtent2D { Width = 1, Height = 1 }
        };

        var viewportState = new VkPipelineViewportStateCreateInfo
        {
            SType = VkStructureType.PipelineViewportStateCreateInfo,
            PNext = null,
            Flags = 0,
            ViewportCount = 1,
            PViewports = &viewport,
            ScissorCount = 1,
            PScissors = &scissor
        };

        var rasterizationState = new VkPipelineRasterizationStateCreateInfo
        {
            SType = VkStructureType.PipelineRasterizationStateCreateInfo,
            PNext = null,
            Flags = 0,
            DepthClampEnable = 0,
            RasterizerDiscardEnable = 0,
            PolygonMode = VulkanConversions.ToVkPolygonMode(_desc.PolygonMode),
            CullMode = VulkanConversions.ToVkCullMode(_desc.CullMode),
            FrontFace = VulkanConversions.ToVkFrontFace(_desc.FrontFace),
            DepthBiasEnable = 0,
            DepthBiasConstantFactor = 0.0f,
            DepthBiasClamp = 0.0f,
            DepthBiasSlopeFactor = 0.0f,
            LineWidth = _desc.LineWidth
        };

        var multisampleState = new VkPipelineMultisampleStateCreateInfo
        {
            SType = VkStructureType.PipelineMultisampleStateCreateInfo,
            PNext = null,
            Flags = 0,
            RasterizationSamples = VulkanConversions.ToVkSampleCount(_desc.SampleCount),
            SampleShadingEnable = 0,
            MinSampleShading = 1.0f,
            PSampleMask = null,
            AlphaToCoverageEnable = 0,
            AlphaToOneEnable = 0
        };

        var depthStencilState = new VkPipelineDepthStencilStateCreateInfo
        {
            SType = VkStructureType.PipelineDepthStencilStateCreateInfo,
            PNext = null,
            Flags = 0,
            DepthTestEnable = _desc.DepthTest ? 1u : 0u,
            DepthWriteEnable = _desc.DepthWrite ? 1u : 0u,
            DepthCompareOp = VulkanConversions.ToVkCompareOp(_desc.DepthCompare),
            DepthBoundsTestEnable = 0,
            StencilTestEnable = 0,
            Front = VulkanConversions.ToVkStencilOpState(_desc.StencilFront),
            Back = VulkanConversions.ToVkStencilOpState(_desc.StencilBack),
            MinDepthBounds = 0.0f,
            MaxDepthBounds = 1.0f
        };

        var blendAttachment = CreateBlendAttachmentState();
        var colorBlendState = new VkPipelineColorBlendStateCreateInfo
        {
            SType = VkStructureType.PipelineColorBlendStateCreateInfo,
            PNext = null,
            Flags = 0,
            LogicOpEnable = 0,
            LogicOp = VkLogicOp.Copy,
            AttachmentCount = Math.Max(_desc.ColorAttachmentCount, 1),
            PAttachments = &blendAttachment,
            BlendConstants0 = 0.0f,
            BlendConstants1 = 0.0f,
            BlendConstants2 = 0.0f,
            BlendConstants3 = 0.0f
        };

        var pipelineCreateInfo = new VkGraphicsPipelineCreateInfo
        {
            SType = VkStructureType.GraphicsPipelineCreateInfo,
            PNext = null,
            Flags = 0,
            StageCount = 1,
            PStages = &stageCreateInfo,
            PVertexInputState = &vertexInputState,
            PInputAssemblyState = &inputAssemblyState,
            PTessellationState = null,
            PViewportState = &viewportState,
            PRasterizationState = &rasterizationState,
            PMultisampleState = &multisampleState,
            PDepthStencilState = &depthStencilState,
            PColorBlendState = &colorBlendState,
            PDynamicState = null,
            Layout = _pipelineLayout,
            RenderPass = VkRenderPass.Null,
            Subpass = 0,
            BasePipelineHandle = VkPipeline.Null,
            BasePipelineIndex = -1
        };

        VulkanNative.CheckResult(
            VulkanNative.vkCreateGraphicsPipelines(vkDevice, VkPipelineCache.Null, 1, &pipelineCreateInfo, null, out _pipeline),
            "创建图形管线");

        Marshal.FreeHGlobal((nint)pEntryPoint);
    }

    /// <summary>
    /// 创建颜色混合附件状态
    /// </summary>
    private VkPipelineColorBlendAttachmentState CreateBlendAttachmentState()
    {
        if (_desc.BlendStates.Length > 0)
        {
            var bs = _desc.BlendStates[0];
            return new VkPipelineColorBlendAttachmentState
            {
                BlendEnable = bs.BlendEnable ? 1u : 0u,
                SrcColorBlendFactor = VulkanConversions.ToVkBlendFactor(bs.SrcBlend),
                DstColorBlendFactor = VulkanConversions.ToVkBlendFactor(bs.DstBlend),
                ColorBlendOp = VulkanConversions.ToVkBlendOp(bs.BlendOp),
                SrcAlphaBlendFactor = VulkanConversions.ToVkBlendFactor(bs.SrcAlphaBlend),
                DstAlphaBlendFactor = VulkanConversions.ToVkBlendFactor(bs.DstAlphaBlend),
                AlphaBlendOp = VulkanConversions.ToVkBlendOp(bs.AlphaBlendOp),
                ColorWriteMask = VulkanConversions.ToVkColorComponentFlags(bs.ColorWriteMask)
            };
        }

        return VulkanConversions.CreateBlendAttachmentFromMode(_desc.BlendMode);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// 释放管线状态
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        var vkDevice = _device.LogicalDevice;

        if (!_pipeline.IsNull)
        {
            VulkanNative.vkDestroyPipeline(vkDevice, _pipeline, null);
        }

        if (!_pipelineLayout.IsNull)
        {
            VulkanNative.vkDestroyPipelineLayout(vkDevice, _pipelineLayout, null);
        }
    }

    #endregion
}

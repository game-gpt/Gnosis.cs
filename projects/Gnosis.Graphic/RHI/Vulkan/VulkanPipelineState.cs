using System.Runtime.InteropServices;
using Gnosis.Graphic.Shader;

namespace Gnosis.Graphic.RHI.Vulkan;

internal sealed unsafe class VulkanPipelineState : RHI.IPipelineState
{
    #region IPipelineState 属性

    public RHI.BlendMode BlendMode => _desc.BlendMode;

    public bool DepthTest => _desc.DepthTest;

    public bool DepthWrite => _desc.DepthWrite;

    public RHI.CompareFunction DepthCompare => _desc.DepthCompare;

    public RHI.CullMode CullMode => _desc.CullMode;

    public RHI.FrontFace FrontFace => _desc.FrontFace;

    public RHI.PolygonMode PolygonMode => _desc.PolygonMode;

    public RHI.PrimitiveTopology Topology => _desc.Topology;

    public RHI.IShaderProgram Shader => _desc.Shader;

    #endregion

    #region Vulkan 句柄

    public VkPipeline Pipeline => _pipeline;

    public VkPipelineLayout PipelineLayout => _pipelineLayout;

    public VkPipelineBindPoint BindPoint => _desc.PipelineType == RHI.PipelineType.Compute
        ? VkPipelineBindPoint.Compute
        : VkPipelineBindPoint.Graphics;

    #endregion

    #region 内部状态

    private readonly RHI.PipelineStateDesc _desc;
    private readonly VulkanDevice _device;
    private VkPipeline _pipeline;
    private VkPipelineLayout _pipelineLayout;
    private VkDescriptorSetLayout _descriptorSetLayout;
    private bool _isDisposed;
    private bool _pipelineCreated;

    #endregion

    public VulkanPipelineState(VulkanDevice device, in RHI.PipelineStateDesc desc)
    {
        _device = device;
        _desc = desc;
        _pipeline = VkPipeline.Null;
        _pipelineLayout = VkPipelineLayout.Null;
        _descriptorSetLayout = VkDescriptorSetLayout.Null;
    }

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

    private void CreatePipeline()
    {
        var vkDevice = _device.LogicalDevice;

        CreateDescriptorSetLayout(vkDevice);
        CreatePipelineLayout(vkDevice);
        CreateGraphicsPipeline(vkDevice);
    }

    private void CreateDescriptorSetLayout(VkDevice vkDevice)
    {
        if (_desc.ShaderResources is not { Length: > 0 })
        {
            return;
        }

        var bindings = new List<VkDescriptorSetLayoutBinding>();

        for (uint i = 0; i < _desc.ShaderResources.Length; i++)
        {
            if (_desc.ShaderResources[i] is not VulkanResource resource)
            {
                continue;
            }

            var descriptorType = resource.ShaderStage == ShaderStage.Compute
                ? VkDescriptorType.StorageBuffer
                : VkDescriptorType.UniformBuffer;

            bindings.Add(new VkDescriptorSetLayoutBinding
            {
                Binding = i,
                DescriptorType = descriptorType,
                DescriptorCount = 1,
                StageFlags = VulkanConversions.ToVkShaderStage(resource.ShaderStage),
                PImmutableSamplers = null
            });
        }

        if (bindings.Count == 0)
        {
            return;
        }

        fixed (VkDescriptorSetLayoutBinding* pBindings = bindings.ToArray())
        {
            var layoutCreateInfo = new VkDescriptorSetLayoutCreateInfo
            {
                SType = VkStructureType.DescriptorSetLayoutCreateInfo,
                PNext = null,
                Flags = 0,
                BindingCount = (uint)bindings.Count,
                PBindings = pBindings
            };

            VulkanNative.CheckResult(
                VulkanNative.vkCreateDescriptorSetLayout(vkDevice, &layoutCreateInfo, null, out _descriptorSetLayout),
                "创建描述符集布局");
        }
    }

    private void CreatePipelineLayout(VkDevice vkDevice)
    {
        VkDescriptorSetLayout* pSetLayouts = null;
        uint setLayoutCount = 0;

        var layoutCreateInfo = new VkPipelineLayoutCreateInfo
        {
            SType = VkStructureType.PipelineLayoutCreateInfo,
            PNext = null,
            Flags = 0,
            SetLayoutCount = setLayoutCount,
            PSetLayouts = pSetLayouts,
            PushConstantRangeCount = 0,
            PPushConstantRanges = null
        };

        if (!_descriptorSetLayout.IsNull)
        {
            fixed (VkDescriptorSetLayout* pLayout = &_descriptorSetLayout)
            {
                layoutCreateInfo.SetLayoutCount = 1;
                layoutCreateInfo.PSetLayouts = pLayout;

                VulkanNative.CheckResult(
                    VulkanNative.vkCreatePipelineLayout(vkDevice, &layoutCreateInfo, null, out _pipelineLayout),
                    "创建管线布局");
            }
        }
        else
        {
            VulkanNative.CheckResult(
                VulkanNative.vkCreatePipelineLayout(vkDevice, &layoutCreateInfo, null, out _pipelineLayout),
                "创建管线布局");
        }
    }

    private void CreateGraphicsPipeline(VkDevice vkDevice)
    {
        var shaderStages = BuildShaderStages();
        var entryPointHandles = new List<nint>();

        var dynamicStates = stackalloc VkDynamicState[2];
        dynamicStates[0] = VkDynamicState.Viewport;
        dynamicStates[1] = VkDynamicState.Scissor;

        var dynamicState = new VkPipelineDynamicStateCreateInfo
        {
            SType = VkStructureType.PipelineDynamicStateCreateInfo,
            PNext = null,
            Flags = 0,
            DynamicStateCount = 2,
            PDynamicStates = dynamicStates
        };

        fixed (VkPipelineShaderStageCreateInfo* pStages = shaderStages)
        {
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

            var viewportState = new VkPipelineViewportStateCreateInfo
            {
                SType = VkStructureType.PipelineViewportStateCreateInfo,
                PNext = null,
                Flags = 0,
                ViewportCount = 1,
                PViewports = null,
                ScissorCount = 1,
                PScissors = null
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
                StageCount = (uint)shaderStages.Length,
                PStages = pStages,
                PVertexInputState = &vertexInputState,
                PInputAssemblyState = &inputAssemblyState,
                PTessellationState = null,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizationState,
                PMultisampleState = &multisampleState,
                PDepthStencilState = &depthStencilState,
                PColorBlendState = &colorBlendState,
                PDynamicState = &dynamicState,
                Layout = _pipelineLayout,
                RenderPass = _device.GetCompatibleRenderPass(),
                Subpass = 0,
                BasePipelineHandle = VkPipeline.Null,
                BasePipelineIndex = -1
            };

            VulkanNative.CheckResult(
                VulkanNative.vkCreateGraphicsPipelines(vkDevice, VkPipelineCache.Null, 1, &pipelineCreateInfo, null, out _pipeline),
                "创建图形管线");
        }

        foreach (var handle in entryPointHandles)
        {
            Marshal.FreeHGlobal(handle);
        }
    }

    private VkPipelineShaderStageCreateInfo[] BuildShaderStages()
    {
        var stages = new List<VkPipelineShaderStageCreateInfo>();

        if (_desc.ShaderResources is { Length: > 0 })
        {
            foreach (var resource in _desc.ShaderResources)
            {
                if (resource is not VulkanResource shaderResource || shaderResource.ShaderModuleHandle.IsNull)
                {
                    continue;
                }

                var pEntryPoint = (byte*)Marshal.StringToHGlobalAnsi(shaderResource.EntryPoint);

                stages.Add(new VkPipelineShaderStageCreateInfo
                {
                    SType = VkStructureType.PipelineShaderStageCreateInfo,
                    PNext = null,
                    Flags = 0,
                    Stage = VulkanConversions.ToVkShaderStage(shaderResource.ShaderStage),
                    Module = shaderResource.ShaderModuleHandle,
                    PName = pEntryPoint,
                    PSpecializationInfo = null
                });
            }
        }

        if (stages.Count == 0)
        {
            throw new InvalidOperationException("未找到着色器资源");
        }

        return stages.ToArray();
    }

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

        if (!_descriptorSetLayout.IsNull)
        {
            VulkanNative.vkDestroyDescriptorSetLayout(vkDevice, _descriptorSetLayout, null);
        }
    }

    #endregion
}

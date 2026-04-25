using System.Runtime.InteropServices;

namespace Gnosis.Graphic.RHI.Vulkan;

#region Surface 辅助结构

/// <summary>
/// Win32 表面创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VkWin32SurfaceCreateInfoKHR
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public nint Hinstance;
    public nint Hwnd;
}

/// <summary>
/// 表面能力
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkSurfaceCapabilitiesKHR
{
    public uint MinImageCount;
    public uint MaxImageCount;
    public VkExtent2D CurrentExtent;
    public VkExtent2D MinImageExtent;
    public VkExtent2D MaxImageExtent;
    public uint MaxImageArrayLayers;
    public VkSurfaceTransformFlagBitsKHR SupportedTransforms;
    public VkSurfaceTransformFlagBitsKHR CurrentTransform;
    public VkCompositeAlphaFlagBitsKHR SupportedCompositeAlpha;
    public VkImageUsageFlagBits SupportedUsageFlags;
}

/// <summary>
/// 表面格式
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkSurfaceFormatKHR
{
    public VkFormat Format;
    public VkColorSpaceKHR ColorSpace;
}

#endregion

/// <summary>
/// Vulkan 交换链 Surface 原生 API
/// </summary>
internal static unsafe class VulkanSurfaceNative
{
    private const string LibraryName = "vulkan";

    /// <summary>
    /// 创建 Win32 表面
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateWin32SurfaceKHR(VkInstance instance, VkWin32SurfaceCreateInfoKHR* pCreateInfo, void* pAllocator, out VkSurfaceKHR pSurface);

    /// <summary>
    /// 销毁表面
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroySurfaceKHR(VkInstance instance, VkSurfaceKHR surface, void* pAllocator);

    /// <summary>
    /// 获取物理设备表面支持
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkGetPhysicalDeviceSurfaceSupportKHR(VkPhysicalDevice physicalDevice, uint queueFamilyIndex, VkSurfaceKHR surface, out uint pSupported);

    /// <summary>
    /// 获取物理设备表面能力
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkGetPhysicalDeviceSurfaceCapabilitiesKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, out VkSurfaceCapabilitiesKHR pSurfaceCapabilities);

    /// <summary>
    /// 获取物理设备表面格式
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkGetPhysicalDeviceSurfaceFormatsKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, uint* pSurfaceFormatCount, VkSurfaceFormatKHR* pSurfaceFormats);

    /// <summary>
    /// 获取物理设备表面呈现模式
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkGetPhysicalDeviceSurfacePresentModesKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, uint* pPresentModeCount, VkPresentModeKHR* pPresentModes);
}

/// <summary>
/// Vulkan 交换链实现
/// </summary>
internal sealed unsafe class VulkanSwapchain : RHI.IRhiSwapchain
{
    #region IRhiSwapchain 属性

    /// <summary>
    /// 交换链宽度
    /// </summary>
    public uint Width { get; private set; }

    /// <summary>
    /// 交换链高度
    /// </summary>
    public uint Height { get; private set; }

    /// <summary>
    /// 交换链图像格式
    /// </summary>
    public RHI.ResourceFormat Format { get; private set; }

    /// <summary>
    /// 交换链图像数量
    /// </summary>
    public uint ImageCount => (uint)_imageViews.Count;

    #endregion

    #region Vulkan 句柄

    /// <summary>
    /// Vulkan 交换链句柄
    /// </summary>
    public VkSwapchainKHR Handle { get; private set; }

    /// <summary>
    /// Vulkan 表面句柄
    /// </summary>
    public VkSurfaceKHR Surface { get; private set; }

    /// <summary>
    /// 交换链图像视图列表
    /// </summary>
    public IReadOnlyList<VkImageView> ImageViews => _imageViews;

    #endregion

    #region 内部状态

    private readonly VkInstance _instance;
    private readonly VkPhysicalDevice _physicalDevice;
    private readonly VkDevice _device;
    private readonly VkQueue _queue;
    private readonly uint _queueFamilyIndex;
    private readonly List<VkImage> _images = new();
    private readonly List<VkImageView> _imageViews = new();
    private bool _isDisposed;
    private uint _currentImageIndex;
    private nint _windowHandle;

    #endregion

    /// <summary>
    /// 创建 Vulkan 交换链
    /// </summary>
    /// <param name="instance">Vulkan 实例</param>
    /// <param name="physicalDevice">物理设备</param>
    /// <param name="device">逻辑设备</param>
    /// <param name="queue">呈现队列</param>
    /// <param name="queueFamilyIndex">队列族索引</param>
    /// <param name="desc">交换链描述</param>
    public VulkanSwapchain(VkInstance instance, VkPhysicalDevice physicalDevice, VkDevice device, VkQueue queue, uint queueFamilyIndex, in RHI.SwapchainDesc desc)
    {
        _instance = instance;
        _physicalDevice = physicalDevice;
        _device = device;
        _queue = queue;
        _queueFamilyIndex = queueFamilyIndex;
        _windowHandle = desc.WindowHandle;
        Width = desc.Width;
        Height = desc.Height;
        Format = desc.Format;

        CreateSurface(desc.WindowHandle);
        CreateSwapchain(desc);
    }

    #region Surface 创建

    /// <summary>
    /// 创建 Vulkan 表面
    /// </summary>
    private void CreateSurface(nint windowHandle)
    {
        var createInfo = new VkWin32SurfaceCreateInfoKHR
        {
            SType = VkStructureType.Win32SurfaceCreateInfoKHR,
            PNext = null,
            Flags = 0,
            Hinstance = Marshal.GetHINSTANCE(typeof(VulkanSwapchain).Module),
            Hwnd = windowHandle
        };

        VulkanNative.CheckResult(
            VulkanSurfaceNative.vkCreateWin32SurfaceKHR(_instance, &createInfo, null, out var surface),
            "创建 Win32 表面");

        Surface = surface;

        VulkanSurfaceNative.vkGetPhysicalDeviceSurfaceSupportKHR(_physicalDevice, _queueFamilyIndex, Surface, out var supported);
        if (supported == 0)
        {
            throw new InvalidOperationException("队列族不支持呈现");
        }
    }

    #endregion

    #region 交换链创建

    /// <summary>
    /// 创建交换链
    /// </summary>
    private void CreateSwapchain(in RHI.SwapchainDesc desc)
    {
        VulkanSurfaceNative.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(_physicalDevice, Surface, out var capabilities);

        var imageCount = Math.Max(desc.ImageCount, capabilities.MinImageCount);
        if (capabilities.MaxImageCount > 0)
        {
            imageCount = Math.Min(imageCount, capabilities.MaxImageCount);
        }

        var extent = ChooseExtent(capabilities, desc.Width, desc.Height);

        var vkFormat = VulkanConversions.ToVkFormat(desc.Format);
        var colorSpace = VkColorSpaceKHR.SrgbNonlinear;

        uint formatCount = 0;
        VulkanSurfaceNative.vkGetPhysicalDeviceSurfaceFormatsKHR(_physicalDevice, Surface, &formatCount, null);
        if (formatCount > 0)
        {
            var formats = stackalloc VkSurfaceFormatKHR[(int)formatCount];
            VulkanSurfaceNative.vkGetPhysicalDeviceSurfaceFormatsKHR(_physicalDevice, Surface, &formatCount, formats);

            for (var i = 0; i < (int)formatCount; i++)
            {
                if (formats[i].Format == vkFormat && formats[i].ColorSpace == colorSpace)
                {
                    break;
                }
            }
        }

        var presentMode = ChoosePresentMode(desc.PresentMode);

        var createInfo = new VkSwapchainCreateInfoKHR
        {
            SType = VkStructureType.SwapchainCreateInfoKHR,
            PNext = null,
            Flags = 0,
            Surface = Surface,
            MinImageCount = imageCount,
            ImageFormat = vkFormat,
            ImageColorSpace = colorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = VkImageUsageFlagBits.ColorAttachment,
            ImageSharingMode = VkSharingMode.Exclusive,
            QueueFamilyIndexCount = 0,
            PQueueFamilyIndices = null,
            PreTransform = capabilities.CurrentTransform,
            CompositeAlpha = VkCompositeAlphaFlagBitsKHR.Opaque,
            PresentMode = presentMode,
            Clipped = 1,
            OldSwapchain = VkSwapchainKHR.Null
        };

        VulkanNative.CheckResult(
            VulkanNative.vkCreateSwapchainKHR(_device, &createInfo, null, out var swapchain),
            "创建交换链");

        Handle = swapchain;

        RetrieveImages();
        CreateImageViews(vkFormat);

        Width = extent.Width;
        Height = extent.Height;
    }

    /// <summary>
    /// 选择交换链范围
    /// </summary>
    private static VkExtent2D ChooseExtent(in VkSurfaceCapabilitiesKHR capabilities, uint width, uint height)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            return capabilities.CurrentExtent;
        }

        return new VkExtent2D
        {
            Width = Math.Clamp(width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width),
            Height = Math.Clamp(height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height)
        };
    }

    /// <summary>
    /// 选择呈现模式
    /// </summary>
    private VkPresentModeKHR ChoosePresentMode(RHI.PresentMode preferredMode)
    {
        uint modeCount = 0;
        VulkanSurfaceNative.vkGetPhysicalDeviceSurfacePresentModesKHR(_physicalDevice, Surface, &modeCount, null);

        if (modeCount == 0)
        {
            return VkPresentModeKHR.Fifo;
        }

        var modes = stackalloc VkPresentModeKHR[(int)modeCount];
        VulkanSurfaceNative.vkGetPhysicalDeviceSurfacePresentModesKHR(_physicalDevice, Surface, &modeCount, modes);

        var targetMode = VulkanConversions.ToVkPresentMode(preferredMode);

        for (var i = 0; i < (int)modeCount; i++)
        {
            if (modes[i] == targetMode)
            {
                return targetMode;
            }
        }

        return VkPresentModeKHR.Fifo;
    }

    /// <summary>
    /// 获取交换链图像
    /// </summary>
    private void RetrieveImages()
    {
        uint imageCount = 0;
        VulkanNative.vkGetSwapchainImagesKHR(_device, Handle, &imageCount, null);

        var images = stackalloc VkImage[(int)imageCount];
        VulkanNative.vkGetSwapchainImagesKHR(_device, Handle, &imageCount, images);

        _images.Clear();
        for (var i = 0; i < (int)imageCount; i++)
        {
            _images.Add(images[i]);
        }
    }

    /// <summary>
    /// 创建图像视图
    /// </summary>
    private void CreateImageViews(VkFormat format)
    {
        DestroyImageViews();

        foreach (var image in _images)
        {
            var createInfo = new VkImageViewCreateInfo
            {
                SType = VkStructureType.ImageViewCreateInfo,
                PNext = null,
                Image = image,
                ViewType = VkImageViewType._2D,
                Format = format,
                Components = new VkComponentMapping
                {
                    R = VkComponentSwizzle.Identity,
                    G = VkComponentSwizzle.Identity,
                    B = VkComponentSwizzle.Identity,
                    A = VkComponentSwizzle.Identity
                },
                SubresourceRange = new VkImageSubresourceRange
                {
                    AspectMask = VkImageAspectFlagBits.Color,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                }
            };

            VulkanNative.CheckResult(
                VulkanNative.vkCreateImageView(_device, &createInfo, null, out var imageView),
                "创建交换链图像视图");

            _imageViews.Add(imageView);
        }
    }

    /// <summary>
    /// 销毁图像视图
    /// </summary>
    private void DestroyImageViews()
    {
        foreach (var imageView in _imageViews)
        {
            if (!imageView.IsNull)
            {
                VulkanNative.vkDestroyImageView(_device, imageView, null);
            }
        }

        _imageViews.Clear();
    }

    #endregion

    #region IRhiSwapchain 实现

    /// <summary>
    /// 获取下一帧可呈现图像的索引
    /// </summary>
    /// <param name="semaphore">信号量</param>
    /// <param name="fence">围栏</param>
    /// <returns>下一帧图像索引</returns>
    public uint AcquireNextImage(RHI.IRhiSemaphore? semaphore, RHI.IRhiFence? fence)
    {
        var vkSemaphore = semaphore != null ? ((VulkanSemaphore)semaphore).Handle : VkSemaphore.Null;
        var vkFence = fence != null ? ((VulkanFence)fence).Handle : VkFence.Null;

        var result = VulkanNative.vkAcquireNextImageKHR(_device, Handle, ulong.MaxValue, vkSemaphore, vkFence, out _currentImageIndex);

        if (result == VkResult.ErrorOutOfDateKHR)
        {
            return 0;
        }

        VulkanNative.CheckResult(result, "获取下一帧图像");

        return _currentImageIndex;
    }

    /// <summary>
    /// 呈现当前帧图像
    /// </summary>
    /// <param name="waitSemaphores">等待的信号量列表</param>
    public void Present(IReadOnlyList<RHI.IRhiSemaphore> waitSemaphores)
    {
        var semaphoreHandles = stackalloc VkSemaphore[waitSemaphores.Count];

        for (var i = 0; i < waitSemaphores.Count; i++)
        {
            semaphoreHandles[i] = ((VulkanSemaphore)waitSemaphores[i]).Handle;
        }

        var swapchain = Handle;
        var imageIndex = _currentImageIndex;

        var presentInfo = new VkPresentInfoKHR
        {
            SType = VkStructureType.PresentInfoKHR,
            PNext = null,
            WaitSemaphoreCount = (uint)waitSemaphores.Count,
            PWaitSemaphores = semaphoreHandles,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &imageIndex,
            PResults = null
        };

        VulkanNative.CheckResult(
            VulkanNative.vkQueuePresentKHR(_queue, &presentInfo),
            "呈现队列");
    }

    /// <summary>
    /// 调整交换链尺寸
    /// </summary>
    /// <param name="width">新宽度</param>
    /// <param name="height">新高度</param>
    public void Resize(uint width, uint height)
    {
        VulkanNative.vkDeviceWaitIdle(_device);

        DestroyImageViews();

        if (!Handle.IsNull)
        {
            VulkanNative.vkDestroySwapchainKHR(_device, Handle, null);
        }

        var desc = new RHI.SwapchainDesc
        {
            WindowHandle = _windowHandle,
            Width = width,
            Height = height,
            Format = Format
        };

        CreateSwapchain(desc);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// 释放交换链
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        DestroyImageViews();

        if (!Handle.IsNull)
        {
            VulkanNative.vkDestroySwapchainKHR(_device, Handle, null);
        }

        if (!Surface.IsNull)
        {
            VulkanSurfaceNative.vkDestroySurfaceKHR(_instance, Surface, null);
        }
    }

    #endregion
}

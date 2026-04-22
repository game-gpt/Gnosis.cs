using Gnosis.Core.Math;
using Gnosis.Graphic.Light;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Shadow;

public sealed class CascadedShadowMap : IDisposable
{
    #region 常量

    public const int MaxCascades = 8;

    #endregion

    #region 字段

    private readonly IDevice _device;
    private bool _isDisposed;

    #endregion

    #region 属性

    public int CascadeCount { get; private set; }
    public int Resolution { get; private set; }
    public IResource DepthTextureArray { get; private set; }
    public IRhiRenderPass RenderPass { get; private set; }
    public IRhiFramebuffer[] Framebuffers { get; private set; }
    public Matrix4x4[] ViewProjectionMatrices { get; private set; }
    public float[] SplitDepths { get; private set; }

    #endregion

    #region 构造函数

    public CascadedShadowMap(IDevice device, int cascadeCount, int resolution)
    {
        _device = device;
        CascadeCount = Math.Min(cascadeCount, MaxCascades);
        Resolution = resolution;

        ViewProjectionMatrices = new Matrix4x4[CascadeCount];
        SplitDepths = new float[CascadeCount + 1];

        DepthTextureArray = CreateDepthTextureArray();
        RenderPass = CreateRenderPass();
        Framebuffers = CreateFramebuffers();
    }

    #endregion

    #region 公开方法

    public void UpdateCascades(IDirectionalLight light, in Matrix4x4 cameraView, in Matrix4x4 cameraProjection, float nearPlane, float farPlane)
    {
        var cascadeSplits = ComputeCascadeSplits(nearPlane, farPlane);

        var lightDir = new Vector3(
            light.Direction[0],
            light.Direction[1],
            light.Direction[2]);

        if (lightDir.LengthSquared() > 0.0001f)
        {
            lightDir = lightDir.Normalize();
        }
        else
        {
            lightDir = new Vector3(0.0f, -1.0f, 0.0f);
        }

        for (int i = 0; i < CascadeCount; i++)
        {
            SplitDepths[i] = cascadeSplits[i];

            var splitNear = i == 0 ? nearPlane : cascadeSplits[i - 1];
            var splitFar = cascadeSplits[i];

            ViewProjectionMatrices[i] = ComputeLightViewProjection(lightDir, splitNear, splitFar, cameraView, cameraProjection);
        }

        SplitDepths[CascadeCount] = farPlane;
    }

    public void Resize(int cascadeCount, int resolution)
    {
        if (cascadeCount == CascadeCount && resolution == Resolution)
        {
            return;
        }

        CascadeCount = Math.Min(cascadeCount, MaxCascades);
        Resolution = resolution;

        ViewProjectionMatrices = new Matrix4x4[CascadeCount];
        SplitDepths = new float[CascadeCount + 1];

        DepthTextureArray.Dispose();
        foreach (var fb in Framebuffers)
        {
            fb.Dispose();
        }
        RenderPass.Dispose();

        DepthTextureArray = CreateDepthTextureArray();
        RenderPass = CreateRenderPass();
        Framebuffers = CreateFramebuffers();
    }

    #endregion

    #region 私有方法

    private float[] ComputeCascadeSplits(float nearPlane, float farPlane)
    {
        var splits = new float[CascadeCount];
        var lambda = 0.75f;

        for (int i = 0; i < CascadeCount; i++)
        {
            var p = (i + 1) / (float)CascadeCount;
            var logSplit = nearPlane * MathF.Pow(farPlane / nearPlane, p);
            var uniformSplit = nearPlane + (farPlane - nearPlane) * p;
            splits[i] = lambda * logSplit + (1.0f - lambda) * uniformSplit;
        }

        return splits;
    }

    private static Matrix4x4 ComputeLightViewProjection(
        in Vector3 lightDir,
        float splitNear,
        float splitFar,
        in Matrix4x4 cameraView,
        in Matrix4x4 cameraProjection)
    {
        var frustumCorners = ComputeFrustumCorners(splitNear, splitFar, cameraView, cameraProjection);

        var frustumCenter = Vector3.Zero;
        foreach (var corner in frustumCorners)
        {
            frustumCenter += corner;
        }
        frustumCenter /= frustumCorners.Length;

        var lightView = Matrix4x4.CreateLookAt(
            frustumCenter - lightDir * (splitFar - splitNear),
            frustumCenter,
            new Vector3(0.0f, 1.0f, 0.0f));

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var corner in frustumCorners)
        {
            var transformed = lightView.TransformPoint(corner);
            minX = MathF.Min(minX, transformed.X);
            maxX = MathF.Max(maxX, transformed.X);
            minY = MathF.Min(minY, transformed.Y);
            maxY = MathF.Max(maxY, transformed.Y);
            minZ = MathF.Min(minZ, transformed.Z);
            maxZ = MathF.Max(maxZ, transformed.Z);
        }

        var zMult = 10.0f;
        if (minZ < 0)
        {
            minZ *= zMult;
        }
        else
        {
            minZ /= zMult;
        }

        if (maxZ < 0)
        {
            maxZ /= zMult;
        }
        else
        {
            maxZ *= zMult;
        }

        var lightProjection = Matrix4x4.CreateOrthographic(
            maxX - minX, maxY - minY, minZ, maxZ);

        return lightView * lightProjection;
    }

    private static Vector3[] ComputeFrustumCorners(
        float nearPlane,
        float farPlane,
        in Matrix4x4 cameraView,
        in Matrix4x4 cameraProjection)
    {
        var viewProj = cameraProjection * cameraView;
        var invViewProj = viewProj.Invert();

        var corners = new Vector3[8];

        var ndcOffsets = new (float x, float y)[]
        {
            (-1, -1), (1, -1), (1, 1), (-1, 1)
        };

        for (int i = 0; i < 4; i++)
        {
            var nearPoint = new Vector4(ndcOffsets[i].x, ndcOffsets[i].y, 0.0f, 1.0f);
            corners[i] = TransformHomogeneous(invViewProj, nearPoint);
        }

        for (int i = 0; i < 4; i++)
        {
            var farPoint = new Vector4(ndcOffsets[i].x, ndcOffsets[i].y, 1.0f, 1.0f);
            corners[i + 4] = TransformHomogeneous(invViewProj, farPoint);
        }

        return corners;
    }

    private static Vector3 TransformHomogeneous(in Matrix4x4 matrix, in Vector4 point)
    {
        var x = matrix.M11 * point.X + matrix.M12 * point.Y + matrix.M13 * point.Z + matrix.M14 * point.W;
        var y = matrix.M21 * point.X + matrix.M22 * point.Y + matrix.M23 * point.Z + matrix.M24 * point.W;
        var z = matrix.M31 * point.X + matrix.M32 * point.Y + matrix.M33 * point.Z + matrix.M34 * point.W;
        var w = matrix.M41 * point.X + matrix.M42 * point.Y + matrix.M43 * point.Z + matrix.M44 * point.W;

        if (MathF.Abs(w) > MathHelper.Epsilon)
        {
            return new Vector3(x / w, y / w, z / w);
        }

        return new Vector3(x, y, z);
    }

    private IResource CreateDepthTextureArray()
    {
        return _device.CreateTexture(new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = (uint)Resolution,
            Height = (uint)Resolution,
            Depth = 1,
            Format = ResourceFormat.D32FloatS8Uint,
            Usage = TextureUsage.DepthStencil | TextureUsage.ShaderResource,
            MipLevels = 1,
            ArrayLayers = (uint)CascadeCount,
            SampleCount = 1
        });
    }

    private IRhiRenderPass CreateRenderPass()
    {
        var attachments = new[]
        {
            new AttachmentDesc
            {
                Format = ResourceFormat.D32FloatS8Uint,
                SampleCount = 1,
                LoadAction = LoadAction.Clear,
                StoreAction = StoreAction.Store,
                StencilLoadAction = LoadAction.DontCare,
                StencilStoreAction = StoreAction.DontCare,
                InitialLayout = TextureLayout.Undefined,
                FinalLayout = TextureLayout.DepthStencilAttachment
            }
        };

        var subPasses = new[]
        {
            new SubPassDesc
            {
                ColorAttachments = [],
                DepthStencilAttachment = 0,
                InputAttachments = []
            }
        };

        var dependencies = new[]
        {
            new SubPassDependency
            {
                SrcSubPass = uint.MaxValue,
                DstSubPass = 0,
                SrcStage = PipelineStageFlag.EarlyFragmentTests,
                DstStage = PipelineStageFlag.EarlyFragmentTests,
                SrcAccess = AccessFlag.None,
                DstAccess = AccessFlag.DepthStencilAttachmentWrite
            }
        };

        return _device.CreateRenderPass(new RenderPassDesc
        {
            Attachments = attachments,
            SubPasses = subPasses,
            Dependencies = dependencies
        });
    }

    private IRhiFramebuffer[] CreateFramebuffers()
    {
        var framebuffers = new IRhiFramebuffer[CascadeCount];
        for (int i = 0; i < CascadeCount; i++)
        {
            framebuffers[i] = _device.CreateFramebuffer(new FramebufferDesc
            {
                RenderPass = RenderPass,
                Attachments = [DepthTextureArray],
                Width = (uint)Resolution,
                Height = (uint)Resolution,
                Layers = 1
            });
        }
        return framebuffers;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        DepthTextureArray.Dispose();
        foreach (var fb in Framebuffers)
        {
            fb.Dispose();
        }
        RenderPass.Dispose();

        _isDisposed = true;
    }

    #endregion
}

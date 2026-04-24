using Gnosis.Graphic.Shader;

namespace Gnosis.Graphic.RHI.OpenGL;

internal static class GlConversions
{
    public static uint ToGlPrimitiveTopology(PrimitiveTopology topology) => topology switch
    {
        PrimitiveTopology.PointList => GlConstants.GL_POINTS,
        PrimitiveTopology.LineList => GlConstants.GL_LINES,
        PrimitiveTopology.LineStrip => GlConstants.GL_LINE_STRIP,
        PrimitiveTopology.TriangleList => GlConstants.GL_TRIANGLES,
        PrimitiveTopology.TriangleStrip => GlConstants.GL_TRIANGLE_STRIP,
        PrimitiveTopology.TriangleFan => GlConstants.GL_TRIANGLE_FAN,
        _ => GlConstants.GL_TRIANGLES
    };

    public static uint ToGlShaderStage(ShaderStage stage) => stage switch
    {
        ShaderStage.Vertex => GlConstants.GL_VERTEX_SHADER,
        ShaderStage.Fragment => GlConstants.GL_FRAGMENT_SHADER,
        ShaderStage.Geometry => GlConstants.GL_GEOMETRY_SHADER,
        ShaderStage.TessControl => GlConstants.GL_TESS_CONTROL_SHADER,
        ShaderStage.TessEvaluation => GlConstants.GL_TESS_EVALUATION_SHADER,
        ShaderStage.Compute => GlConstants.GL_COMPUTE_SHADER,
        _ => GlConstants.GL_VERTEX_SHADER
    };

    public static uint ToGlCompareFunction(CompareFunction func) => func switch
    {
        CompareFunction.Never => GlConstants.GL_NEVER,
        CompareFunction.Less => GlConstants.GL_LESS,
        CompareFunction.Equal => GlConstants.GL_EQUAL,
        CompareFunction.LessEqual => GlConstants.GL_LEQUAL,
        CompareFunction.Greater => GlConstants.GL_GREATER,
        CompareFunction.NotEqual => GlConstants.GL_NOTEQUAL,
        CompareFunction.GreaterEqual => GlConstants.GL_GEQUAL,
        CompareFunction.Always => GlConstants.GL_ALWAYS,
        _ => GlConstants.GL_LESS
    };

    public static uint ToGlCullMode(CullMode mode) => mode switch
    {
        CullMode.None => 0,
        CullMode.Front => GlConstants.GL_FRONT,
        CullMode.Back => GlConstants.GL_BACK,
        _ => 0
    };

    public static uint ToGlFrontFace(FrontFace face) => face switch
    {
        FrontFace.Clockwise => GlConstants.GL_CW,
        FrontFace.CounterClockwise => GlConstants.GL_CCW,
        _ => GlConstants.GL_CCW
    };

    public static uint ToGlPolygonMode(PolygonMode mode) => mode switch
    {
        PolygonMode.Fill => GlConstants.GL_FILL,
        PolygonMode.Line => GlConstants.GL_LINE,
        PolygonMode.Point => GlConstants.GL_POINT,
        _ => GlConstants.GL_FILL
    };

    public static uint ToGlBlendFactor(BlendFactor factor) => factor switch
    {
        BlendFactor.Zero => GlConstants.GL_ZERO,
        BlendFactor.One => GlConstants.GL_ONE,
        BlendFactor.SrcColor => GlConstants.GL_SRC_COLOR,
        BlendFactor.OneMinusSrcColor => GlConstants.GL_ONE_MINUS_SRC_COLOR,
        BlendFactor.SrcAlpha => GlConstants.GL_SRC_ALPHA,
        BlendFactor.OneMinusSrcAlpha => GlConstants.GL_ONE_MINUS_SRC_ALPHA,
        BlendFactor.DstAlpha => GlConstants.GL_DST_ALPHA,
        BlendFactor.OneMinusDstAlpha => GlConstants.GL_ONE_MINUS_DST_ALPHA,
        BlendFactor.DstColor => GlConstants.GL_DST_COLOR,
        BlendFactor.OneMinusDstColor => GlConstants.GL_ONE_MINUS_DST_COLOR,
        _ => GlConstants.GL_ZERO
    };

    public static uint ToGlBlendOp(BlendOp op) => op switch
    {
        BlendOp.Add => GlConstants.GL_FUNC_ADD,
        BlendOp.Subtract => GlConstants.GL_FUNC_SUBTRACT,
        BlendOp.ReverseSubtract => GlConstants.GL_FUNC_REVERSE_SUBTRACT,
        BlendOp.Min => GlConstants.GL_MIN,
        BlendOp.Max => GlConstants.GL_MAX,
        _ => GlConstants.GL_FUNC_ADD
    };

    public static uint ToGlFilterMode(FilterMode mode) => mode switch
    {
        FilterMode.Nearest => GlConstants.GL_NEAREST,
        FilterMode.Linear => GlConstants.GL_LINEAR,
        _ => GlConstants.GL_LINEAR
    };

    public static uint ToGlAddressMode(SamplerAddressMode mode) => mode switch
    {
        SamplerAddressMode.Repeat => GlConstants.GL_REPEAT,
        SamplerAddressMode.ClampToEdge => GlConstants.GL_CLAMP_TO_EDGE,
        SamplerAddressMode.MirroredRepeat => GlConstants.GL_MIRRORED_REPEAT,
        SamplerAddressMode.ClampToBorder => GlConstants.GL_CLAMP_TO_BORDER,
        _ => GlConstants.GL_REPEAT
    };

    public static int ToGlInternalFormat(ResourceFormat format) => format switch
    {
        ResourceFormat.R8G8B8A8Unorm => (int)GlConstants.GL_RGBA8,
        ResourceFormat.B8G8R8A8Unorm => (int)GlConstants.GL_RGBA8,
        ResourceFormat.R16G16B16A16Float => (int)GlConstants.GL_RGBA16F,
        ResourceFormat.R32G32B32A32Float => (int)GlConstants.GL_RGBA32F,
        ResourceFormat.D24UnormS8Uint => (int)GlConstants.GL_DEPTH24_STENCIL8,
        ResourceFormat.D32FloatS8Uint => (int)GlConstants.GL_DEPTH32F_STENCIL8,
        _ => (int)GlConstants.GL_RGBA8
    };

    public static uint ToGlFormat(ResourceFormat format) => format switch
    {
        ResourceFormat.R8G8B8A8Unorm => GlConstants.GL_RGBA,
        ResourceFormat.B8G8R8A8Unorm => GlConstants.GL_BGRA,
        ResourceFormat.R16G16B16A16Float => GlConstants.GL_RGBA,
        ResourceFormat.R32G32B32A32Float => GlConstants.GL_RGBA,
        ResourceFormat.D24UnormS8Uint => GlConstants.GL_DEPTH_COMPONENT,
        ResourceFormat.D32FloatS8Uint => GlConstants.GL_DEPTH_COMPONENT,
        _ => GlConstants.GL_RGBA
    };

    public static uint ToGlType(ResourceFormat format) => format switch
    {
        ResourceFormat.R8G8B8A8Unorm => GlConstants.GL_UNSIGNED_BYTE,
        ResourceFormat.B8G8R8A8Unorm => GlConstants.GL_UNSIGNED_BYTE,
        ResourceFormat.R16G16B16A16Float => GlConstants.GL_HALF_FLOAT,
        ResourceFormat.R32G32B32A32Float => GlConstants.GL_FLOAT,
        ResourceFormat.D24UnormS8Uint => GlConstants.GL_UNSIGNED_INT,
        ResourceFormat.D32FloatS8Uint => GlConstants.GL_FLOAT,
        _ => GlConstants.GL_UNSIGNED_BYTE
    };

    public static uint ToGlTextureTarget(TextureDimension dim) => dim switch
    {
        TextureDimension.Texture1D => GlConstants.GL_TEXTURE_2D,
        TextureDimension.Texture2D => GlConstants.GL_TEXTURE_2D,
        TextureDimension.Texture3D => GlConstants.GL_TEXTURE_3D,
        TextureDimension.TextureCube => GlConstants.GL_TEXTURE_CUBE_MAP,
        TextureDimension.Texture1DArray => GlConstants.GL_TEXTURE_2D_ARRAY,
        TextureDimension.Texture2DArray => GlConstants.GL_TEXTURE_2D_ARRAY,
        _ => GlConstants.GL_TEXTURE_2D
    };

    public static uint ToGlBufferUsage(BufferUsage usage) => usage switch
    {
        BufferUsage.VertexBuffer => GlConstants.GL_ARRAY_BUFFER,
        BufferUsage.IndexBuffer => GlConstants.GL_ELEMENT_ARRAY_BUFFER,
        BufferUsage.UniformBuffer => GlConstants.GL_UNIFORM_BUFFER,
        BufferUsage.StorageBuffer => GlConstants.GL_SHADER_STORAGE_BUFFER,
        _ => GlConstants.GL_ARRAY_BUFFER
    };
}

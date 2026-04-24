namespace Gnosis.Graphic.RHI.OpenGL;

internal static class GlConstants
{
    public const uint GL_FALSE = 0;
    public const uint GL_TRUE = 1;
    public const uint GL_NONE = 0;

    public const uint GL_DEPTH_BUFFER_BIT = 0x00000100;
    public const uint GL_STENCIL_BUFFER_BIT = 0x00000400;
    public const uint GL_COLOR_BUFFER_BIT = 0x00004000;

    public const uint GL_TRIANGLES = 0x0004;
    public const uint GL_TRIANGLE_STRIP = 0x0005;
    public const uint GL_TRIANGLE_FAN = 0x0006;
    public const uint GL_LINES = 0x0001;
    public const uint GL_LINE_STRIP = 0x0003;
    public const uint GL_LINE_LOOP = 0x0002;
    public const uint GL_POINTS = 0x0000;
    public const uint GL_PATCHES = 0x000E;

    public const uint GL_VERTEX_SHADER = 0x8B31;
    public const uint GL_FRAGMENT_SHADER = 0x8B30;
    public const uint GL_GEOMETRY_SHADER = 0x8DD9;
    public const uint GL_TESS_CONTROL_SHADER = 0x8E88;
    public const uint GL_TESS_EVALUATION_SHADER = 0x8E87;
    public const uint GL_COMPUTE_SHADER = 0x91B9;

    public const uint GL_COMPILE_STATUS = 0x8B81;
    public const uint GL_LINK_STATUS = 0x8B82;
    public const uint GL_INFO_LOG_LENGTH = 0x8B84;

    public const uint GL_ARRAY_BUFFER = 0x8892;
    public const uint GL_ELEMENT_ARRAY_BUFFER = 0x8893;
    public const uint GL_UNIFORM_BUFFER = 0x8A11;
    public const uint GL_SHADER_STORAGE_BUFFER = 0x90D2;

    public const uint GL_STATIC_DRAW = 0x88E4;
    public const uint GL_DYNAMIC_DRAW = 0x88E8;
    public const uint GL_STREAM_DRAW = 0x88E0;

    public const uint GL_TEXTURE_2D = 0x0DE1;
    public const uint GL_TEXTURE_3D = 0x806F;
    public const uint GL_TEXTURE_2D_ARRAY = 0x8C1A;
    public const uint GL_TEXTURE_CUBE_MAP = 0x8513;

    public const uint GL_TEXTURE0 = 0x84C0;

    public const uint GL_RGBA = 0x1908;
    public const uint GL_BGRA = 0x80E1;
    public const uint GL_RGB = 0x1907;
    public const uint GL_RG = 0x8227;
    public const uint GL_RED = 0x1903;
    public const uint GL_RGBA8 = 0x8058;
    public const uint GL_RGBA16F = 0x881A;
    public const uint GL_RGBA32F = 0x8814;
    public const uint GL_RG16F = 0x822F;
    public const uint GL_R16F = 0x822D;
    public const uint GL_R8 = 0x8229;
    public const uint GL_RG8 = 0x822B;
    public const uint GL_SRGB8_ALPHA8 = 0x8C43;
    public const uint GL_DEPTH_COMPONENT = 0x1902;
    public const uint GL_DEPTH_COMPONENT16 = 0x81A5;
    public const uint GL_DEPTH_COMPONENT24 = 0x81A6;
    public const uint GL_DEPTH_COMPONENT32F = 0x8CAC;
    public const uint GL_DEPTH24_STENCIL8 = 0x88F0;
    public const uint GL_DEPTH32F_STENCIL8 = 0x8CAD;
    public const uint GL_UNSIGNED_BYTE = 0x1401;
    public const uint GL_UNSIGNED_SHORT = 0x1403;
    public const uint GL_UNSIGNED_INT = 0x1405;
    public const uint GL_FLOAT = 0x1406;
    public const uint GL_HALF_FLOAT = 0x140B;

    public const uint GL_TEXTURE_MIN_FILTER = 0x2801;
    public const uint GL_TEXTURE_MAG_FILTER = 0x2800;
    public const uint GL_TEXTURE_WRAP_S = 0x2802;
    public const uint GL_TEXTURE_WRAP_T = 0x2803;
    public const uint GL_TEXTURE_WRAP_R = 0x2804;

    public const uint GL_NEAREST = 0x2600;
    public const uint GL_LINEAR = 0x2601;
    public const uint GL_NEAREST_MIPMAP_NEAREST = 0x2700;
    public const uint GL_LINEAR_MIPMAP_NEAREST = 0x2701;
    public const uint GL_NEAREST_MIPMAP_LINEAR = 0x2702;
    public const uint GL_LINEAR_MIPMAP_LINEAR = 0x2703;

    public const uint GL_REPEAT = 0x2901;
    public const uint GL_CLAMP_TO_EDGE = 0x812F;
    public const uint GL_MIRRORED_REPEAT = 0x8370;
    public const uint GL_CLAMP_TO_BORDER = 0x812D;

    public const uint GL_FRAMEBUFFER = 0x8D40;
    public const uint GL_READ_FRAMEBUFFER = 0x8CA8;
    public const uint GL_DRAW_FRAMEBUFFER = 0x8CA9;
    public const uint GL_RENDERBUFFER = 0x8D41;
    public const uint GL_COLOR_ATTACHMENT0 = 0x8CE0;
    public const uint GL_DEPTH_ATTACHMENT = 0x8D00;
    public const uint GL_STENCIL_ATTACHMENT = 0x8D20;
    public const uint GL_DEPTH_STENCIL_ATTACHMENT = 0x821A;
    public const uint GL_FRAMEBUFFER_COMPLETE = 0x8CD5;

    public const uint GL_BLEND = 0x0BE2;
    public const uint GL_DEPTH_TEST = 0x0B71;
    public const uint GL_STENCIL_TEST = 0x0B90;
    public const uint GL_CULL_FACE = 0x0B44;
    public const uint GL_SCISSOR_TEST = 0x0C11;

    public const uint GL_FRONT = 0x0404;
    public const uint GL_BACK = 0x0405;
    public const uint GL_FRONT_AND_BACK = 0x0408;

    public const uint GL_CW = 0x0900;
    public const uint GL_CCW = 0x0901;

    public const uint GL_POINT = 0x1B00;
    public const uint GL_LINE = 0x1B01;
    public const uint GL_FILL = 0x1B02;

    public const uint GL_NEVER = 0x0200;
    public const uint GL_LESS = 0x0201;
    public const uint GL_EQUAL = 0x0202;
    public const uint GL_LEQUAL = 0x0203;
    public const uint GL_GREATER = 0x0204;
    public const uint GL_NOTEQUAL = 0x0205;
    public const uint GL_GEQUAL = 0x0206;
    public const uint GL_ALWAYS = 0x0207;

    public const uint GL_ZERO = 0;
    public const uint GL_ONE = 1;
    public const uint GL_SRC_COLOR = 0x0300;
    public const uint GL_ONE_MINUS_SRC_COLOR = 0x0301;
    public const uint GL_SRC_ALPHA = 0x0302;
    public const uint GL_ONE_MINUS_SRC_ALPHA = 0x0303;
    public const uint GL_DST_ALPHA = 0x0304;
    public const uint GL_ONE_MINUS_DST_ALPHA = 0x0305;
    public const uint GL_DST_COLOR = 0x0306;
    public const uint GL_ONE_MINUS_DST_COLOR = 0x0307;

    public const uint GL_FUNC_ADD = 0x8006;
    public const uint GL_FUNC_SUBTRACT = 0x800A;
    public const uint GL_FUNC_REVERSE_SUBTRACT = 0x800B;
    public const uint GL_MIN = 0x8007;
    public const uint GL_MAX = 0x8008;

    public const uint GL_UNPACK_ALIGNMENT = 0x0CF5;

    public const uint GL_TEXTURE_MAX_ANISOTROPY = 0x84FE;
    public const uint GL_MAX_TEXTURE_MAX_ANISOTROPY = 0x84FF;

    public const uint GL_TEXTURE_COMPARE_MODE = 0x884C;
    public const uint GL_TEXTURE_COMPARE_FUNC = 0x884D;
    public const uint GL_COMPARE_REF_TO_TEXTURE = 0x884E;

    public const uint GL_TEXTURE_LOD_BIAS = 0x8501;
    public const uint GL_TEXTURE_MIN_LOD = 0x813A;
    public const uint GL_TEXTURE_MAX_LOD = 0x813B;

    public const uint GL_SHADER_BINARY_FORMAT_SPIR_V = 0x9551;
    public const uint GL_SPIR_V_BINARY = 0x9552;
}

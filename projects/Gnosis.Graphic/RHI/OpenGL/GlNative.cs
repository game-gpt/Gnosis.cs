using System.Runtime.InteropServices;

namespace Gnosis.Graphic.RHI.OpenGL;

internal static unsafe class GlNative
{
    private const string OpenGLDll = "opengl32.dll";

    [DllImport(OpenGLDll, SetLastError = true)]
    public static extern nint wglGetCurrentContext();

    [DllImport(OpenGLDll, SetLastError = true)]
    public static extern nint wglCreateContext(nint hdc);

    [DllImport(OpenGLDll, SetLastError = true)]
    public static extern int wglMakeCurrent(nint hdc, nint hglrc);

    [DllImport(OpenGLDll, SetLastError = true)]
    public static extern int wglDeleteContext(nint hglrc);

    public delegate uint GlCreateShaderProc(uint type);
    public delegate void GlShaderSourceProc(uint shader, int count, byte** strings, int* lengths);
    public delegate void GlCompileShaderProc(uint shader);
    public delegate void GlGetShaderivProc(uint shader, uint pname, int* params_);
    public delegate void GlGetShaderInfoLogProc(uint shader, int bufSize, int* length, byte* infoLog);
    public delegate void GlDeleteShaderProc(uint shader);
    public delegate uint GlCreateProgramProc();
    public delegate void GlAttachShaderProc(uint program, uint shader);
    public delegate void GlLinkProgramProc(uint program);
    public delegate void GlGetProgramivProc(uint program, uint pname, int* params_);
    public delegate void GlGetProgramInfoLogProc(uint program, int bufSize, int* length, byte* infoLog);
    public delegate void GlDeleteProgramProc(uint program);
    public delegate void GlUseProgramProc(uint program);
    public delegate int GlGetUniformLocationProc(uint program, byte* name);
    public delegate void GlUniform1fProc(int location, float v0);
    public delegate void GlUniform1iProc(int location, int v0);
    public delegate void GlUniform2fProc(int location, float v0, float v1);
    public delegate void GlUniform3fProc(int location, float v0, float v1, float v2);
    public delegate void GlUniform4fProc(int location, float v0, float v1, float v2, float v3);
    public delegate void GlUniformMatrix4fvProc(int location, int count, bool transpose, float* value);
    public delegate void GlGenBuffersProc(int n, uint* buffers);
    public delegate void GlDeleteBuffersProc(int n, uint* buffers);
    public delegate void GlBindBufferProc(uint target, uint buffer);
    public delegate void GlBufferDataProc(uint target, nint size, void* data, uint usage);
    public delegate void GlBufferSubDataProc(uint target, nint offset, nint size, void* data);
    public delegate void GlGenVertexArraysProc(int n, uint* arrays);
    public delegate void GlDeleteVertexArraysProc(int n, uint* arrays);
    public delegate void GlBindVertexArrayProc(uint array);
    public delegate void GlEnableVertexAttribArrayProc(uint index);
    public delegate void GlVertexAttribPointerProc(uint index, int size, uint type, bool normalized, int stride, nint pointer);
    public delegate void GlGenTexturesProc(int n, uint* textures);
    public delegate void GlDeleteTexturesProc(int n, uint* textures);
    public delegate void GlBindTextureProc(uint target, uint texture);
    public delegate void GlTexImage2DProc(uint target, int level, int internalFormat, int width, int height, int border, uint format, uint type, void* pixels);
    public delegate void GlTexSubImage2DProc(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, void* pixels);
    public delegate void GlTexParameteriProc(uint target, uint pname, int param);
    public delegate void GlActiveTextureProc(uint texture);
    public delegate void GlGenFramebuffersProc(int n, uint* framebuffers);
    public delegate void GlDeleteFramebuffersProc(int n, uint* framebuffers);
    public delegate void GlBindFramebufferProc(uint target, uint framebuffer);
    public delegate void GlFramebufferTexture2DProc(uint target, uint attachment, uint textarget, uint texture, int level);
    public delegate void GlFramebufferRenderbufferProc(uint target, uint attachment, uint renderbuffertarget, uint renderbuffer);
    public delegate uint GlCheckFramebufferStatusProc(uint target);
    public delegate void GlGenRenderbuffersProc(int n, uint* renderbuffers);
    public delegate void GlDeleteRenderbuffersProc(int n, uint* renderbuffers);
    public delegate void GlBindRenderbufferProc(uint target, uint renderbuffer);
    public delegate void GlRenderbufferStorageProc(uint target, uint internalformat, int width, int height);
    public delegate void GlDrawBuffersProc(int n, uint* bufs);
    public delegate void GlViewportProc(int x, int y, int width, int height);
    public delegate void GlScissorProc(int x, int y, int width, int height);
    public delegate void GlClearColorProc(float red, float green, float blue, float alpha);
    public delegate void GlClearProc(uint mask);
    public delegate void GlEnableProc(uint cap);
    public delegate void GlDisableProc(uint cap);
    public delegate void GlBlendFuncProc(uint sfactor, uint dfactor);
    public delegate void GlBlendFuncSeparateProc(uint sfactorRGB, uint dfactorRGB, uint sfactorAlpha, uint dfactorAlpha);
    public delegate void GlDepthFuncProc(uint func);
    public delegate void GlDepthMaskProc(bool flag);
    public delegate void GlCullFaceProc(uint mode);
    public delegate void GlFrontFaceProc(uint mode);
    public delegate void GlPolygonModeProc(uint face, uint mode);
    public delegate void GlDrawArraysProc(uint mode, int first, int count);
    public delegate void GlDrawElementsProc(uint mode, int count, uint type, nint indices);
    public delegate void GlPixelStoreiProc(uint pname, int param);
    public delegate void GlFlushProc();
    public delegate void GlFinishProc();
    public delegate nint GlGetProcAddressProc(byte* name);

    public delegate void GlGenSamplersProc(int n, uint* samplers);
    public delegate void GlDeleteSamplersProc(int n, uint* samplers);
    public delegate void GlSamplerParameteriProc(uint sampler, uint pname, int param);
    public delegate void GlSamplerParameterfProc(uint sampler, uint pname, float param);
    public delegate void GlBindSamplerProc(uint unit, uint sampler);
    public delegate void GlBindBufferBaseProc(uint target, uint index, uint buffer);
    public delegate void GlBindBufferRangeProc(uint target, uint index, uint buffer, nint offset, nint size);

    public static GlCreateShaderProc? CreateShader;
    public static GlShaderSourceProc? ShaderSource;
    public static GlCompileShaderProc? CompileShader;
    public static GlGetShaderivProc? GetShaderiv;
    public static GlGetShaderInfoLogProc? GetShaderInfoLog;
    public static GlDeleteShaderProc? DeleteShader;
    public static GlCreateProgramProc? CreateProgram;
    public static GlAttachShaderProc? AttachShader;
    public static GlLinkProgramProc? LinkProgram;
    public static GlGetProgramivProc? GetProgramiv;
    public static GlGetProgramInfoLogProc? GetProgramInfoLog;
    public static GlDeleteProgramProc? DeleteProgram;
    public static GlUseProgramProc? UseProgram;
    public static GlGetUniformLocationProc? GetUniformLocation;
    public static GlUniform1fProc? Uniform1f;
    public static GlUniform1iProc? Uniform1i;
    public static GlUniform2fProc? Uniform2f;
    public static GlUniform3fProc? Uniform3f;
    public static GlUniform4fProc? Uniform4f;
    public static GlUniformMatrix4fvProc? UniformMatrix4fv;
    public static GlGenBuffersProc? GenBuffers;
    public static GlDeleteBuffersProc? DeleteBuffers;
    public static GlBindBufferProc? BindBuffer;
    public static GlBufferDataProc? BufferData;
    public static GlBufferSubDataProc? BufferSubData;
    public static GlGenVertexArraysProc? GenVertexArrays;
    public static GlDeleteVertexArraysProc? DeleteVertexArrays;
    public static GlBindVertexArrayProc? BindVertexArray;
    public static GlEnableVertexAttribArrayProc? EnableVertexAttribArray;
    public static GlVertexAttribPointerProc? VertexAttribPointer;
    public static GlGenTexturesProc? GenTextures;
    public static GlDeleteTexturesProc? DeleteTextures;
    public static GlBindTextureProc? BindTexture;
    public static GlTexImage2DProc? TexImage2D;
    public static GlTexSubImage2DProc? TexSubImage2D;
    public static GlTexParameteriProc? TexParameteri;
    public static GlActiveTextureProc? ActiveTexture;
    public static GlGenFramebuffersProc? GenFramebuffers;
    public static GlDeleteFramebuffersProc? DeleteFramebuffers;
    public static GlBindFramebufferProc? BindFramebuffer;
    public static GlFramebufferTexture2DProc? FramebufferTexture2D;
    public static GlFramebufferRenderbufferProc? FramebufferRenderbuffer;
    public static GlCheckFramebufferStatusProc? CheckFramebufferStatus;
    public static GlGenRenderbuffersProc? GenRenderbuffers;
    public static GlDeleteRenderbuffersProc? DeleteRenderbuffers;
    public static GlBindRenderbufferProc? BindRenderbuffer;
    public static GlRenderbufferStorageProc? RenderbufferStorage;
    public static GlDrawBuffersProc? DrawBuffers;
    public static GlViewportProc? Viewport;
    public static GlScissorProc? Scissor;
    public static GlClearColorProc? ClearColor;
    public static GlClearProc? Clear;
    public static GlEnableProc? Enable;
    public static GlDisableProc? Disable;
    public static GlBlendFuncProc? BlendFunc;
    public static GlBlendFuncSeparateProc? BlendFuncSeparate;
    public static GlDepthFuncProc? DepthFunc;
    public static GlDepthMaskProc? DepthMask;
    public static GlCullFaceProc? CullFace;
    public static GlFrontFaceProc? FrontFace;
    public static GlPolygonModeProc? PolygonMode;
    public static GlDrawArraysProc? DrawArrays;
    public static GlDrawElementsProc? DrawElements;
    public static GlPixelStoreiProc? PixelStorei;
    public static GlFlushProc? Flush;
    public static GlFinishProc? Finish;

    public static void LoadFunctions(Func<string, nint> getProcAddress)
    {
        CreateShader = LoadDelegate<GlCreateShaderProc>(getProcAddress, "glCreateShader");
        ShaderSource = LoadDelegate<GlShaderSourceProc>(getProcAddress, "glShaderSource");
        CompileShader = LoadDelegate<GlCompileShaderProc>(getProcAddress, "glCompileShader");
        GetShaderiv = LoadDelegate<GlGetShaderivProc>(getProcAddress, "glGetShaderiv");
        GetShaderInfoLog = LoadDelegate<GlGetShaderInfoLogProc>(getProcAddress, "glGetShaderInfoLog");
        DeleteShader = LoadDelegate<GlDeleteShaderProc>(getProcAddress, "glDeleteShader");
        CreateProgram = LoadDelegate<GlCreateProgramProc>(getProcAddress, "glCreateProgram");
        AttachShader = LoadDelegate<GlAttachShaderProc>(getProcAddress, "glAttachShader");
        LinkProgram = LoadDelegate<GlLinkProgramProc>(getProcAddress, "glLinkProgram");
        GetProgramiv = LoadDelegate<GlGetProgramivProc>(getProcAddress, "glGetProgramiv");
        GetProgramInfoLog = LoadDelegate<GlGetProgramInfoLogProc>(getProcAddress, "glGetProgramInfoLog");
        DeleteProgram = LoadDelegate<GlDeleteProgramProc>(getProcAddress, "glDeleteProgram");
        UseProgram = LoadDelegate<GlUseProgramProc>(getProcAddress, "glUseProgram");
        GetUniformLocation = LoadDelegate<GlGetUniformLocationProc>(getProcAddress, "glGetUniformLocation");
        Uniform1f = LoadDelegate<GlUniform1fProc>(getProcAddress, "glUniform1f");
        Uniform1i = LoadDelegate<GlUniform1iProc>(getProcAddress, "glUniform1i");
        Uniform2f = LoadDelegate<GlUniform2fProc>(getProcAddress, "glUniform2f");
        Uniform3f = LoadDelegate<GlUniform3fProc>(getProcAddress, "glUniform3f");
        Uniform4f = LoadDelegate<GlUniform4fProc>(getProcAddress, "glUniform4f");
        UniformMatrix4fv = LoadDelegate<GlUniformMatrix4fvProc>(getProcAddress, "glUniformMatrix4fv");
        GenBuffers = LoadDelegate<GlGenBuffersProc>(getProcAddress, "glGenBuffers");
        DeleteBuffers = LoadDelegate<GlDeleteBuffersProc>(getProcAddress, "glDeleteBuffers");
        BindBuffer = LoadDelegate<GlBindBufferProc>(getProcAddress, "glBindBuffer");
        BufferData = LoadDelegate<GlBufferDataProc>(getProcAddress, "glBufferData");
        BufferSubData = LoadDelegate<GlBufferSubDataProc>(getProcAddress, "glBufferSubData");
        GenVertexArrays = LoadDelegate<GlGenVertexArraysProc>(getProcAddress, "glGenVertexArrays");
        DeleteVertexArrays = LoadDelegate<GlDeleteVertexArraysProc>(getProcAddress, "glDeleteVertexArrays");
        BindVertexArray = LoadDelegate<GlBindVertexArrayProc>(getProcAddress, "glBindVertexArray");
        EnableVertexAttribArray = LoadDelegate<GlEnableVertexAttribArrayProc>(getProcAddress, "glEnableVertexAttribArray");
        VertexAttribPointer = LoadDelegate<GlVertexAttribPointerProc>(getProcAddress, "glVertexAttribPointer");
        GenTextures = LoadDelegate<GlGenTexturesProc>(getProcAddress, "glGenTextures");
        DeleteTextures = LoadDelegate<GlDeleteTexturesProc>(getProcAddress, "glDeleteTextures");
        BindTexture = LoadDelegate<GlBindTextureProc>(getProcAddress, "glBindTexture");
        TexImage2D = LoadDelegate<GlTexImage2DProc>(getProcAddress, "glTexImage2D");
        TexSubImage2D = LoadDelegate<GlTexSubImage2DProc>(getProcAddress, "glTexSubImage2D");
        TexParameteri = LoadDelegate<GlTexParameteriProc>(getProcAddress, "glTexParameteri");
        ActiveTexture = LoadDelegate<GlActiveTextureProc>(getProcAddress, "glActiveTexture");
        GenFramebuffers = LoadDelegate<GlGenFramebuffersProc>(getProcAddress, "glGenFramebuffers");
        DeleteFramebuffers = LoadDelegate<GlDeleteFramebuffersProc>(getProcAddress, "glDeleteFramebuffers");
        BindFramebuffer = LoadDelegate<GlBindFramebufferProc>(getProcAddress, "glBindFramebuffer");
        FramebufferTexture2D = LoadDelegate<GlFramebufferTexture2DProc>(getProcAddress, "glFramebufferTexture2D");
        FramebufferRenderbuffer = LoadDelegate<GlFramebufferRenderbufferProc>(getProcAddress, "glFramebufferRenderbuffer");
        CheckFramebufferStatus = LoadDelegate<GlCheckFramebufferStatusProc>(getProcAddress, "glCheckFramebufferStatus");
        GenRenderbuffers = LoadDelegate<GlGenRenderbuffersProc>(getProcAddress, "glGenRenderbuffers");
        DeleteRenderbuffers = LoadDelegate<GlDeleteRenderbuffersProc>(getProcAddress, "glDeleteRenderbuffers");
        BindRenderbuffer = LoadDelegate<GlBindRenderbufferProc>(getProcAddress, "glBindRenderbuffer");
        RenderbufferStorage = LoadDelegate<GlRenderbufferStorageProc>(getProcAddress, "glRenderbufferStorage");
        DrawBuffers = LoadDelegate<GlDrawBuffersProc>(getProcAddress, "glDrawBuffers");
        Viewport = LoadDelegate<GlViewportProc>(getProcAddress, "glViewport");
        Scissor = LoadDelegate<GlScissorProc>(getProcAddress, "glScissor");
        ClearColor = LoadDelegate<GlClearColorProc>(getProcAddress, "glClearColor");
        Clear = LoadDelegate<GlClearProc>(getProcAddress, "glClear");
        Enable = LoadDelegate<GlEnableProc>(getProcAddress, "glEnable");
        Disable = LoadDelegate<GlDisableProc>(getProcAddress, "glDisable");
        BlendFunc = LoadDelegate<GlBlendFuncProc>(getProcAddress, "glBlendFunc");
        BlendFuncSeparate = LoadDelegate<GlBlendFuncSeparateProc>(getProcAddress, "glBlendFuncSeparate");
        DepthFunc = LoadDelegate<GlDepthFuncProc>(getProcAddress, "glDepthFunc");
        DepthMask = LoadDelegate<GlDepthMaskProc>(getProcAddress, "glDepthMask");
        CullFace = LoadDelegate<GlCullFaceProc>(getProcAddress, "glCullFace");
        FrontFace = LoadDelegate<GlFrontFaceProc>(getProcAddress, "glFrontFace");
        PolygonMode = LoadDelegate<GlPolygonModeProc>(getProcAddress, "glPolygonMode");
        DrawArrays = LoadDelegate<GlDrawArraysProc>(getProcAddress, "glDrawArrays");
        DrawElements = LoadDelegate<GlDrawElementsProc>(getProcAddress, "glDrawElements");
        PixelStorei = LoadDelegate<GlPixelStoreiProc>(getProcAddress, "glPixelStorei");
        Flush = LoadDelegate<GlFlushProc>(getProcAddress, "glFlush");
        Finish = LoadDelegate<GlFinishProc>(getProcAddress, "glFinish");
    }

    private static T LoadDelegate<T>(Func<string, nint> getProcAddress, string name) where T : Delegate
    {
        var ptr = getProcAddress(name);
        if (ptr == nint.Zero)
        {
            throw new InvalidOperationException($"无法加载 OpenGL 函数：{name}");
        }

        return Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }
}

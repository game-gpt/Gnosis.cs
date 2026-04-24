using System.Numerics;

namespace Gnosis.Graphic.RHI.OpenGL;

internal sealed unsafe class OpenGLDescriptorSet : IRhiDescriptorSet
{
    private readonly Dictionary<uint, (OpenGLResource Resource, ulong Offset, ulong Range)> _uniformBuffers = [];
    private readonly Dictionary<uint, OpenGLResource> _textures = [];
    private readonly Dictionary<uint, OpenGLResource> _samplers = [];
    private uint _program;

    public void BindBuffer(uint binding, IResource buffer, ulong offset = 0, ulong range = ulong.MaxValue)
    {
        var glBuffer = buffer as OpenGLResource;
        if (glBuffer != null)
        {
            _uniformBuffers[binding] = (glBuffer, offset, range);
        }
    }

    public void BindTexture(uint binding, IResource texture)
    {
        var glTexture = texture as OpenGLResource;
        if (glTexture != null)
        {
            _textures[binding] = glTexture;
        }
    }

    public void BindSampler(uint binding, IResource sampler)
    {
        var glSampler = sampler as OpenGLResource;
        if (glSampler != null)
        {
            _samplers[binding] = glSampler;
        }
    }

    public void BindUniformData(uint binding, void* data, ulong size)
    {
        if (_program == 0)
        {
            return;
        }

        int loc = (int)binding;
        if (size == (ulong)sizeof(float))
        {
            GlNative.Uniform1f!(loc, *(float*)data);
        }
        else if (size == (ulong)sizeof(Vector2))
        {
            var v = (Vector2*)data;
            GlNative.Uniform2f!(loc, v->X, v->Y);
        }
        else if (size == (ulong)sizeof(Vector3))
        {
            var v = (Vector3*)data;
            GlNative.Uniform3f!(loc, v->X, v->Y, v->Z);
        }
        else if (size == (ulong)sizeof(Vector4))
        {
            var v = (Vector4*)data;
            GlNative.Uniform4f!(loc, v->X, v->Y, v->Z, v->W);
        }
        else if (size == (ulong)sizeof(Matrix4x4))
        {
            GlNative.UniformMatrix4fv!(loc, 1, false, (float*)data);
        }
        else if (size == (ulong)sizeof(int))
        {
            GlNative.Uniform1i!(loc, *(int*)data);
        }
    }

    public void SetProgram(uint program)
    {
        _program = program;
    }

    public void Bind()
    {
        foreach (var (binding, tex) in _textures)
        {
            GlNative.ActiveTexture!(GlConstants.GL_TEXTURE0 + binding);
            GlNative.BindTexture!(GlConstants.GL_TEXTURE_2D, tex.GlTexture);
        }

        foreach (var (binding, sampler) in _samplers)
        {
            if (sampler.GlSampler != 0)
            {
                GlNative.BindSampler!(binding, sampler.GlSampler);
            }
        }

        foreach (var (binding, (buffer, offset, range)) in _uniformBuffers)
        {
            if (buffer.GlBuffer != 0)
            {
                GlNative.BindBufferBase!(GlConstants.GL_UNIFORM_BUFFER, binding, buffer.GlBuffer);
            }
        }
    }

    public void Dispose()
    {
    }
}

using System.Runtime.InteropServices;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Material;

public sealed class ParameterBlock : IDisposable
{
    private readonly Dictionary<string, int> _offsets = [];
    private readonly List<ParameterEntry> _entries = [];
    private byte[] _data;
    private IResource? _gpuBuffer;
    private bool _isDirty = true;
    private bool _isDisposed;

    public string Name { get; }
    public uint Size { get; }
    public uint AlignedSize { get; }
    public IResource? GpuBuffer => _gpuBuffer;

    public ParameterBlock(string name, uint size)
    {
        Name = name;
        Size = size;
        AlignedSize = AlignUp(size, 256);
        _data = new byte[AlignedSize];
    }

    public void SetFloat(string name, float value)
    {
        SetScalar(name, value, sizeof(float));
    }

    public void SetFloat2(string name, float x, float y)
    {
        SetVector(name, [x, y], sizeof(float) * 2);
    }

    public void SetFloat3(string name, float x, float y, float z)
    {
        SetVector(name, [x, y, z], sizeof(float) * 3);
    }

    public void SetFloat4(string name, float x, float y, float z, float w)
    {
        SetVector(name, [x, y, z, w], sizeof(float) * 4);
    }

    public void SetInt(string name, int value)
    {
        SetScalar(name, value, sizeof(int));
    }

    public void SetMatrix4x4(string name, float[] matrix)
    {
        if (matrix.Length != 16)
        {
            throw new ArgumentException("4x4 矩阵需要 16 个浮点数", nameof(matrix));
        }

        SetVector(name, matrix, sizeof(float) * 16);
    }

    public float GetFloat(string name)
    {
        var offset = GetOffset(name);
        return BitConverter.ToSingle(_data, offset);
    }

    public int GetInt(string name)
    {
        var offset = GetOffset(name);
        return BitConverter.ToInt32(_data, offset);
    }

    public ReadOnlySpan<byte> GetData()
    {
        return _data.AsSpan(0, (int)Size);
    }

    public IResource UploadToGpu(IDevice device)
    {
        if (_gpuBuffer != null && !_isDirty)
        {
            return _gpuBuffer;
        }

        var desc = new BufferDesc
        {
            Size = AlignedSize,
            Usage = BufferUsage.UniformBuffer | BufferUsage.TransferDst,
            HostVisible = true,
            InitialData = _data
        };

        _gpuBuffer?.Dispose();
        _gpuBuffer = device.CreateBuffer(desc);

        _isDirty = false;
        return _gpuBuffer;
    }

    public void MarkDirty()
    {
        _isDirty = true;
    }

    internal void RegisterParameter(string name, int offset, MaterialPropertyType type)
    {
        _offsets[name] = offset;
        _entries.Add(new ParameterEntry(name, offset, type));
    }

    private void SetScalar<T>(string name, T value, int size) where T : struct
    {
        var offset = GetOffset(name);
        MemoryMarshal.Write(_data.AsSpan(offset, size), ref value);
        _isDirty = true;
    }

    private void SetVector(string name, float[] values, int size)
    {
        var offset = GetOffset(name);
        MemoryMarshal.AsBytes(values.AsSpan()).CopyTo(_data.AsSpan(offset, size));
        _isDirty = true;
    }

    private int GetOffset(string name)
    {
        if (!_offsets.TryGetValue(name, out var offset))
        {
            throw new KeyNotFoundException($"参数块 '{Name}' 中未找到参数 '{name}'");
        }

        return offset;
    }

    private static uint AlignUp(uint value, uint alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _gpuBuffer?.Dispose();
    }

    private readonly record struct ParameterEntry(string Name, int Offset, MaterialPropertyType Type);
}

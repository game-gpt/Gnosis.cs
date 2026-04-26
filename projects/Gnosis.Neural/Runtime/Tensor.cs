namespace Gnosis.Neural.Runtime;

/// <summary>
/// 张量实现，提供对神经网络张量数据的访问
/// </summary>
public sealed class Tensor : ITensor
{
    #region 字段

    private readonly float[] _data;
    private readonly byte[]? _rawDataCache;

    #endregion

    #region ITensor 实现

    /// <summary>
    /// 张量形状
    /// </summary>
    public TensorShape Shape { get; }

    /// <summary>
    /// 张量数据类型
    /// </summary>
    public TensorDataType DataType { get; }

    /// <summary>
    /// 元素总数
    /// </summary>
    public int Length => _data.Length;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建指定形状的张量
    /// </summary>
    public Tensor(TensorShape shape, TensorDataType dataType = TensorDataType.Float32)
    {
        Shape = shape;
        DataType = dataType;
        _data = new float[shape.ElementCount];
    }

    /// <summary>
    /// 从浮点数组创建张量
    /// </summary>
    public Tensor(float[] data, TensorShape shape, TensorDataType dataType = TensorDataType.Float32)
    {
        _data = data;
        Shape = shape;
        DataType = dataType;
    }

    /// <summary>
    /// 从字节数据创建张量
    /// </summary>
    public Tensor(ReadOnlySpan<byte> rawData, TensorShape shape, TensorDataType dataType)
    {
        Shape = shape;
        DataType = dataType;
        _data = new float[shape.ElementCount];

        var floatSpan = rawData;
        for (var i = 0; i < _data.Length && i * sizeof(float) + sizeof(float) <= floatSpan.Length; i++)
        {
            _data[i] = BitConverter.ToSingle(floatSpan.Slice(i * sizeof(float), sizeof(float)));
        }
    }

    #endregion

    #region ITensor 实现

    /// <summary>
    /// 获取原始数据
    /// </summary>
    public ReadOnlySpan<byte> GetRawData()
    {
        var bytes = new byte[_data.Length * sizeof(float)];
        Buffer.BlockCopy(_data, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// 获取指定索引的浮点值
    /// </summary>
    public float GetFloat(int index)
    {
        if (index < 0 || index >= _data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _data[index];
    }

    /// <summary>
    /// 设置指定索引的浮点值
    /// </summary>
    public void SetFloat(int index, float value)
    {
        if (index < 0 || index >= _data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _data[index] = value;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 获取内部数据数组的引用
    /// </summary>
    public float[] GetData()
    {
        return _data;
    }

    /// <summary>
    /// 创建张量的深拷贝
    /// </summary>
    public Tensor Clone()
    {
        var dataCopy = new float[_data.Length];
        Array.Copy(_data, dataCopy, _data.Length);
        return new Tensor(dataCopy, Shape, DataType);
    }

    /// <summary>
    /// 用零填充张量
    /// </summary>
    public void Zero()
    {
        Array.Clear(_data);
    }

    #endregion

    #region 静态工厂方法

    /// <summary>
    /// 创建零张量
    /// </summary>
    public static Tensor Zeros(params int[] dimensions)
    {
        return new Tensor(new TensorShape(dimensions));
    }

    /// <summary>
    /// 从浮点数组创建一维张量
    /// </summary>
    public static Tensor FromArray(float[] data)
    {
        return new Tensor(data, new TensorShape(data.Length));
    }

    #endregion
}

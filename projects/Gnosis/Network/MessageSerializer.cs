using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Gnosis.Network;

/// <summary>
/// 提供基于内存布局的二进制消息序列化与反序列化功能
/// </summary>
public sealed class MessageSerializer : IMessageSerializer
{
    /// <summary>
    /// 将结构体序列化为字节数组
    /// </summary>
    /// <typeparam name="T">要序列化的结构体类型</typeparam>
    /// <param name="message">要序列化的结构体实例</param>
    /// <returns>包含序列化数据的字节数组</returns>
    public byte[] Serialize<T>(T message) where T : struct
    {
        var span = MemoryMarshal.CreateReadOnlySpan(ref message, 1);
        return MemoryMarshal.AsBytes(span).ToArray();
    }

    /// <summary>
    /// 将字节数据反序列化为结构体
    /// </summary>
    /// <typeparam name="T">要反序列化的结构体类型</typeparam>
    /// <param name="data">包含序列化数据的字节跨度</param>
    /// <returns>反序列化得到的结构体实例</returns>
    /// <exception cref="ArgumentException">当数据长度小于结构体大小时抛出</exception>
    public T Deserialize<T>(ReadOnlySpan<byte> data) where T : struct
    {
        var size = Unsafe.SizeOf<T>();

        if (data.Length < size)
        {
            throw new ArgumentException($"数据长度 {data.Length} 小于结构体 {typeof(T).Name} 所需的大小 {size}");
        }

        return MemoryMarshal.Read<T>(data);
    }
}

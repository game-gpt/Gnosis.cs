using System;
using System.Collections.Generic;
using Gnosis.Network.Channel;
using Gnosis.Network.Serialization;
using Gnosis.Network.Transport;

namespace Gnosis.Network.RPC;

/// <summary>
/// RPC 调用类型
/// </summary>
public enum RpcType : byte
{
    /// <summary>
    /// 客户端到服务器
    /// </summary>
    ServerRpc = 0,

    /// <summary>
    /// 服务器到特定客户端
    /// </summary>
    ClientRpc = 1,

    /// <summary>
    /// 服务器到所有客户端
    /// </summary>
    MulticastRpc = 2
}

/// <summary>
/// RPC 调用描述，包含方法标识和参数数据
/// </summary>
public readonly record struct RpcCall
{
    /// <summary>
    /// RPC 方法标识
    /// </summary>
    public int MethodId { get; init; }

    /// <summary>
    /// RPC 调用类型
    /// </summary>
    public RpcType RpcType { get; init; }

    /// <summary>
    /// 目标连接标识（仅 ClientRpc 使用）
    /// </summary>
    public ConnectionId TargetConnection { get; init; }

    /// <summary>
    /// 调用参数数据
    /// </summary>
    public ReadOnlyMemory<byte> Arguments { get; init; }
}

/// <summary>
/// RPC 方法处理器委托
/// </summary>
public delegate void RpcHandler(ConnectionId caller, ReadOnlySpan<byte> arguments);

/// <summary>
/// RPC 系统，提供远程过程调用的注册、路由与执行
/// </summary>
public sealed class RpcSystem
{
    #region 字段

    private readonly Dictionary<int, RpcHandler> _handlers = new();
    private readonly Dictionary<string, int> _methodNameToId = new();
    private readonly Dictionary<int, string> _methodIdToName = new();
    private readonly IMessageSerializer _serializer;
    private readonly List<RpcCall> _pendingCalls = [];
    private int _nextMethodId;

    #endregion

    #region 属性

    /// <summary>
    /// 获取已注册的 RPC 方法数量
    /// </summary>
    public int RegisteredMethodCount => _handlers.Count;

    /// <summary>
    /// 获取待处理的 RPC 调用数量
    /// </summary>
    public int PendingCallCount => _pendingCalls.Count;

    #endregion

    #region 事件

    /// <summary>
    /// RPC 调用执行时触发
    /// </summary>
    public event Action<int, ConnectionId>? OnRpcExecuted;

    /// <summary>
    /// RPC 调用失败时触发
    /// </summary>
    public event Action<int, ConnectionId, string>? OnRpcFailed;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化 RPC 系统
    /// </summary>
    /// <param name="serializer">消息序列化器</param>
    public RpcSystem(IMessageSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _nextMethodId = 1;
    }

    #endregion

    #region 公共方法 - 注册

    /// <summary>
    /// 注册 RPC 方法处理器
    /// </summary>
    /// <param name="methodName">方法名称</param>
    /// <param name="handler">方法处理器</param>
    /// <returns>分配的方法标识</returns>
    public int Register(string methodName, RpcHandler handler)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            throw new ArgumentException("方法名称不能为空", nameof(methodName));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        if (_methodNameToId.TryGetValue(methodName, out var existingId))
        {
            _handlers[existingId] = handler;
            return existingId;
        }

        var methodId = _nextMethodId++;
        _handlers[methodId] = handler;
        _methodNameToId[methodName] = methodId;
        _methodIdToName[methodId] = methodName;

        return methodId;
    }

    /// <summary>
    /// 注销 RPC 方法
    /// </summary>
    /// <param name="methodName">方法名称</param>
    /// <returns>是否成功注销</returns>
    public bool Unregister(string methodName)
    {
        if (!_methodNameToId.TryGetValue(methodName, out var methodId))
        {
            return false;
        }

        _handlers.Remove(methodId);
        _methodNameToId.Remove(methodName);
        _methodIdToName.Remove(methodId);

        return true;
    }

    /// <summary>
    /// 根据方法名称获取标识
    /// </summary>
    /// <param name="methodName">方法名称</param>
    /// <returns>方法标识，不存在返回 -1</returns>
    public int GetMethodId(string methodName)
    {
        return _methodNameToId.GetValueOrDefault(methodName, -1);
    }

    /// <summary>
    /// 根据方法标识获取名称
    /// </summary>
    /// <param name="methodId">方法标识</param>
    /// <returns>方法名称，不存在返回 null</returns>
    public string? GetMethodName(int methodId)
    {
        return _methodIdToName.GetValueOrDefault(methodId);
    }

    #endregion

    #region 公共方法 - 调用

    /// <summary>
    /// 创建 Server RPC 调用（客户端到服务器）
    /// </summary>
    /// <param name="methodId">方法标识</param>
    /// <param name="arguments">调用参数</param>
    /// <returns>RPC 调用描述</returns>
    public RpcCall CreateServerRpc(int methodId, ReadOnlySpan<byte> arguments)
    {
        if (!_handlers.ContainsKey(methodId))
        {
            throw new ArgumentException($"RPC 方法 {methodId} 未注册", nameof(methodId));
        }

        var args = arguments.ToArray();
        return new RpcCall
        {
            MethodId = methodId,
            RpcType = RpcType.ServerRpc,
            Arguments = args
        };
    }

    /// <summary>
    /// 创建 Client RPC 调用（服务器到特定客户端）
    /// </summary>
    /// <param name="methodId">方法标识</param>
    /// <param name="targetConnection">目标连接</param>
    /// <param name="arguments">调用参数</param>
    /// <returns>RPC 调用描述</returns>
    public RpcCall CreateClientRpc(int methodId, ConnectionId targetConnection, ReadOnlySpan<byte> arguments)
    {
        if (!_handlers.ContainsKey(methodId))
        {
            throw new ArgumentException($"RPC 方法 {methodId} 未注册", nameof(methodId));
        }

        var args = arguments.ToArray();
        return new RpcCall
        {
            MethodId = methodId,
            RpcType = RpcType.ClientRpc,
            TargetConnection = targetConnection,
            Arguments = args
        };
    }

    /// <summary>
    /// 创建 Multicast RPC 调用（服务器到所有客户端）
    /// </summary>
    /// <param name="methodId">方法标识</param>
    /// <param name="arguments">调用参数</param>
    /// <returns>RPC 调用描述</returns>
    public RpcCall CreateMulticastRpc(int methodId, ReadOnlySpan<byte> arguments)
    {
        if (!_handlers.ContainsKey(methodId))
        {
            throw new ArgumentException($"RPC 方法 {methodId} 未注册", nameof(methodId));
        }

        var args = arguments.ToArray();
        return new RpcCall
        {
            MethodId = methodId,
            RpcType = RpcType.MulticastRpc,
            Arguments = args
        };
    }

    /// <summary>
    /// 创建 Server RPC 调用（泛型参数版本）
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="methodId">方法标识</param>
    /// <param name="argument">调用参数</param>
    /// <returns>RPC 调用描述</returns>
    public RpcCall CreateServerRpc<T>(int methodId, T argument) where T : struct
    {
        var data = _serializer.Serialize(argument);
        return CreateServerRpc(methodId, data);
    }

    /// <summary>
    /// 创建 Client RPC 调用（泛型参数版本）
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="methodId">方法标识</param>
    /// <param name="targetConnection">目标连接</param>
    /// <param name="argument">调用参数</param>
    /// <returns>RPC 调用描述</returns>
    public RpcCall CreateClientRpc<T>(int methodId, ConnectionId targetConnection, T argument) where T : struct
    {
        var data = _serializer.Serialize(argument);
        return CreateClientRpc(methodId, targetConnection, data);
    }

    /// <summary>
    /// 创建 Multicast RPC 调用（泛型参数版本）
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="methodId">方法标识</param>
    /// <param name="argument">调用参数</param>
    /// <returns>RPC 调用描述</returns>
    public RpcCall CreateMulticastRpc<T>(int methodId, T argument) where T : struct
    {
        var data = _serializer.Serialize(argument);
        return CreateMulticastRpc(methodId, data);
    }

    #endregion

    #region 公共方法 - 序列化

    /// <summary>
    /// 将 RPC 调用序列化为字节数组
    /// </summary>
    /// <param name="call">RPC 调用描述</param>
    /// <returns>序列化后的字节数组</returns>
    public byte[] SerializeCall(RpcCall call)
    {
        var headerSize = 1 + 4 + 8;
        var totalSize = headerSize + call.Arguments.Length;
        var buffer = new byte[totalSize];

        buffer[0] = (byte)call.RpcType;
        WriteInt32(buffer, 1, call.MethodId);
        WriteUInt64(buffer, 5, call.TargetConnection.Value);
        call.Arguments.Span.CopyTo(buffer.AsSpan(headerSize));

        return buffer;
    }

    /// <summary>
    /// 从字节数组反序列化 RPC 调用
    /// </summary>
    /// <param name="data">序列化数据</param>
    /// <returns>RPC 调用描述</returns>
    public RpcCall DeserializeCall(ReadOnlySpan<byte> data)
    {
        if (data.Length < 13)
        {
            throw new ArgumentException($"RPC 数据长度不足：{data.Length}，至少需要 13 字节");
        }

        var rpcType = (RpcType)data[0];
        var methodId = ReadInt32(data, 1);
        var targetConnectionValue = ReadUInt64(data, 5);

        var headerSize = 13;
        var argsLength = data.Length - headerSize;
        var args = new byte[argsLength];
        data[headerSize..].CopyTo(args);

        return new RpcCall
        {
            MethodId = methodId,
            RpcType = rpcType,
            TargetConnection = new ConnectionId(targetConnectionValue),
            Arguments = args
        };
    }

    #endregion

    #region 公共方法 - 执行

    /// <summary>
    /// 执行 RPC 调用
    /// </summary>
    /// <param name="caller">调用者连接标识</param>
    /// <param name="call">RPC 调用描述</param>
    public void Execute(ConnectionId caller, RpcCall call)
    {
        if (!_handlers.TryGetValue(call.MethodId, out var handler))
        {
            var methodName = _methodIdToName.GetValueOrDefault(call.MethodId, $"未知方法({call.MethodId})");
            OnRpcFailed?.Invoke(call.MethodId, caller, $"RPC 方法未注册：{methodName}");
            return;
        }

        try
        {
            handler(caller, call.Arguments.Span);
            OnRpcExecuted?.Invoke(call.MethodId, caller);
        }
        catch (Exception ex)
        {
            var methodName = _methodIdToName.GetValueOrDefault(call.MethodId, $"未知方法({call.MethodId})");
            OnRpcFailed?.Invoke(call.MethodId, caller, $"RPC 方法 {methodName} 执行失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 处理接收到的 RPC 数据
    /// </summary>
    /// <param name="caller">调用者连接标识</param>
    /// <param name="data">接收到的 RPC 数据</param>
    public void HandleIncoming(ConnectionId caller, ReadOnlySpan<byte> data)
    {
        var call = DeserializeCall(data);
        Execute(caller, call);
    }

    /// <summary>
    /// 将 RPC 调用加入待发送队列
    /// </summary>
    /// <param name="call">RPC 调用描述</param>
    public void EnqueueCall(RpcCall call)
    {
        _pendingCalls.Add(call);
    }

    /// <summary>
    /// 取出所有待发送的 RPC 调用
    /// </summary>
    /// <returns>待发送的 RPC 调用列表</returns>
    public IReadOnlyList<RpcCall> DrainPendingCalls()
    {
        var calls = _pendingCalls.ToArray();
        _pendingCalls.Clear();
        return calls;
    }

    /// <summary>
    /// 清除所有注册和待处理调用
    /// </summary>
    public void Clear()
    {
        _handlers.Clear();
        _methodNameToId.Clear();
        _methodIdToName.Clear();
        _pendingCalls.Clear();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 写入 32 位整数（小端序）
    /// </summary>
    private static void WriteInt32(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }

    /// <summary>
    /// 读取 32 位整数（小端序）
    /// </summary>
    private static int ReadInt32(ReadOnlySpan<byte> data, int offset)
    {
        return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
    }

    /// <summary>
    /// 写入 64 位无符号整数（小端序）
    /// </summary>
    private static void WriteUInt64(byte[] buffer, int offset, ulong value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
        buffer[offset + 4] = (byte)(value >> 32);
        buffer[offset + 5] = (byte)(value >> 40);
        buffer[offset + 6] = (byte)(value >> 48);
        buffer[offset + 7] = (byte)(value >> 56);
    }

    /// <summary>
    /// 读取 64 位无符号整数（小端序）
    /// </summary>
    private static ulong ReadUInt64(ReadOnlySpan<byte> data, int offset)
    {
        var low = (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
        var high = (uint)(data[offset + 4] | (data[offset + 5] << 8) | (data[offset + 6] << 16) | (data[offset + 7] << 24));
        return ((ulong)high << 32) | low;
    }

    #endregion
}

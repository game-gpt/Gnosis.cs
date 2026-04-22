using Gnosis.Core;
using Gnosis.Core.Event;
using Gnosis.Network.Serialization;
using Gnosis.Network.Transport;
using Gnosis.Security.AntiCheat;

namespace Gnosis.Network.RPC;

public sealed class RateLimitedRpcSystem
{
    #region 字段

    private readonly RpcSystem _innerSystem;
    private readonly IRateLimiter _rateLimiter;
    private readonly Dictionary<int, string> _methodIdToActionType = new();

    #endregion

    #region 属性

    public int RegisteredMethodCount => _innerSystem.RegisteredMethodCount;

    public int PendingCallCount => _innerSystem.PendingCallCount;

    #endregion

    #region 事件

    public event Action<int, ConnectionId>? OnRpcExecuted
    {
        add => _innerSystem.OnRpcExecuted += value;
        remove => _innerSystem.OnRpcExecuted -= value;
    }

    public event Action<int, ConnectionId, string>? OnRpcFailed
    {
        add => _innerSystem.OnRpcFailed += value;
        remove => _innerSystem.OnRpcFailed -= value;
    }

    public event Action<ConnectionId, int>? OnRateLimitExceeded;

    #endregion

    #region 构造函数

    public RateLimitedRpcSystem(RpcSystem innerSystem, IRateLimiter rateLimiter)
    {
        _innerSystem = innerSystem ?? throw new ArgumentNullException(nameof(innerSystem));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
    }

    public RateLimitedRpcSystem(IMessageSerializer serializer, int maxCallsPerSecond = 30)
        : this(new RpcSystem(serializer), new RateLimiterImpl(maxCallsPerSecond, 1.0))
    {
    }

    #endregion

    #region 公共方法 - 注册

    public int Register(string methodName, RpcHandler handler, string? actionType = null)
    {
        var methodId = _innerSystem.Register(methodName, handler);
        _methodIdToActionType[methodId] = actionType ?? methodName;
        return methodId;
    }

    public bool Unregister(string methodName)
    {
        var methodId = _innerSystem.GetMethodId(methodName);
        if (methodId >= 0)
        {
            _methodIdToActionType.Remove(methodId);
        }

        return _innerSystem.Unregister(methodName);
    }

    public int GetMethodId(string methodName)
    {
        return _innerSystem.GetMethodId(methodName);
    }

    public string? GetMethodName(int methodId)
    {
        return _innerSystem.GetMethodName(methodId);
    }

    #endregion

    #region 公共方法 - 调用

    public RpcCall CreateServerRpc(int methodId, ReadOnlySpan<byte> arguments)
    {
        return _innerSystem.CreateServerRpc(methodId, arguments);
    }

    public RpcCall CreateClientRpc(int methodId, ConnectionId targetConnection, ReadOnlySpan<byte> arguments)
    {
        return _innerSystem.CreateClientRpc(methodId, targetConnection, arguments);
    }

    public RpcCall CreateMulticastRpc(int methodId, ReadOnlySpan<byte> arguments)
    {
        return _innerSystem.CreateMulticastRpc(methodId, arguments);
    }

    public RpcCall CreateServerRpc<T>(int methodId, T argument) where T : struct
    {
        return _innerSystem.CreateServerRpc(methodId, argument);
    }

    public RpcCall CreateClientRpc<T>(int methodId, ConnectionId targetConnection, T argument) where T : struct
    {
        return _innerSystem.CreateClientRpc(methodId, targetConnection, argument);
    }

    public RpcCall CreateMulticastRpc<T>(int methodId, T argument) where T : struct
    {
        return _innerSystem.CreateMulticastRpc(methodId, argument);
    }

    #endregion

    #region 公共方法 - 执行

    public void Execute(ConnectionId caller, PlayerId callerPlayerId, RpcCall call)
    {
        if (!_methodIdToActionType.TryGetValue(call.MethodId, out var actionType))
        {
            actionType = call.MethodId.ToString();
        }

        if (!_rateLimiter.IsAllowed(callerPlayerId, actionType))
        {
            OnRateLimitExceeded?.Invoke(caller, call.MethodId);
            _rateLimiter.RecordAction(callerPlayerId, actionType);
            return;
        }

        _rateLimiter.RecordAction(callerPlayerId, actionType);
        _innerSystem.Execute(caller, call);
    }

    public void HandleIncoming(ConnectionId caller, PlayerId callerPlayerId, ReadOnlySpan<byte> data)
    {
        var call = _innerSystem.DeserializeCall(data);
        Execute(caller, callerPlayerId, call);
    }

    public byte[] SerializeCall(RpcCall call)
    {
        return _innerSystem.SerializeCall(call);
    }

    public RpcCall DeserializeCall(ReadOnlySpan<byte> data)
    {
        return _innerSystem.DeserializeCall(data);
    }

    public void EnqueueCall(RpcCall call)
    {
        _innerSystem.EnqueueCall(call);
    }

    public IReadOnlyList<RpcCall> DrainPendingCalls()
    {
        return _innerSystem.DrainPendingCalls();
    }

    public void Clear()
    {
        _innerSystem.Clear();
        _methodIdToActionType.Clear();
    }

    #endregion
}

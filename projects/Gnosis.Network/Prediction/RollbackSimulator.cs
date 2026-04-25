using Gnosis.Core;

namespace Gnosis.Network.Prediction;

public delegate byte[] SerializeStateDelegate();
public delegate void DeserializeStateDelegate(byte[] data);
public delegate void ApplyInputDelegate(byte[] inputData);
public delegate void SimulateFrameDelegate(float delta);

public sealed class RollbackSimulator<TSnapshot, TInput>
    where TSnapshot : IStateSnapshot
    where TInput : IInputPayload
{
    #region 字段

    private readonly StateSnapshotBuffer<TSnapshot> _stateBuffer;
    private readonly InputBuffer<TInput> _inputBuffer;
    private readonly PredictionSystem _predictionSystem;

    private SerializeStateDelegate? _serializeState;
    private DeserializeStateDelegate? _deserializeState;
    private ApplyInputDelegate? _applyInput;
    private SimulateFrameDelegate? _simulateFrame;

    private int _currentFrame;
    private int _lastAckedFrame;

    #endregion

    #region 属性

    public int CurrentFrame => _currentFrame;

    public int LastAckedFrame => _lastAckedFrame;

    public StateSnapshotBuffer<TSnapshot> StateBuffer => _stateBuffer;

    public InputBuffer<TInput> InputBuffer => _inputBuffer;

    public PredictionSystem Prediction => _predictionSystem;

    public float ReconciliationThreshold
    {
        get => _predictionSystem.ReconciliationThreshold;
        set => _predictionSystem.ReconciliationThreshold = value;
    }

    public bool SmoothCorrectionEnabled
    {
        get => _predictionSystem.SmoothCorrectionEnabled;
        set => _predictionSystem.SmoothCorrectionEnabled = value;
    }

    #endregion

    #region 事件

    public event Action<int, int>? OnRollback;

    public event Action<int, float>? OnReconciliationError;

    public event Action<int>? OnStateRestored;

    #endregion

    #region 枢纽注入

    public void SetSerializeState(SerializeStateDelegate fn) => _serializeState = fn;
    public void SetDeserializeState(DeserializeStateDelegate fn) => _deserializeState = fn;
    public void SetApplyInput(ApplyInputDelegate fn) => _applyInput = fn;
    public void SetSimulateFrame(SimulateFrameDelegate fn) => _simulateFrame = fn;

    #endregion

    #region 构造函数

    public RollbackSimulator(int stateBufferSize = 64, int inputBufferSize = 128)
    {
        _stateBuffer = new StateSnapshotBuffer<TSnapshot>(stateBufferSize);
        _inputBuffer = new InputBuffer<TInput>(inputBufferSize);
        _predictionSystem = new PredictionSystem();
        _lastAckedFrame = -1;
    }

    #endregion

    #region 公开方法 - 客户端预测

    public void ClientPredict(int frame, TInput input, float delta)
    {
        _inputBuffer.Record(frame, input);

        if (_applyInput != null)
        {
            _applyInput(input.Serialize());
        }

        if (_simulateFrame != null)
        {
            _simulateFrame(delta);
        }

        if (_serializeState != null)
        {
            var stateData = _serializeState();
            _predictionSystem.RecordPrediction(frame, stateData);
        }

        _currentFrame = frame;
    }

    #endregion

    #region 公开方法 - 服务器和解

    public bool ServerReconcile(int serverFrame, byte[] serverState)
    {
        if (_predictionSystem.Reconcile(serverFrame, serverState))
        {
            OnReconciliationError?.Invoke(serverFrame, _predictionSystem.LastReconciliationError);

            PerformRollback(serverFrame, serverState);
            return true;
        }

        if (serverFrame > _lastAckedFrame)
        {
            _lastAckedFrame = serverFrame;
            _predictionSystem.ClearHistoryBefore(serverFrame);
            _stateBuffer.ClearBefore(serverFrame);
        }

        return false;
    }

    #endregion

    #region 公开方法 - 回滚重模拟

    public void PerformRollback(int targetFrame, byte[] targetState)
    {
        var rollbackFrom = _currentFrame;
        var rollbackTo = targetFrame;

        if (_deserializeState != null)
        {
            _deserializeState(targetState);
            OnStateRestored?.Invoke(targetFrame);
        }

        OnRollback?.Invoke(rollbackFrom, rollbackTo);

        for (int frame = targetFrame; frame <= _currentFrame; frame++)
        {
            var input = _inputBuffer.Get(frame);
            if (input != null && _applyInput != null)
            {
                _applyInput(input.Serialize());
            }

            if (_simulateFrame != null)
            {
                _simulateFrame(1f / 60f);
            }

            if (_serializeState != null)
            {
                var newStateData = _serializeState();
                _predictionSystem.RecordPrediction(frame, newStateData);
            }
        }
    }

    #endregion

    #region 公开方法 - 服务器端

    public void ServerProcessInput(int frame, TInput input, float delta)
    {
        _inputBuffer.Record(frame, input);

        if (_applyInput != null)
        {
            _applyInput(input.Serialize());
        }

        if (_simulateFrame != null)
        {
            _simulateFrame(delta);
        }

        _currentFrame = frame;
    }

    public byte[] ServerGetState()
    {
        if (_serializeState == null)
        {
            return [];
        }

        return _serializeState();
    }

    #endregion

    #region 公开方法 - 清理

    public void Clear()
    {
        _stateBuffer.Clear();
        _inputBuffer.Clear();
        _predictionSystem.Clear();
        _currentFrame = 0;
        _lastAckedFrame = -1;
    }

    #endregion
}

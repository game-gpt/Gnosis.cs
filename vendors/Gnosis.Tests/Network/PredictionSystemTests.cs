using Gnosis.Core.Event;
using Gnosis.Network.Prediction;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.Network;

public class PredictionSystemTests : GnosisTester
{
    private PredictionSystem _system = null!;

    public override void Setup()
    {
        base.Setup();
        _system = new PredictionSystem();
    }

    public override void Teardown()
    {
        _system.Clear();
        base.Teardown();
    }

    [Test]
    public void RecordPrediction_记录后历史数量增加()
    {
        _system.RecordPrediction(1, [1, 2, 3]);

        Assert.That(_system.HistoryCount, Is.EqualTo(1));
    }

    [Test]
    public void RecordPrediction_多次记录后历史数量增加()
    {
        _system.RecordPrediction(1, [1, 2, 3]);
        _system.RecordPrediction(2, [4, 5, 6]);
        _system.RecordPrediction(3, [7, 8, 9]);

        Assert.That(_system.HistoryCount, Is.EqualTo(3));
    }

    [Test]
    public void RecordPrediction_超过缓冲区大小时自动清理旧记录()
    {
        _system.HistoryBufferSize = 3;

        _system.RecordPrediction(1, [1]);
        _system.RecordPrediction(2, [2]);
        _system.RecordPrediction(3, [3]);
        _system.RecordPrediction(4, [4]);

        Assert.That(_system.HistoryCount, Is.EqualTo(3));
        Assert.That(_system.GetPredictedState(1), Is.Null);
    }

    [Test]
    public void Reconcile_服务器状态与预测一致时返回False()
    {
        _system.RecordPrediction(1, [1, 2, 3]);

        var needsCorrection = _system.Reconcile(1, [1, 2, 3]);

        Assert.That(needsCorrection, Is.False);
    }

    [Test]
    public void Reconcile_服务器状态与预测不一致时返回True()
    {
        _system.ReconciliationThreshold = 0.01f;
        _system.RecordPrediction(1, [1, 2, 3]);

        var needsCorrection = _system.Reconcile(1, [10, 20, 30]);

        Assert.That(needsCorrection, Is.True);
    }

    [Test]
    public void Reconcile_误差在阈值内不校正()
    {
        _system.ReconciliationThreshold = 5.0f;
        _system.RecordPrediction(1, [1, 2, 3]);

        var needsCorrection = _system.Reconcile(1, [2, 3, 4]);

        Assert.That(needsCorrection, Is.False);
    }

    [Test]
    public void Reconcile_无预测历史时返回False()
    {
        var needsCorrection = _system.Reconcile(1, [1, 2, 3]);

        Assert.That(needsCorrection, Is.False);
    }

    [Test]
    public void Reconcile_校正后LastReconciliationError更新()
    {
        _system.ReconciliationThreshold = 0.01f;
        _system.RecordPrediction(1, [1, 2, 3]);

        _system.Reconcile(1, [10, 20, 30]);

        Assert.That(_system.LastReconciliationError, Is.GreaterThan(0f));
    }

    [Test]
    public void Reconcile_触发OnPredictionError事件()
    {
        _system.ReconciliationThreshold = 0.01f;
        _system.RecordPrediction(1, [1, 2, 3]);

        int errorFrame = -1;
        float errorValue = 0f;
        _system.OnPredictionError += (frame, error) => { errorFrame = frame; errorValue = error; };

        _system.Reconcile(1, [10, 20, 30]);

        Assert.That(errorFrame, Is.EqualTo(1));
        Assert.That(errorValue, Is.GreaterThan(0f));
    }

    [Test]
    public void ReconcileEntity_实体预测与服务器不一致时返回True()
    {
        _system.RecordEntityPrediction(EntityId.New(), [1, 2, 3]);

        var needsCorrection = _system.ReconcileEntity(EntityId.New(), [10, 20, 30]);

        Assert.That(needsCorrection, Is.False);
    }

    [Test]
    public void ReconcileEntity_同一实体预测与服务器不一致时返回True()
    {
        var entityId = EntityId.New();
        _system.RecordEntityPrediction(entityId, [1, 2, 3]);

        _system.ReconciliationThreshold = 0.01f;
        var needsCorrection = _system.ReconcileEntity(entityId, [10, 20, 30]);

        Assert.That(needsCorrection, Is.True);
    }

    [Test]
    public void GetPredictedState_返回已记录的预测状态()
    {
        _system.RecordPrediction(5, [10, 20, 30]);

        var state = _system.GetPredictedState(5);

        Assert.That(state, Is.Not.Null);
        Assert.That(state, Is.EqualTo(new byte[] { 10, 20, 30 }));
    }

    [Test]
    public void GetPredictedState_不存在时返回Null()
    {
        var state = _system.GetPredictedState(999);

        Assert.That(state, Is.Null);
    }

    [Test]
    public void ClearHistoryBefore_清除指定帧之前的历史()
    {
        _system.RecordPrediction(1, [1]);
        _system.RecordPrediction(2, [2]);
        _system.RecordPrediction(3, [3]);

        _system.ClearHistoryBefore(3);

        Assert.That(_system.HistoryCount, Is.EqualTo(1));
        Assert.That(_system.GetPredictedState(1), Is.Null);
        Assert.That(_system.GetPredictedState(2), Is.Null);
        Assert.That(_system.GetPredictedState(3), Is.Not.Null);
    }

    [Test]
    public void Clear_清除后历史数量为零()
    {
        _system.RecordPrediction(1, [1, 2, 3]);
        _system.RecordPrediction(2, [4, 5, 6]);

        _system.Clear();

        Assert.That(_system.HistoryCount, Is.EqualTo(0));
        Assert.That(_system.LastReconciliationError, Is.EqualTo(0f));
    }

    [Test]
    public void HistoryBufferSize_设置后生效()
    {
        _system.HistoryBufferSize = 10;

        Assert.That(_system.HistoryBufferSize, Is.EqualTo(10));
    }

    [Test]
    public void HistoryBufferSize_最小值为一()
    {
        _system.HistoryBufferSize = 0;

        Assert.That(_system.HistoryBufferSize, Is.EqualTo(1));
    }

    [Test]
    public void Reconcile_长度不同时返回True()
    {
        _system.ReconciliationThreshold = 0.01f;
        _system.RecordPrediction(1, [1, 2, 3]);

        var needsCorrection = _system.Reconcile(1, [1, 2, 3, 4]);

        Assert.That(needsCorrection, Is.True);
    }
}

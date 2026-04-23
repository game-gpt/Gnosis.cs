using System.Collections;
using Gnosis.Runtime.Coroutine;
using Gnosis.Runtime.Interop;
using Gnosis.Runtime.VM;
using NUnit.Framework;

namespace Gnosis.Runtime;

[TestFixture]
public class SandboxTests
{
    [Test]
    public void ScriptPermissionSet_CreateDefault_HasExpectedPermissions()
    {
        var permissions = Gnosis.Runtime.Sandbox.ScriptPermissionSet.CreateDefault();
        Assert.That(permissions.Has(Gnosis.Runtime.Sandbox.ScriptPermission.NativeCall), Is.True);
        Assert.That(permissions.Has(Gnosis.Runtime.Sandbox.ScriptPermission.EntityAccess), Is.True);
        Assert.That(permissions.Has(Gnosis.Runtime.Sandbox.ScriptPermission.Reflection), Is.False);
    }

    [Test]
    public void ScriptPermissionSet_Demand_ThrowsWhenMissing()
    {
        var permissions = new Gnosis.Runtime.Sandbox.ScriptPermissionSet("test");
        Assert.Throws<Gnosis.Runtime.Sandbox.SandboxViolationException>(() =>
            permissions.Demand(Gnosis.Runtime.Sandbox.ScriptPermission.NetworkAccess));
    }

    [Test]
    public void ResourceQuota_Default_HasExpectedLimits()
    {
        var quota = Gnosis.Runtime.Sandbox.ResourceQuota.Default();
        Assert.That(quota.MaxMemoryBytes, Is.EqualTo(64 * 1024 * 1024));
        Assert.That(quota.MaxObjectCount, Is.EqualTo(10000));
    }

    [Test]
    public void ResourceQuota_CanAllocateMemory_TracksUsage()
    {
        var quota = new Gnosis.Runtime.Sandbox.ResourceQuota { MaxMemoryBytes = 1024 };
        Assert.That(quota.CanAllocateMemory(512), Is.True);
        quota.RecordAllocation(512);
        Assert.That(quota.CurrentMemoryUsage, Is.EqualTo(512));
        Assert.That(quota.CanAllocateMemory(1), Is.False);
    }

    [Test]
    public void Sandbox_DemandPermission_ThrowsOnMissing()
    {
        var permissions = Gnosis.Runtime.Sandbox.ScriptPermissionSet.CreateStrict();
        var quota = Gnosis.Runtime.Sandbox.ResourceQuota.Default();
        using var sandbox = new Gnosis.Runtime.Sandbox.Sandbox("test-script", permissions, quota);
        Assert.Throws<Gnosis.Runtime.Sandbox.SandboxViolationException>(() =>
            sandbox.DemandPermission(Gnosis.Runtime.Sandbox.ScriptPermission.EntityAccess));
    }
}

[TestFixture]
public class CoroutineTests
{
    [Test]
    public void Coroutine_StartsRunning()
    {
        var scheduler = new CoroutineScheduler();
        var coroutine = scheduler.Start(SimpleRoutine());
        Assert.That(coroutine.IsRunning, Is.True);
        Assert.That(coroutine.IsComplete, Is.False);
    }

    [Test]
    public void Coroutine_CompletesWhenDone()
    {
        var scheduler = new CoroutineScheduler();
        var coroutine = scheduler.Start(SimpleRoutine());
        scheduler.Update(0.016f);
        scheduler.Update(0.016f);
        scheduler.Update(0.016f);
        Assert.That(coroutine.IsComplete, Is.True);
    }

    [Test]
    public void Coroutine_Stop_MarksComplete()
    {
        var scheduler = new CoroutineScheduler();
        var coroutine = scheduler.Start(EndlessRoutine());
        scheduler.Update(0.016f);
        coroutine.Stop();
        Assert.That(coroutine.IsComplete, Is.True);
    }

    [Test]
    public void Coroutine_PauseAndResume_Works()
    {
        var scheduler = new CoroutineScheduler();
        var coroutine = scheduler.Start(SimpleRoutine());
        coroutine.Pause();
        Assert.That(coroutine.IsPaused, Is.True);
        coroutine.Resume();
        Assert.That(coroutine.IsRunning, Is.True);
    }

    [Test]
    public void CoroutineScheduler_StopAll_ClearsAll()
    {
        var scheduler = new CoroutineScheduler();
        scheduler.Start(SimpleRoutine());
        scheduler.Start(SimpleRoutine());
        scheduler.StopAll();
        Assert.That(scheduler.ActiveCount, Is.EqualTo(0));
    }

    [Test]
    public void WaitForSeconds_YieldsForCorrectDuration()
    {
        var wait = new WaitForSeconds(1.0f);
        wait.Update(0.5f);
        Assert.That(wait.IsDone, Is.False);
        wait.Update(0.5f);
        Assert.That(wait.IsDone, Is.True);
    }

    [Test]
    public void WaitForFrame_CountsDown()
    {
        var wait = new WaitForFrame(3);
        wait.Update(0.016f);
        Assert.That(wait.IsDone, Is.False);
        wait.Update(0.016f);
        wait.Update(0.016f);
        Assert.That(wait.IsDone, Is.True);
    }

    [Test]
    public void WaitForCondition_ResolvesWhenMet()
    {
        var value = 0;
        var wait = new WaitForCondition(() => value >= 5);
        wait.Update(0.016f);
        Assert.That(wait.IsDone, Is.False);
        value = 5;
        wait.Update(0.016f);
        Assert.That(wait.IsDone, Is.True);
    }

    private static IEnumerator SimpleRoutine()
    {
        yield return null;
        yield return null;
    }

    private static IEnumerator EndlessRoutine()
    {
        while (true)
        {
            yield return null;
        }
    }
}

[TestFixture]
public class ObjectModelTests
{
    [Test]
    public void GGObject_SetGetField_WorksCorrectly()
    {
        var obj = new GGObject("TestType");
        obj.SetField("name", GGValue.FromString(new GGString("hello")));
        obj.SetField("count", GGValue.FromInt(42));
        Assert.That(obj.GetField("name").StringValue?.Value, Is.EqualTo("hello"));
        Assert.That(obj.GetField("count").IntValue, Is.EqualTo(42));
        Assert.That(obj.GetField("nonexistent").IsNull, Is.True);
        Assert.That(obj.HasField("name"), Is.True);
        Assert.That(obj.HasField("missing"), Is.False);
    }

    [Test]
    public void GGString_PoolInternsStrings()
    {
        var pool = new StringPool();
        var str1 = pool.Intern("hello");
        var str2 = pool.Intern("hello");
        Assert.That(str1, Is.SameAs(str2));
        Assert.That(str1.Value, Is.EqualTo("hello"));
    }

    [Test]
    public void GGArray_AddAndGet_WorksCorrectly()
    {
        var arr = new GGArray(4);
        arr.Add(GGValue.FromInt(10));
        arr.Add(GGValue.FromInt(20));
        arr.Add(GGValue.FromInt(30));
        Assert.That(arr.Count, Is.EqualTo(3));
        Assert.That(arr[0].IntValue, Is.EqualTo(10));
        Assert.That(arr[1].IntValue, Is.EqualTo(20));
        Assert.That(arr[2].IntValue, Is.EqualTo(30));
    }

    [Test]
    public void GGArray_Set_WorksCorrectly()
    {
        var arr = new GGArray(4);
        arr.Add(GGValue.FromInt(10));
        arr.Add(GGValue.FromInt(20));
        arr[1] = GGValue.FromInt(99);
        Assert.That(arr[1].IntValue, Is.EqualTo(99));
    }

    [Test]
    public void GGStruct_FieldAccess_WorksCorrectly()
    {
        var str = new GGStruct("Vec3", new[] { "x", "y", "z" });
        str.SetField(0, GGValue.FromFloat(1.0));
        str.SetField(1, GGValue.FromFloat(2.0));
        str.SetField(2, GGValue.FromFloat(3.0));
        Assert.That(str.GetField(0).FloatValue, Is.EqualTo(1.0));
        Assert.That(str.GetField(1).FloatValue, Is.EqualTo(2.0));
        Assert.That(str.GetFieldIndex("y"), Is.EqualTo(1));
    }

    [Test]
    public void GGClosure_UpvalueAccess_WorksCorrectly()
    {
        var closure = new GGClosure(100, 2);
        closure.SetUpvalue(0, GGValue.FromInt(42));
        closure.SetUpvalue(1, GGValue.FromString(new GGString("hello")));
        Assert.That(closure.GetUpvalue(0).IntValue, Is.EqualTo(42));
        Assert.That(closure.GetUpvalue(1).StringValue?.Value, Is.EqualTo("hello"));
        Assert.That(closure.FunctionAddress, Is.EqualTo(100));
    }

    [Test]
    public void GGValue_Types_WorksCorrectly()
    {
        var intVal = GGValue.FromInt(42);
        Assert.That(intVal.Type, Is.EqualTo(GGValueType.Int));
        Assert.That(intVal.IntValue, Is.EqualTo(42));

        var floatVal = GGValue.FromFloat(3.14);
        Assert.That(floatVal.Type, Is.EqualTo(GGValueType.Float));
        Assert.That(floatVal.FloatValue, Is.EqualTo(3.14));

        var boolVal = GGValue.FromBool(true);
        Assert.That(boolVal.Type, Is.EqualTo(GGValueType.Bool));
        Assert.That(boolVal.BoolValue, Is.True);

        var nullVal = GGValue.Null;
        Assert.That(nullVal.Type, Is.EqualTo(GGValueType.Null));
        Assert.That(nullVal.IsNull, Is.True);
    }

    [Test]
    public void GGValue_NaNBoxing_FloatPreserved()
    {
        var val = GGValue.FromFloat(double.NaN);
        Assert.That(val.Type, Is.EqualTo(GGValueType.Float));
        Assert.That(double.IsNaN(val.FloatValue), Is.True);
    }

    [Test]
    public void GGValue_NaNBoxing_NegativeFloat()
    {
        var val = GGValue.FromFloat(-3.14);
        Assert.That(val.Type, Is.EqualTo(GGValueType.Float));
        Assert.That(val.FloatValue, Is.EqualTo(-3.14));
    }

    [Test]
    public void GGValue_NaNBoxing_ZeroFloat()
    {
        var val = GGValue.FromFloat(0.0);
        Assert.That(val.Type, Is.EqualTo(GGValueType.Float));
        Assert.That(val.FloatValue, Is.EqualTo(0.0));
    }

    [Test]
    public void GGValue_Entity_WorksCorrectly()
    {
        var val = GGValue.FromEntity(42);
        Assert.That(val.Type, Is.EqualTo(GGValueType.Entity));
        Assert.That(val.EntityId, Is.EqualTo(42));
        Assert.That(val.IsEntity, Is.True);
    }

    [Test]
    public void GGValue_IsTruthy_WorksCorrectly()
    {
        Assert.That(GGValue.FromInt(0).IsTruthy(), Is.False);
        Assert.That(GGValue.FromInt(1).IsTruthy(), Is.True);
        Assert.That(GGValue.FromBool(false).IsTruthy(), Is.False);
        Assert.That(GGValue.FromBool(true).IsTruthy(), Is.True);
        Assert.That(GGValue.Null.IsTruthy(), Is.False);
        Assert.That(GGValue.FromFloat(0.0).IsTruthy(), Is.False);
        Assert.That(GGValue.FromFloat(1.0).IsTruthy(), Is.True);
    }

    [Test]
    public void GGValue_Equality_WorksCorrectly()
    {
        var a = GGValue.FromInt(42);
        var b = GGValue.FromInt(42);
        var c = GGValue.FromInt(43);
        Assert.That(a == b, Is.True);
        Assert.That(a != c, Is.True);
        Assert.That(a.Equals(b), Is.True);
    }
}

[TestFixture]
public class MemoryManagerTests
{
    [Test]
    public void MemoryManager_AllocateAndRetrieve_WorksCorrectly()
    {
        var mm = new MemoryManager();
        var obj = new GGObject("TestType");
        var id = mm.Allocate(obj);
        var retrieved = mm.GetObject<GGObject>(id);
        Assert.That(retrieved, Is.SameAs(obj));
    }

    [Test]
    public void MemoryManager_Collect_RemovesUnreachable()
    {
        var mm = new MemoryManager();
        var obj = new GGObject("TestType");
        var id = mm.Allocate(obj);
        mm.AddRoot(obj);
        mm.Collect();
        Assert.That(mm.GetObject<GGObject>(id), Is.Not.Null);
        mm.RemoveRoot(obj);
        mm.Collect();
        Assert.That(mm.GetObject<GGObject>(id), Is.Null);
    }

    [Test]
    public void MemoryManager_CollectsCircularReferences()
    {
        var mm = new MemoryManager();
        var objA = new GGObject("A");
        var objB = new GGObject("B");
        objA.SetField("ref", GGValue.FromObject(objB));
        objB.SetField("ref", GGValue.FromObject(objA));
        var idA = mm.Allocate(objA);
        var idB = mm.Allocate(objB);
        mm.AddRoot(objA);
        mm.Collect();
        Assert.That(mm.GetObject<GGObject>(idA), Is.Not.Null);
        Assert.That(mm.GetObject<GGObject>(idB), Is.Not.Null);
        mm.RemoveRoot(objA);
        mm.Collect();
        Assert.That(mm.GetObject<GGObject>(idA), Is.Null);
        Assert.That(mm.GetObject<GGObject>(idB), Is.Null);
    }
}

[TestFixture]
public class InteropTests
{
    [Test]
    public void Marshaller_ToGGValue_ConvertsPrimitives()
    {
        var intVal = Marshaller.ToGGValue(42);
        Assert.That(intVal.IsInt, Is.True);
        Assert.That(intVal.IntValue, Is.EqualTo(42));

        var floatVal = Marshaller.ToGGValue(3.14);
        Assert.That(floatVal.IsFloat, Is.True);
        Assert.That(floatVal.FloatValue, Is.EqualTo(3.14));

        var boolVal = Marshaller.ToGGValue(true);
        Assert.That(boolVal.IsBool, Is.True);
        Assert.That(boolVal.BoolValue, Is.True);

        var nullVal = Marshaller.ToGGValue(null);
        Assert.That(nullVal.IsNull, Is.True);
    }

    [Test]
    public void Marshaller_FromGGValue_ConvertsBack()
    {
        Assert.That(Marshaller.FromGGValue(GGValue.FromInt(42)), Is.EqualTo(42L));
        Assert.That(Marshaller.FromGGValue(GGValue.FromFloat(3.14)), Is.EqualTo(3.14));
        Assert.That(Marshaller.ToInt64(GGValue.FromInt(42)), Is.EqualTo(42));
        Assert.That(Marshaller.ToFloat64(GGValue.FromFloat(3.14)), Is.EqualTo(3.14));
        Assert.That(Marshaller.ToBool(GGValue.FromInt(1)), Is.True);
    }

    [Test]
    public void ExceptionBridge_WrapException_MapsCorrectly()
    {
        var ex1 = new DivideByZeroException("test");
        var vm1 = ExceptionBridge.WrapException(ex1);
        Assert.That(vm1, Is.TypeOf<VMDivideByZeroException>());

        var ex2 = new InvalidOperationException("test");
        var vm2 = ExceptionBridge.WrapException(ex2);
        Assert.That(vm2, Is.TypeOf<VMRuntimeException>());
    }

    [Test]
    public void ExceptionBridge_WrapException_PreservesVMException()
    {
        var original = new VMRuntimeException("original");
        var wrapped = ExceptionBridge.WrapException(original);
        Assert.That(wrapped, Is.SameAs(original));
    }
}

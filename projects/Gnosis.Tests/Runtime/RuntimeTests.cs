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
        Assert.That(permissions.Has(Gnosis.Runtime.Sandbox.ScriptPermission.HotReloadTrigger), Is.False);
    }

    [Test]
    public void ScriptPermissionSet_GrantAndRevoke_WorksCorrectly()
    {
        var permissions = new Gnosis.Runtime.Sandbox.ScriptPermissionSet("test");

        Assert.That(permissions.Has(Gnosis.Runtime.Sandbox.ScriptPermission.NetworkAccess), Is.False);

        permissions.Grant(Gnosis.Runtime.Sandbox.ScriptPermission.NetworkAccess);
        Assert.That(permissions.Has(Gnosis.Runtime.Sandbox.ScriptPermission.NetworkAccess), Is.True);

        permissions.Revoke(Gnosis.Runtime.Sandbox.ScriptPermission.NetworkAccess);
        Assert.That(permissions.Has(Gnosis.Runtime.Sandbox.ScriptPermission.NetworkAccess), Is.False);
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
        Assert.That(quota.MaxInstructionCount, Is.EqualTo(10_000_000));
        Assert.That(quota.MaxStackDepth, Is.EqualTo(256));
        Assert.That(quota.MaxCoroutineCount, Is.EqualTo(100));
    }

    [Test]
    public void ResourceQuota_CanAllocateMemory_TracksUsage()
    {
        var quota = new Gnosis.Runtime.Sandbox.ResourceQuota { MaxMemoryBytes = 1024 };

        Assert.That(quota.CanAllocateMemory(512), Is.True);
        quota.RecordAllocation(512);
        Assert.That(quota.CurrentMemoryUsage, Is.EqualTo(512));

        Assert.That(quota.CanAllocateMemory(512), Is.True);
        quota.RecordAllocation(512);
        Assert.That(quota.CurrentMemoryUsage, Is.EqualTo(1024));

        Assert.That(quota.CanAllocateMemory(1), Is.False);
    }

    [Test]
    public void ResourceQuota_CanCreateObject_RespectsLimit()
    {
        var quota = new Gnosis.Runtime.Sandbox.ResourceQuota { MaxObjectCount = 2 };

        Assert.That(quota.CanCreateObject(), Is.True);
        quota.RecordObjectCreated();
        Assert.That(quota.CanCreateObject(), Is.True);
        quota.RecordObjectCreated();
        Assert.That(quota.CanCreateObject(), Is.False);
    }

    [Test]
    public void ResourceQuota_CanExecuteInstruction_RespectsLimit()
    {
        var quota = new Gnosis.Runtime.Sandbox.ResourceQuota { MaxInstructionCount = 3 };

        Assert.That(quota.CanExecuteInstruction(), Is.True);
        quota.RecordInstructionExecuted();
        Assert.That(quota.CanExecuteInstruction(), Is.True);
        quota.RecordInstructionExecuted();
        Assert.That(quota.CanExecuteInstruction(), Is.True);
        quota.RecordInstructionExecuted();
        Assert.That(quota.CanExecuteInstruction(), Is.False);
    }

    [Test]
    public void ResourceQuota_CanPushFrame_RespectsLimit()
    {
        var quota = new Gnosis.Runtime.Sandbox.ResourceQuota { MaxStackDepth = 4 };

        Assert.That(quota.CanPushFrame(3), Is.True);
        Assert.That(quota.CanPushFrame(4), Is.False);
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

    [Test]
    public void Sandbox_TryAllocateMemory_RespectsQuota()
    {
        var permissions = Gnosis.Runtime.Sandbox.ScriptPermissionSet.CreateDefault();
        var quota = new Gnosis.Runtime.Sandbox.ResourceQuota { MaxMemoryBytes = 100 };
        using var sandbox = new Gnosis.Runtime.Sandbox.Sandbox("test-script", permissions, quota);

        Assert.That(sandbox.TryAllocateMemory(50), Is.True);
        Assert.That(sandbox.TryAllocateMemory(50), Is.True);
        Assert.That(sandbox.TryAllocateMemory(1), Is.False);
    }

    [Test]
    public void Sandbox_NativeFunctionControl_WorksCorrectly()
    {
        var permissions = Gnosis.Runtime.Sandbox.ScriptPermissionSet.CreateDefault();
        var quota = Gnosis.Runtime.Sandbox.ResourceQuota.Unlimited();
        using var sandbox = new Gnosis.Runtime.Sandbox.Sandbox("test-script", permissions, quota);

        Assert.That(sandbox.CanCallNativeFunction("print"), Is.True);

        sandbox.DenyNativeFunction("print");
        Assert.That(sandbox.CanCallNativeFunction("print"), Is.False);

        sandbox.AllowNativeFunction("print");
        Assert.That(sandbox.CanCallNativeFunction("print"), Is.True);
    }

    [Test]
    public void Sandbox_IsolatedStorage_WorksCorrectly()
    {
        var permissions = Gnosis.Runtime.Sandbox.ScriptPermissionSet.CreateDefault();
        var quota = Gnosis.Runtime.Sandbox.ResourceQuota.Unlimited();
        using var sandbox = new Gnosis.Runtime.Sandbox.Sandbox("test-script", permissions, quota);

        sandbox.SetData("key1", 42);
        sandbox.SetData("key2", "hello");

        Assert.That(sandbox.GetData<int>("key1"), Is.EqualTo(42));
        Assert.That(sandbox.GetData<string>("key2"), Is.EqualTo("hello"));
        Assert.That(sandbox.GetData("nonexistent"), Is.Null);
    }

    [Test]
    public void Sandbox_SuspendAndResume_ControlsActivity()
    {
        var permissions = Gnosis.Runtime.Sandbox.ScriptPermissionSet.CreateDefault();
        var quota = Gnosis.Runtime.Sandbox.ResourceQuota.Unlimited();
        using var sandbox = new Gnosis.Runtime.Sandbox.Sandbox("test-script", permissions, quota);

        Assert.That(sandbox.IsActive, Is.True);

        sandbox.Suspend();
        Assert.That(sandbox.IsActive, Is.False);

        Assert.Throws<Gnosis.Runtime.Sandbox.SandboxViolationException>(() =>
            sandbox.DemandPermission(Gnosis.Runtime.Sandbox.ScriptPermission.NativeCall));

        sandbox.Resume();
        Assert.That(sandbox.IsActive, Is.True);
        sandbox.DemandPermission(Gnosis.Runtime.Sandbox.ScriptPermission.NativeCall);
    }

    [Test]
    public void Sandbox_ViolationLog_RecordsViolations()
    {
        var permissions = Gnosis.Runtime.Sandbox.ScriptPermissionSet.CreateStrict();
        var quota = Gnosis.Runtime.Sandbox.ResourceQuota.Unlimited();
        using var sandbox = new Gnosis.Runtime.Sandbox.Sandbox("test-script", permissions, quota);

        try { sandbox.DemandPermission(Gnosis.Runtime.Sandbox.ScriptPermission.NetworkAccess); } catch { }

        Assert.That(sandbox.ViolationCount, Is.EqualTo(1));
        Assert.That(sandbox.ViolationLog[0].ViolationType, Is.EqualTo(Gnosis.Runtime.Sandbox.ViolationType.PermissionDenied));
    }

    [Test]
    public void SandboxManager_CreateAndDestroy_WorksCorrectly()
    {
        using var manager = new Gnosis.Runtime.Sandbox.SandboxManager();

        var sandbox = manager.CreateSandbox("script-1");
        Assert.That(sandbox.IsActive, Is.True);
        Assert.That(manager.TotalSandboxCount, Is.EqualTo(1));

        Assert.That(manager.DestroySandbox("script-1"), Is.True);
        Assert.That(manager.TotalSandboxCount, Is.EqualTo(0));
    }

    [Test]
    public void SandboxManager_GetOrCreate_ReturnsExisting()
    {
        using var manager = new Gnosis.Runtime.Sandbox.SandboxManager();

        var sandbox1 = manager.CreateSandbox("script-1");
        var sandbox2 = manager.GetOrCreateSandbox("script-1");

        Assert.That(sandbox2, Is.SameAs(sandbox1));
    }

    [Test]
    public void SandboxManager_SuspendAll_StopsAllSandboxes()
    {
        using var manager = new Gnosis.Runtime.Sandbox.SandboxManager();

        manager.CreateSandbox("s1");
        manager.CreateSandbox("s2");

        manager.SuspendAll();

        Assert.That(manager.ActiveSandboxCount, Is.EqualTo(0));
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

        obj.SetField("name", "hello");
        obj.SetField("count", 42L);

        Assert.That(obj.GetField("name"), Is.EqualTo("hello"));
        Assert.That(obj.GetField("count"), Is.EqualTo(42L));
        Assert.That(obj.GetField("nonexistent"), Is.Null);
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

        arr.Add(10L);
        arr.Add(20L);
        arr.Add(30L);

        Assert.That(arr.Count, Is.EqualTo(3));
        Assert.That(arr[0], Is.EqualTo(10L));
        Assert.That(arr[1], Is.EqualTo(20L));
        Assert.That(arr[2], Is.EqualTo(30L));
    }

    [Test]
    public void GGArray_Set_WorksCorrectly()
    {
        var arr = new GGArray(4);
        arr.Add(10L);
        arr.Add(20L);

        arr[1] = 99L;

        Assert.That(arr[1], Is.EqualTo(99L));
    }

    [Test]
    public void GGStruct_FieldAccess_WorksCorrectly()
    {
        var str = new GGStruct("Vec3", new[] { "x", "y", "z" });

        str.SetField(0, 1.0);
        str.SetField(1, 2.0);
        str.SetField(2, 3.0);

        Assert.That(str.GetField(0), Is.EqualTo(1.0));
        Assert.That(str.GetField(1), Is.EqualTo(2.0));
        Assert.That(str.GetFieldIndex("y"), Is.EqualTo(1));
    }

    [Test]
    public void GGClosure_UpvalueAccess_WorksCorrectly()
    {
        var closure = new GGClosure(100, 2);

        closure.SetUpvalue(0, 42L);
        closure.SetUpvalue(1, "hello");

        Assert.That(closure.GetUpvalue(0), Is.EqualTo(42L));
        Assert.That(closure.GetUpvalue(1), Is.EqualTo("hello"));
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
        objA.SetField("ref", objB);
        objB.SetField("ref", objA);

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
        Assert.That(Marshaller.ToGGValue(42), Is.EqualTo(42L));
        Assert.That(Marshaller.ToGGValue(3.14f), Is.TypeOf<double>());
        Assert.That(Marshaller.ToGGValue(true), Is.EqualTo(1L));
        Assert.That(Marshaller.ToGGValue(null), Is.Null);
    }

    [Test]
    public void Marshaller_FromGGValue_ConvertsBack()
    {
        Assert.That(Marshaller.FromGGValue(42L), Is.EqualTo(42L));
        Assert.That(Marshaller.FromGGValue(3.14), Is.EqualTo(3.14));
        Assert.That(Marshaller.ToInt64(42L), Is.EqualTo(42));
        Assert.That(Marshaller.ToFloat64(3.14), Is.EqualTo(3.14));
        Assert.That(Marshaller.ToBool(1L), Is.True);
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

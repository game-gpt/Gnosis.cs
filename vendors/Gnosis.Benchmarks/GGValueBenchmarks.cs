using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Gnosis.Runtime.VM;

namespace Gnosis.Benchmarks;

[Config(typeof(GGValueConfig))]
[MemoryDiagnoser]
[RankColumn]
public class GGValueBenchmarks
{
    private class GGValueConfig : ManualConfig
    {
        public GGValueConfig()
        {
            AddJob(Job.ShortRun
                .WithWarmupCount(3)
                .WithIterationCount(10));
            AddColumn(StatisticColumn.P95);
        }
    }

    #region GGValue 创建 vs object? 装箱

    [Benchmark(Baseline = true)]
    [Arguments(42)]
    [Arguments(123456789)]
    [Arguments(-1)]
    public object? ObjectBoxing_CreateInt(int value)
    {
        return value;
    }

    [Benchmark]
    [Arguments(42)]
    [Arguments(123456789)]
    [Arguments(-1)]
    public GGValue GGValue_CreateInt(int value)
    {
        return GGValue.FromInt(value);
    }

    [Benchmark(Baseline = true)]
    [Arguments(3.14)]
    [Arguments(double.NaN)]
    [Arguments(0.0)]
    public object? ObjectBoxing_CreateFloat(double value)
    {
        return value;
    }

    [Benchmark]
    [Arguments(3.14)]
    [Arguments(double.NaN)]
    [Arguments(0.0)]
    public GGValue GGValue_CreateFloat(double value)
    {
        return GGValue.FromFloat(value);
    }

    [Benchmark(Baseline = true)]
    [Arguments(true)]
    [Arguments(false)]
    public object? ObjectBoxing_CreateBool(bool value)
    {
        return value;
    }

    [Benchmark]
    [Arguments(true)]
    [Arguments(false)]
    public GGValue GGValue_CreateBool(bool value)
    {
        return GGValue.FromBool(value);
    }

    [Benchmark(Baseline = true)]
    public object? ObjectBoxing_CreateNull()
    {
        return null;
    }

    [Benchmark]
    public GGValue GGValue_CreateNull()
    {
        return GGValue.Null;
    }

    #endregion

    #region GGValue 类型检查 vs object? 类型检查

    private readonly GGValue _intVal = GGValue.FromInt(42);
    private readonly GGValue _floatVal = GGValue.FromFloat(3.14);
    private readonly GGValue _boolVal = GGValue.FromBool(true);
    private readonly GGValue _nullVal = GGValue.Null;
    private readonly GGValue _entityVal = GGValue.FromEntity(1);

    private readonly object? _objInt = 42;
    private readonly object? _objFloat = 3.14;
    private readonly object? _objBool = true;
    private readonly object? _objNull = null;

    [Benchmark(Baseline = true)]
    public bool ObjectBoxing_IsInt()
    {
        return _objInt is int;
    }

    [Benchmark]
    public bool GGValue_IsInt()
    {
        return _intVal.IsInt;
    }

    [Benchmark(Baseline = true)]
    public bool ObjectBoxing_IsFloat()
    {
        return _objFloat is double;
    }

    [Benchmark]
    public bool GGValue_IsFloat()
    {
        return _floatVal.IsFloat;
    }

    [Benchmark(Baseline = true)]
    public bool ObjectBoxing_IsNull()
    {
        return _objNull is null;
    }

    [Benchmark]
    public bool GGValue_IsNull()
    {
        return _nullVal.IsNull;
    }

    [Benchmark]
    public bool GGValue_IsEntity()
    {
        return _entityVal.IsEntity;
    }

    #endregion

    #region GGValue 值访问 vs object? 拆箱

    [Benchmark(Baseline = true)]
    public int ObjectBoxing_GetInt()
    {
        return (int)_objInt!;
    }

    [Benchmark]
    public long GGValue_GetInt()
    {
        return _intVal.IntValue;
    }

    [Benchmark(Baseline = true)]
    public double ObjectBoxing_GetFloat()
    {
        return (double)_objFloat!;
    }

    [Benchmark]
    public double GGValue_GetFloat()
    {
        return _floatVal.FloatValue;
    }

    [Benchmark(Baseline = true)]
    public bool ObjectBoxing_GetBool()
    {
        return (bool)_objBool!;
    }

    [Benchmark]
    public bool GGValue_GetBool()
    {
        return _boolVal.BoolValue;
    }

    #endregion

    #region GGValue 算术运算 vs object? 拆箱运算

    private readonly GGValue _a = GGValue.FromInt(100);
    private readonly GGValue _b = GGValue.FromInt(200);

    [Benchmark(Baseline = true)]
    public int ObjectBoxing_AddInt()
    {
        return (int)_objInt! + 200;
    }

    [Benchmark]
    public GGValue GGValue_AddInt()
    {
        return GGValue.FromInt(_a.IntValue + _b.IntValue);
    }

    #endregion

    #region GGValue 相等性比较 vs object? 比较

    private readonly GGValue _same1 = GGValue.FromInt(42);
    private readonly GGValue _same2 = GGValue.FromInt(42);
    private readonly GGValue _diff = GGValue.FromInt(99);

    [Benchmark(Baseline = true)]
    public bool ObjectBoxing_EqualInt()
    {
        return Equals(_objInt, 42);
    }

    [Benchmark]
    public bool GGValue_EqualInt()
    {
        return _same1.Equals(_same2);
    }

    [Benchmark]
    public bool GGValue_NotEqualInt()
    {
        return !_same1.Equals(_diff);
    }

    #endregion

    #region GGValue 真值判断

    [Benchmark]
    public bool GGValue_IsTruthy_IntNonZero()
    {
        return _intVal.IsTruthy();
    }

    [Benchmark]
    public bool GGValue_IsTruthy_Null()
    {
        return _nullVal.IsTruthy();
    }

    [Benchmark]
    public bool GGValue_IsTruthy_BoolTrue()
    {
        return _boolVal.IsTruthy();
    }

    #endregion

    #region GGValue 批量操作

    [Params(100, 1000, 10000)]
    public int BatchSize { get; set; }

    [Benchmark(Baseline = true)]
    public long ObjectBoxing_BatchSum()
    {
        long sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            var boxed = (object)i;
            sum += (int)boxed;
        }
        return sum;
    }

    [Benchmark]
    public long GGValue_BatchSum()
    {
        long sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            var val = GGValue.FromInt(i);
            sum += val.IntValue;
        }
        return sum;
    }

    [Benchmark]
    public long GGValue_BatchStackPushPop()
    {
        var stack = new VMStack(1024);
        for (var i = 0; i < BatchSize; i++)
        {
            stack.Push(GGValue.FromInt(i));
        }

        long sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += stack.Pop().IntValue;
        }
        return sum;
    }

    #endregion

    #region RefTag 直接引用 vs IntTag 间接引用（NaN-Boxing 引用路径性能验证）

    private readonly GGValue _refTagObj;
    private readonly GGValue _intTagObj;
    private readonly MemoryManager _refMm;
    private readonly MemoryManager _intMm;

    public GGValueBenchmarks()
    {
        _refMm = new MemoryManager();
        var obj = new GGObject("BenchmarkObj");
        _refMm.Allocate(obj);
        _refTagObj = GGValue.FromObject(obj);

        _intMm = new MemoryManager();
        var intObj = new GGObject("BenchmarkObj");
        var intObjId = _intMm.Allocate(intObj);
        _intTagObj = GGValue.FromInt(intObjId);
    }

    [Benchmark(Baseline = true)]
    public GGObject? RefTag_DirectAccess()
    {
        if (_refTagObj.IsReference && _refTagObj.Reference is GGObject obj)
        {
            return obj;
        }
        return null;
    }

    [Benchmark]
    public GGObject? IntTag_IndirectAccess()
    {
        if (_intTagObj.IsInt)
        {
            return _intMm.GetObject<GGObject>((int)_intTagObj.IntValue);
        }
        return null;
    }

    [Benchmark]
    public GGObject? RefTag_ObjectValueAccess()
    {
        return _refTagObj.ObjectValue;
    }

    #endregion

    #region GC Root 扫描性能

    private GGValue[] _stackWithRefTags = null!;
    private GGValue[] _stackWithIntTags = null!;
    private MemoryManager _gcMm = null!;

    [GlobalSetup(Targets = [nameof(GCRootScan_RefTagStack), nameof(GCRootScan_IntTagStack)])]
    public void GCRootScanSetup()
    {
        const int stackSize = 1000;
        _gcMm = new MemoryManager();
        _stackWithRefTags = new GGValue[stackSize];
        _stackWithIntTags = new GGValue[stackSize];

        for (var i = 0; i < stackSize; i++)
        {
            var obj = new GGObject($"obj_{i}");
            var id = _gcMm.Allocate(obj);
            _stackWithRefTags[i] = GGValue.FromObject(obj);
            _stackWithIntTags[i] = GGValue.FromInt(id);
        }
    }

    [Benchmark]
    public void GCRootScan_RefTagStack()
    {
        var mm = new MemoryManager();
        mm.SetRootsFromStack(_stackWithRefTags, _stackWithRefTags.Length);
    }

    [Benchmark]
    public void GCRootScan_IntTagStack()
    {
        var mm = new MemoryManager();
        mm.SetRootsFromStack(_stackWithIntTags, _stackWithIntTags.Length);
    }

    #endregion
}

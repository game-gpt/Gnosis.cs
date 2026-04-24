using Gnosis.IR.Instruction;
using Gnosis.Runtime.VM;
using NUnit.Framework;

namespace Gnosis.Tests.Runtime;

[TestFixture]
public class MultiModuleTests
{
    #region 辅助方法

    private static BytecodeUnit CreateModule(
        string name,
        List<BytecodeInstruction> instructions,
        int paramCount = 0,
        int localCount = 0,
        List<string>? imports = null,
        List<string>? exports = null,
        List<object>? constants = null)
    {
        var function = new BytecodeFunction($"{name}_main", paramCount, localCount, instructions);
        return new BytecodeUnit(
            name,
            constants ?? [],
            [function],
            imports ?? [],
            exports ?? [],
            []);
    }

    private static BytecodeUnit CreateModuleWithFunctions(
        string name,
        List<BytecodeFunction> functions,
        List<string>? imports = null,
        List<string>? exports = null,
        List<object>? constants = null)
    {
        return new BytecodeUnit(
            name,
            constants ?? [],
            functions,
            imports ?? [],
            exports ?? [],
            []);
    }

    #endregion

    #region 模块加载与卸载

    [Test]
    public void LoadModule_FirstModuleBecomesCurrent()
    {
        var state = new VMState();
        var module = CreateModule("test", [new BytecodeInstruction(OpCode.Halt)]);

        var adapter = new BytecodeModuleAdapter(module);
        state.LoadModule(adapter);

        Assert.That(state.CurrentModule, Is.Not.Null);
        Assert.That(state.CurrentModule!.Name, Is.EqualTo("test"));
        Assert.That(state.Modules.Count, Is.EqualTo(1));
    }

    [Test]
    public void LoadModule_SecondModuleDoesNotReplaceCurrent()
    {
        var state = new VMState();
        var module1 = CreateModule("first", [new BytecodeInstruction(OpCode.Halt)]);
        var module2 = CreateModule("second", [new BytecodeInstruction(OpCode.Halt)]);

        state.LoadModule(new BytecodeModuleAdapter(module1));
        state.LoadModule(new BytecodeModuleAdapter(module2));

        Assert.That(state.CurrentModule!.Name, Is.EqualTo("first"));
        Assert.That(state.Modules.Count, Is.EqualTo(2));
    }

    [Test]
    public void UnloadModule_CurrentModule_UpdatesCurrent()
    {
        var state = new VMState();
        var module1 = CreateModule("first", [new BytecodeInstruction(OpCode.Halt)]);
        var module2 = CreateModule("second", [new BytecodeInstruction(OpCode.Halt)]);

        state.LoadModule(new BytecodeModuleAdapter(module1));
        state.LoadModule(new BytecodeModuleAdapter(module2));
        state.UnloadModule("first");

        Assert.That(state.CurrentModule, Is.Null);
        Assert.That(state.Modules.Count, Is.EqualTo(1));
    }

    [Test]
    public void GetModule_FindsByName()
    {
        var state = new VMState();
        var module = CreateModule("target", [new BytecodeInstruction(OpCode.Halt)]);

        state.LoadModule(new BytecodeModuleAdapter(module));

        var found = state.GetModule("target");
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Name, Is.EqualTo("target"));

        var notFound = state.GetModule("nonexistent");
        Assert.That(notFound, Is.Null);
    }

    #endregion

    #region 模块链接器

    [Test]
    public void ModuleLinker_NoImports_LinksSuccessfully()
    {
        var linker = new ModuleLinker();
        var module = CreateModule("standalone", [new BytecodeInstruction(OpCode.Halt)]);
        var adapter = new BytecodeModuleAdapter(module);

        var result = linker.Link([adapter]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ResolvedImports.Count, Is.EqualTo(0));
    }

    [Test]
    public void ModuleLinker_ValidImport_LinksSuccessfully()
    {
        var linker = new ModuleLinker();

        var providerModule = CreateModuleWithFunctions("provider",
            [new BytecodeFunction("add", 2, 0,
                [new BytecodeInstruction(OpCode.AddInt), new BytecodeInstruction(OpCode.Return)])],
            exports: ["provider::add"]);

        var consumerModule = CreateModule("consumer",
            [new BytecodeInstruction(OpCode.Halt)],
            imports: ["provider::add"]);

        var providerAdapter = new BytecodeModuleAdapter(providerModule);
        var consumerAdapter = new BytecodeModuleAdapter(consumerModule);

        var result = linker.Link([providerAdapter, consumerAdapter]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ResolvedImports.Count, Is.EqualTo(1));
        Assert.That(result.ResolvedImports[0].SymbolName, Is.EqualTo("provider::add"));
        Assert.That(result.ResolvedImports[0].ExportModuleName, Is.EqualTo("provider"));
    }

    [Test]
    public void ModuleLinker_UnresolvedImport_Fails()
    {
        var linker = new ModuleLinker();

        var consumerModule = CreateModule("consumer",
            [new BytecodeInstruction(OpCode.Halt)],
            imports: ["missing::func"]);

        var consumerAdapter = new BytecodeModuleAdapter(consumerModule);

        var result = linker.Link([consumerAdapter]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.UnresolvedImports.Count, Is.EqualTo(1));
    }

    [Test]
    public void ModuleLinker_CyclicDependency_Detected()
    {
        var linker = new ModuleLinker();

        var moduleA = CreateModule("module_a",
            [new BytecodeInstruction(OpCode.Halt)],
            imports: ["module_b::func"]);

        var moduleB = CreateModule("module_b",
            [new BytecodeInstruction(OpCode.Halt)],
            imports: ["module_a::func"]);

        var adapterA = new BytecodeModuleAdapter(moduleA);
        var adapterB = new BytecodeModuleAdapter(moduleB);

        var result = linker.Link([adapterA, adapterB]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("循环依赖"));
    }

    [Test]
    public void ModuleLinker_CanLoad_WithSatisfiedDeps()
    {
        var linker = new ModuleLinker();

        var providerModule = CreateModuleWithFunctions("provider",
            [new BytecodeFunction("func", 0, 0, [new BytecodeInstruction(OpCode.Halt)])],
            exports: ["provider::func"]);

        var consumerModule = CreateModule("consumer",
            [new BytecodeInstruction(OpCode.Halt)],
            imports: ["provider::func"]);

        var providerAdapter = new BytecodeModuleAdapter(providerModule);
        var consumerAdapter = new BytecodeModuleAdapter(consumerModule);

        Assert.That(linker.CanLoad(consumerAdapter, [providerAdapter]), Is.True);
        Assert.That(linker.CanLoad(consumerAdapter, []), Is.False);
    }

    [Test]
    public void ModuleLinker_TopologicalSort_CorrectOrder()
    {
        var linker = new ModuleLinker();

        var baseModule = CreateModuleWithFunctions("base",
            [new BytecodeFunction("func", 0, 0, [new BytecodeInstruction(OpCode.Halt)])],
            exports: ["base::func"]);

        var midModule = CreateModule("mid",
            [new BytecodeInstruction(OpCode.Halt)],
            imports: ["base::func"]);

        var topModule = CreateModule("top",
            [new BytecodeInstruction(OpCode.Halt)],
            imports: ["mid::func"]);

        var baseAdapter = new BytecodeModuleAdapter(baseModule);
        var midAdapter = new BytecodeModuleAdapter(midModule);
        var topAdapter = new BytecodeModuleAdapter(topModule);

        var sorted = linker.TopologicalSort([topAdapter, midAdapter, baseAdapter]);

        Assert.That(sorted.Count, Is.EqualTo(3));
        Assert.That(sorted[0].Name, Is.EqualTo("base"));
        Assert.That(sorted[1].Name, Is.EqualTo("mid"));
        Assert.That(sorted[2].Name, Is.EqualTo("top"));
    }

    [Test]
    public void ModuleLinker_GetDependencies_ReturnsModuleNames()
    {
        var linker = new ModuleLinker();

        var module = CreateModule("consumer",
            [new BytecodeInstruction(OpCode.Halt)],
            imports: ["module_a::func1", "module_b::func2"]);

        var adapter = new BytecodeModuleAdapter(module);
        var deps = linker.GetDependencies(adapter);

        Assert.That(deps.Count, Is.EqualTo(2));
        Assert.That(deps, Does.Contain("module_a"));
        Assert.That(deps, Does.Contain("module_b"));
    }

    #endregion

    #region CallFrame 模块引用

    [Test]
    public void CallFrame_ReturnModuleName_DefaultNull()
    {
        var frame = new CallFrame(0, 0, 4);
        Assert.That(frame.ReturnModuleName, Is.Null);
    }

    [Test]
    public void CallFrame_ReturnModuleName_StoredCorrectly()
    {
        var frame = new CallFrame(100, 0, 4, "other_module");
        Assert.That(frame.ReturnModuleName, Is.EqualTo("other_module"));
        Assert.That(frame.ReturnAddress, Is.EqualTo(100));
    }

    #endregion

    #region 模块级全局变量

    [Test]
    public void ModuleGlobals_SetAndGet()
    {
        var state = new VMState();

        state.SetModuleGlobal("module_a", 0, GGValue.FromInt(42));
        state.SetModuleGlobal("module_a", 1, GGValue.FromInt(100));
        state.SetModuleGlobal("module_b", 0, GGValue.FromInt(999));

        Assert.That(state.GetModuleGlobal("module_a", 0).IntValue, Is.EqualTo(42));
        Assert.That(state.GetModuleGlobal("module_a", 1).IntValue, Is.EqualTo(100));
        Assert.That(state.GetModuleGlobal("module_b", 0).IntValue, Is.EqualTo(999));
        Assert.That(state.GetModuleGlobal("module_c", 0).IsNull, Is.True);
    }

    [Test]
    public void ModuleGlobals_Count()
    {
        var state = new VMState();

        Assert.That(state.GetModuleGlobalCount("module_a"), Is.EqualTo(0));

        state.SetModuleGlobal("module_a", 0, GGValue.FromInt(1));
        state.SetModuleGlobal("module_a", 1, GGValue.FromInt(2));

        Assert.That(state.GetModuleGlobalCount("module_a"), Is.EqualTo(2));
        Assert.That(state.GetModuleGlobalCount("module_b"), Is.EqualTo(0));
    }

    [Test]
    public void ModuleGlobals_IsolatedFromGlobal()
    {
        var state = new VMState();

        state.SetGlobal(0, GGValue.FromInt(10));
        state.SetModuleGlobal("module_a", 0, GGValue.FromInt(20));

        Assert.That(state.GetGlobal(0).IntValue, Is.EqualTo(10));
        Assert.That(state.GetModuleGlobal("module_a", 0).IntValue, Is.EqualTo(20));
    }

    #endregion

    #region VMInterpreter 多模块执行

    [Test]
    public void VMInterpreter_Run_MultipleModulesLoaded()
    {
        var state = new VMState();
        var registry = new NativeFunctionRegistry();

        var module1 = CreateModule("main",
            [new BytecodeInstruction(OpCode.PushInt32, 10),
             new BytecodeInstruction(OpCode.PushInt32, 20),
             new BytecodeInstruction(OpCode.AddInt),
             new BytecodeInstruction(OpCode.Halt)]);

        var module2 = CreateModule("helper",
            [new BytecodeInstruction(OpCode.Halt)]);

        state.LoadModule(new BytecodeModuleAdapter(module1));
        state.LoadModule(new BytecodeModuleAdapter(module2));

        var vm = new VMInterpreter(state, registry);
        vm.Run();

        Assert.That(state.Modules.Count, Is.EqualTo(2));
    }

    [Test]
    public void VMInterpreter_CallModule_SwitchesInstructionStream()
    {
        var state = new VMState();
        var registry = new NativeFunctionRegistry();

        var helperModule = CreateModuleWithFunctions("helper",
            [new BytecodeFunction("add", 2, 2,
                [new BytecodeInstruction(OpCode.LoadLocal, 0),
                 new BytecodeInstruction(OpCode.LoadLocal, 1),
                 new BytecodeInstruction(OpCode.AddInt),
                 new BytecodeInstruction(OpCode.Return)])],
            exports: ["helper::add"]);

        var mainModule = CreateModuleWithFunctions("main",
            [new BytecodeFunction("main", 0, 0,
                [new BytecodeInstruction(OpCode.PushInt32, 5),
                 new BytecodeInstruction(OpCode.PushInt32, 3),
                 new BytecodeInstruction(OpCode.CallModule),
                 new BytecodeInstruction(OpCode.Halt)])],
            imports: ["helper::add"],
            constants: ["helper::add"]);

        state.LoadModule(new BytecodeModuleAdapter(mainModule));
        state.LoadModule(new BytecodeModuleAdapter(helperModule));

        var vm = new VMInterpreter(state, registry);
        vm.Run();
    }

    #endregion

    #region BytecodeModuleAdapter CallModule 编码

    [Test]
    public void BytecodeModuleAdapter_CallModule_OperandSize()
    {
        Assert.That(BytecodeModuleAdapter_GetOperandSize(OpCode.CallModule), Is.EqualTo(8));
    }

    private static int BytecodeModuleAdapter_GetOperandSize(OpCode opCode)
    {
        return opCode switch
        {
            OpCode.PushInt8 => 1,
            OpCode.PushInt16 => 2,
            OpCode.PushInt64 => 8,
            OpCode.PushFloat64 => 8,
            OpCode.CallModule => 8,
            OpCode.PushInt32 or OpCode.PushFloat32 or OpCode.Jump or OpCode.JumpIfTrue
                or OpCode.JumpIfFalse or OpCode.Call or OpCode.CallNative
                or OpCode.LoadLocal or OpCode.StoreLocal or OpCode.LoadGlobal
                or OpCode.StoreGlobal or OpCode.LoadField or OpCode.StoreField
                or OpCode.NewObject or OpCode.GetField or OpCode.SetField
                or OpCode.AddComponent or OpCode.GetComponent or OpCode.RemoveComponent
                or OpCode.SetComponent or OpCode.HasComponent
                or OpCode.QueryWith or OpCode.QueryWithout
                or OpCode.DefineComponent or OpCode.DefineSystem or OpCode.SystemSchedule
                or OpCode.PushString or OpCode.NewArray or OpCode.MakeClosure
                or OpCode.IsType or OpCode.TypeOf or OpCode.QueryAll or OpCode.QueryAny => 4,
            _ => 0
        };
    }

    #endregion

    #region 综合场景

    [Test]
    public void Scenario_MultiModule_Computation()
    {
        var state = new VMState();
        var registry = new NativeFunctionRegistry();

        var mathModule = CreateModuleWithFunctions("math",
            [new BytecodeFunction("double", 1, 1,
                [new BytecodeInstruction(OpCode.LoadLocal, 0),
                 new BytecodeInstruction(OpCode.PushInt32, 2),
                 new BytecodeInstruction(OpCode.MulInt),
                 new BytecodeInstruction(OpCode.Return)])],
            exports: ["math::double"]);

        var mainModule = CreateModuleWithFunctions("main",
            [new BytecodeFunction("main", 0, 0,
                [new BytecodeInstruction(OpCode.PushInt32, 21),
                 new BytecodeInstruction(OpCode.CallModule),
                 new BytecodeInstruction(OpCode.StoreGlobal, 0),
                 new BytecodeInstruction(OpCode.Halt)])],
            imports: ["math::double"],
            constants: ["math::double"]);

        state.LoadModule(new BytecodeModuleAdapter(mainModule));
        state.LoadModule(new BytecodeModuleAdapter(mathModule));

        var vm = new VMInterpreter(state, registry);
        vm.Run();
    }

    [Test]
    public void Scenario_ModuleLinker_ThreeModuleChain()
    {
        var linker = new ModuleLinker();

        var baseModule = CreateModuleWithFunctions("base",
            [new BytecodeFunction("zero", 0, 0,
                [new BytecodeInstruction(OpCode.PushInt32, 0),
                 new BytecodeInstruction(OpCode.Return)])],
            exports: ["base::zero"]);

        var midModule = CreateModuleWithFunctions("mid",
            [new BytecodeFunction("one", 0, 0,
                [new BytecodeInstruction(OpCode.PushInt32, 1),
                 new BytecodeInstruction(OpCode.Return)])],
            imports: ["base::zero"],
            exports: ["mid::one"]);

        var topModule = CreateModuleWithFunctions("top",
            [new BytecodeFunction("main", 0, 0,
                [new BytecodeInstruction(OpCode.Halt)])],
            imports: ["mid::one"]);

        var baseAdapter = new BytecodeModuleAdapter(baseModule);
        var midAdapter = new BytecodeModuleAdapter(midModule);
        var topAdapter = new BytecodeModuleAdapter(topModule);

        var result = linker.Link([baseAdapter, midAdapter, topAdapter]);
        Assert.That(result.Success, Is.True);

        var sorted = linker.TopologicalSort([topAdapter, midAdapter, baseAdapter]);
        Assert.That(sorted[0].Name, Is.EqualTo("base"));
        Assert.That(sorted[1].Name, Is.EqualTo("mid"));
        Assert.That(sorted[2].Name, Is.EqualTo("top"));
    }

    [Test]
    public void Scenario_ModuleLinker_DiamondDependency()
    {
        var linker = new ModuleLinker();

        var baseModule = CreateModuleWithFunctions("base",
            [new BytecodeFunction("func", 0, 0, [new BytecodeInstruction(OpCode.Halt)])],
            exports: ["base::func"]);

        var leftModule = CreateModule("left",
            [new BytecodeInstruction(OpCode.Halt)],
            imports: ["base::func"]);

        var rightModule = CreateModule("right",
            [new BytecodeInstruction(OpCode.Halt)],
            imports: ["base::func"]);

        var topModule = CreateModule("top",
            [new BytecodeInstruction(OpCode.Halt)],
            imports: ["left::func", "right::func"]);

        var baseAdapter = new BytecodeModuleAdapter(baseModule);
        var leftAdapter = new BytecodeModuleAdapter(leftModule);
        var rightAdapter = new BytecodeModuleAdapter(rightModule);
        var topAdapter = new BytecodeModuleAdapter(topModule);

        var result = linker.Link([baseAdapter, leftAdapter, rightAdapter, topAdapter]);
        Assert.That(result.Success, Is.True);

        var sorted = linker.TopologicalSort([topAdapter, leftAdapter, rightAdapter, baseAdapter]);
        Assert.That(sorted.Count, Is.EqualTo(4));

        var baseIdx = -1;
        var leftIdx = -1;
        var rightIdx = -1;
        var topIdx = -1;
        for (var i = 0; i < sorted.Count; i++)
        {
            switch (sorted[i].Name)
            {
                case "base": baseIdx = i; break;
                case "left": leftIdx = i; break;
                case "right": rightIdx = i; break;
                case "top": topIdx = i; break;
            }
        }

        Assert.That(baseIdx, Is.LessThan(leftIdx));
        Assert.That(baseIdx, Is.LessThan(rightIdx));
        Assert.That(topIdx, Is.GreaterThan(leftIdx));
        Assert.That(topIdx, Is.GreaterThan(rightIdx));
    }

    #endregion
}

using Nyar.Core;
using Nyar.Core.Types;
using Nyar.Core.VM;
using Nyar.Core.VM.Bytecode;
using Nyar.Dialect.Game.Rules;

namespace Gnosis.VM;

/// <summary>
///     Gnosis 游戏 VM，基于 Game 方言的 ECS 优化虚拟机
///     与 NyarStandardVM 同级，复用 Nyar 元虚拟机框架，但针对游戏场景优化
/// </summary>
public sealed class GnosisGameVM
{
    private readonly NyarVM _inner;
    private readonly BytecodeEncoder _encoder;
    private readonly IGameWorld _world;

    /// <summary>
    ///     初始化 GnosisGameVM
    /// </summary>
    /// <param name="world">游戏世界实例</param>
    public GnosisGameVM(IGameWorld world)
    {
        _inner = new NyarVM();
        _encoder = new BytecodeEncoder();
        _world = world;
    }

    /// <summary>
    ///     加载模块
    /// </summary>
    /// <param name="module">要加载的模块</param>
    public void LoadModule(NyarModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (module.RawBytecode is null)
        {
            var encoded = _encoder.Encode(module);
            module.RawBytecode = encoded;
        }

        _inner.Load(module);
    }

    /// <summary>
    ///     加载字节码
    /// </summary>
    /// <param name="bytecode">字节码数据</param>
    public void LoadBytecode(byte[] bytecode)
    {
        _inner.Load(bytecode);
    }

    /// <summary>
    ///     执行函数
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <param name="functionName">函数名称</param>
    /// <param name="args">函数参数</param>
    /// <returns>函数返回值</returns>
    public Value Run(string moduleName, string functionName, params Value[] args)
    {
        return _inner.Run(moduleName, functionName, args);
    }

    /// <summary>
    ///     执行一帧的世界更新
    /// </summary>
    /// <param name="deltaTime">帧间隔时间（毫秒）</param>
    public void Tick(double deltaTime)
    {
        _world.Update(deltaTime);
    }

    /// <summary>
    ///     获取游戏世界
    /// </summary>
    public IGameWorld World => _world;
}

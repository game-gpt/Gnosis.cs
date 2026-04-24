using System.Numerics;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.FX;

/// <summary>
/// GPU 粒子发射器，基于 Compute Shader 在 GPU 端执行粒子模拟
/// </summary>
public sealed class GpuParticleEmitter : IParticleEmitter, IDisposable
{
    #region 字段

    private readonly IDevice _device;
    private IResource? _particleBuffer;
    private IResource? _aliveBuffer;
    private IResource? _deadBuffer;
    private IResource? _counterBuffer;
    private IResource? _parameterBuffer;
    private IResource? _sampler;
    private IRhiDescriptorSet? _descriptorSet;
    private Compute.ComputePipeline? _emitPipeline;
    private Compute.ComputePipeline? _simulatePipeline;
    private bool _isDisposed;

    private float _emitAccumulator;
    private float _burstTimer;
    private int _aliveCount;

    #endregion

    #region 属性

    public float Rate { get; set; }
    public int BurstCount { get; set; }
    public float BurstInterval { get; set; }
    public ParticleEmitterShape Shape { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; }
    public float Angle { get; set; }
    public float Radius { get; set; }
    public Vector3 BoxSize { get; set; }
    public float MinLifetime { get; set; }
    public float MaxLifetime { get; set; }
    public float MinSpeed { get; set; }
    public float MaxSpeed { get; set; }
    public int MaxParticles { get; set; }
    public bool UseGpuSimulation { get; set; }

    /// <summary>
    /// 当前活跃粒子数量
    /// </summary>
    public int AliveCount => _aliveCount;

    /// <summary>
    /// 粒子数据缓冲区
    /// </summary>
    public IResource? ParticleBuffer => _particleBuffer;

    /// <summary>
    /// 活跃粒子索引缓冲区
    /// </summary>
    public IResource? AliveBuffer => _aliveBuffer;

    /// <summary>
    /// 计数器缓冲区
    /// </summary>
    public IResource? CounterBuffer => _counterBuffer;

    #endregion

    #region 构造函数

    public GpuParticleEmitter(IDevice device, int maxParticles = 10000)
    {
        _device = device;
        MaxParticles = maxParticles;

        Rate = 100.0f;
        BurstCount = 0;
        BurstInterval = 0.0f;
        Shape = ParticleEmitterShape.Sphere;
        Position = Vector3.Zero;
        Direction = Vector3.UnitY;
        Angle = 30.0f;
        Radius = 1.0f;
        BoxSize = Vector3.One;
        MinLifetime = 1.0f;
        MaxLifetime = 3.0f;
        MinSpeed = 1.0f;
        MaxSpeed = 5.0f;
        UseGpuSimulation = true;

        _emitAccumulator = 0.0f;
        _burstTimer = 0.0f;
        _aliveCount = 0;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 初始化 GPU 资源
    /// </summary>
    /// <param name="emitShader">发射着色器程序</param>
    /// <param name="simulateShader">模拟着色器程序</param>
    public void Initialize(IShaderProgram? emitShader, IShaderProgram? simulateShader)
    {
        CreateBuffers();
        CreateSampler();
        CreateDescriptorSet();

        if (emitShader is not null)
        {
            var dispatcher = new Compute.ComputeDispatcher(_device);
            _emitPipeline = dispatcher.CreatePipeline("ParticleEmit", emitShader);
        }

        if (simulateShader is not null)
        {
            var dispatcher = new Compute.ComputeDispatcher(_device);
            _simulatePipeline = dispatcher.CreatePipeline("ParticleSimulate", simulateShader);
        }
    }

    public void Emit(int count)
    {
        _emitAccumulator += count;
    }

    public void Reset()
    {
        _emitAccumulator = 0.0f;
        _burstTimer = 0.0f;
        _aliveCount = 0;
    }

    /// <summary>
    /// 更新粒子模拟
    /// </summary>
    /// <param name="commandTable">命令表</param>
    /// <param name="delta">帧间隔时间</param>
    public void Update(ICommandTable commandTable, float delta)
    {
        UpdateEmitAccumulator(delta);
        UpdateParameters(delta);

        if (_simulatePipeline is not null)
        {
            commandTable.SetPipelineState(_simulatePipeline.PipelineState);

            if (_descriptorSet is not null)
            {
                commandTable.BindDescriptorSet(_descriptorSet, 0);
            }

            uint groupCount = Compute.ComputeDispatcher.CalculateGroupCount((uint)MaxParticles, 64);
            commandTable.Dispatch(groupCount, 1, 1);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _particleBuffer?.Dispose();
        _aliveBuffer?.Dispose();
        _deadBuffer?.Dispose();
        _counterBuffer?.Dispose();
        _parameterBuffer?.Dispose();
        _sampler?.Dispose();
        _descriptorSet?.Dispose();
        _emitPipeline?.PipelineState.Dispose();
        _simulatePipeline?.PipelineState.Dispose();

        _particleBuffer = null;
        _aliveBuffer = null;
        _deadBuffer = null;
        _counterBuffer = null;
        _parameterBuffer = null;
        _sampler = null;
        _descriptorSet = null;

        _isDisposed = true;
    }

    #endregion

    #region 私有方法

    private void CreateBuffers()
    {
        const int ParticleStride = 64;
        ulong particleBufferSize = (ulong)ParticleStride * (ulong)MaxParticles;

        _particleBuffer = _device.CreateBuffer(new BufferDesc
        {
            Size = particleBufferSize,
            Usage = BufferUsage.StorageBuffer | BufferUsage.TransferDst,
            DeviceLocal = true
        });

        _aliveBuffer = _device.CreateBuffer(new BufferDesc
        {
            Size = (ulong)(MaxParticles * sizeof(uint)),
            Usage = BufferUsage.StorageBuffer | BufferUsage.TransferDst,
            DeviceLocal = true
        });

        _deadBuffer = _device.CreateBuffer(new BufferDesc
        {
            Size = (ulong)(MaxParticles * sizeof(uint)),
            Usage = BufferUsage.StorageBuffer | BufferUsage.TransferDst,
            DeviceLocal = true
        });

        _counterBuffer = _device.CreateBuffer(new BufferDesc
        {
            Size = 16,
            Usage = BufferUsage.StorageBuffer | BufferUsage.TransferDst,
            HostVisible = true
        });

        _parameterBuffer = _device.CreateBuffer(new BufferDesc
        {
            Size = 256,
            Usage = BufferUsage.UniformBuffer | BufferUsage.TransferDst,
            DeviceLocal = true
        });
    }

    private void CreateSampler()
    {
        _sampler = _device.CreateSampler(new SamplerDesc
        {
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge
        });
    }

    private void CreateDescriptorSet()
    {
        _descriptorSet = _device.CreateDescriptorSet(
        [
            new DescriptorSetBinding
            {
                Binding = 0,
                DescriptorType = DescriptorType.StorageBuffer,
                StageFlags = ShaderStageFlag.Compute
            },
            new DescriptorSetBinding
            {
                Binding = 1,
                DescriptorType = DescriptorType.StorageBuffer,
                StageFlags = ShaderStageFlag.Compute
            },
            new DescriptorSetBinding
            {
                Binding = 2,
                DescriptorType = DescriptorType.StorageBuffer,
                StageFlags = ShaderStageFlag.Compute
            },
            new DescriptorSetBinding
            {
                Binding = 3,
                DescriptorType = DescriptorType.StorageBuffer,
                StageFlags = ShaderStageFlag.Compute
            },
            new DescriptorSetBinding
            {
                Binding = 4,
                DescriptorType = DescriptorType.UniformBuffer,
                StageFlags = ShaderStageFlag.Compute
            }
        ]);
    }

    private void UpdateEmitAccumulator(float delta)
    {
        _emitAccumulator += Rate * delta;

        if (BurstCount > 0 && BurstInterval > 0)
        {
            _burstTimer += delta;

            if (_burstTimer >= BurstInterval)
            {
                _emitAccumulator += BurstCount;
                _burstTimer -= BurstInterval;
            }
        }
    }

    private unsafe void UpdateParameters(float delta)
    {
        if (_descriptorSet is null || _parameterBuffer is null)
        {
            return;
        }

        var parameters = new ParticleSimulateParameters
        {
            DeltaTime = delta,
            EmitCount = (int)_emitAccumulator,
            MaxParticles = MaxParticles,
            MinLifetime = MinLifetime,
            MaxLifetime = MaxLifetime,
            MinSpeed = MinSpeed,
            MaxSpeed = MaxSpeed,
            ShapeType = (int)Shape,
            Position = Position,
            Direction = Vector3.Normalize(Direction),
            Angle = MathF.Cos(Angle * MathF.PI / 180.0f),
            Radius = Radius,
            BoxSize = BoxSize
        };

        _descriptorSet.BindBuffer(4, _parameterBuffer);

        if (_particleBuffer is not null)
        {
            _descriptorSet.BindBuffer(0, _particleBuffer);
        }

        if (_aliveBuffer is not null)
        {
            _descriptorSet.BindBuffer(1, _aliveBuffer);
        }

        if (_deadBuffer is not null)
        {
            _descriptorSet.BindBuffer(2, _deadBuffer);
        }

        if (_counterBuffer is not null)
        {
            _descriptorSet.BindBuffer(3, _counterBuffer);
        }

        _emitAccumulator -= (int)_emitAccumulator;
    }

    #endregion

    #region 嵌套类型

    private struct GpuParticleData
    {
        public Vector3 Position;
        public float Lifetime;
        public Vector3 Velocity;
        public float Age;
        public Vector4 Color;
        public Vector3 Size;
        public int Alive;
    }

    private struct ParticleSimulateParameters
    {
        public float DeltaTime;
        public int EmitCount;
        public int MaxParticles;
        public float MinLifetime;
        public float MaxLifetime;
        public float MinSpeed;
        public float MaxSpeed;
        public int ShapeType;
        public Vector3 Position;
        public Vector3 Direction;
        public float Angle;
        public float Radius;
        public Vector3 BoxSize;
    }

    #endregion
}

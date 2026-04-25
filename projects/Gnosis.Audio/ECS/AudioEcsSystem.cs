using System.Numerics;
using Gnosis.Audio.Driver;
using Gnosis.Audio.Listener;
using Gnosis.Audio.Source;
using Gnosis.Audio.Spatial;
using Gnosis.Core;
using Gnosis.ECS.Component;
using Gnosis.ECS.System;
using Gnosis.ECS.World;

namespace Gnosis.Audio.ECS;

public struct AudioTransformComponent : IComponent
{
    public Vector3 Position;
    public Vector3 Forward;
    public Vector3 Up;
    public Vector3 Velocity;
}

public sealed class AudioEcsSystem : IWorldSystem
{
    #region 字段

    private World? _world;
    private readonly IAudioSystem _audioSystem;
    private readonly SpatialAudioCalculator _spatialCalculator;
    private readonly Dictionary<EntityId, IAudioSource> _entityToSource = new();

    #endregion

    #region 属性

    public SystemPhase Phase => SystemPhase.PostUpdate;

    public IAudioSystem AudioSystem => _audioSystem;

    #endregion

    #region 构造函数

    public AudioEcsSystem()
    {
        _audioSystem = new AudioSystem();
        _spatialCalculator = new SpatialAudioCalculator();
    }

    public AudioEcsSystem(IAudioSystem audioSystem)
    {
        _audioSystem = audioSystem;
        _spatialCalculator = new SpatialAudioCalculator();
    }

    #endregion

    #region ISystem 实现

    public void Initialize()
    {
    }

    public void Shutdown()
    {
        foreach (var source in _entityToSource.Values)
        {
            _audioSystem.DestroySource(source);
        }

        _entityToSource.Clear();
    }

    public void Update(float delta)
    {
        if (_world == null)
        {
            return;
        }

        SyncAudioSources();
        ApplySpatialAudio();
        _audioSystem.Update(delta);
    }

    public void SetWorld(World world)
    {
        _world = world;
    }

    #endregion

    #region 私有方法 - AudioSource 同步

    private void SyncAudioSources()
    {
        if (_world == null)
        {
            return;
        }

        var query = _world.CreateQuery()
            .All<AudioSourceComponent>()
            .Build();

        var currentEntities = new HashSet<EntityId>(query);

        var removedEntities = new List<EntityId>();
        foreach (var entityId in _entityToSource.Keys)
        {
            if (!currentEntities.Contains(entityId))
            {
                removedEntities.Add(entityId);
            }
        }

        foreach (var entityId in removedEntities)
        {
            if (_entityToSource.Remove(entityId, out var source))
            {
                _audioSystem.DestroySource(source);
            }
        }

        foreach (var entityId in currentEntities)
        {
            var audioComp = _world.GetComponent<AudioSourceComponent>(entityId);

            if (_entityToSource.TryGetValue(entityId, out var existingSource))
            {
                SyncSourceProperties(existingSource, audioComp);
            }
            else
            {
                var source = _audioSystem.CreateSource();
                source.Clip = audioComp.ClipPath != null ? _audioSystem.LoadClip(audioComp.ClipPath) : null;
                source.Volume = audioComp.Volume;
                source.IsLooping = audioComp.Loop;

                if (audioComp.PlayOnAwake && source.Clip != null)
                {
                    source.Play();
                }

                _entityToSource[entityId] = source;

                audioComp.Source = source;
                _world.SetComponent(entityId, audioComp);
            }
        }
    }

    private void SyncSourceProperties(IAudioSource source, AudioSourceComponent component)
    {
        if (Math.Abs(source.Volume - component.Volume) > 0.0001f)
        {
            source.Volume = component.Volume;
        }

        if (source.IsLooping != component.Loop)
        {
            source.IsLooping = component.Loop;
        }
    }

    #endregion

    #region 私有方法 - 空间音频

    private void ApplySpatialAudio()
    {
        if (_world == null)
        {
            return;
        }

        var listenerQuery = _world.CreateQuery()
            .All<AudioListenerComponent>()
            .All<AudioTransformComponent>()
            .Build();

        AudioTransformComponent listenerTransform = default;
        IAudioListener? listener = null;

        foreach (var entityId in listenerQuery)
        {
            var listenerComp = _world.GetComponent<AudioListenerComponent>(entityId);
            if (listenerComp.Listener != null)
            {
                listener = listenerComp.Listener;
                listenerTransform = _world.GetComponent<AudioTransformComponent>(entityId);
                break;
            }
        }

        if (listener == null)
        {
            return;
        }

        listener.Position = listenerTransform.Position;
        listener.Forward = listenerTransform.Forward;
        listener.Up = listenerTransform.Up;

        var sourceQuery = _world.CreateQuery()
            .All<AudioSourceComponent>()
            .Build();

        foreach (var entityId in sourceQuery)
        {
            if (!_entityToSource.TryGetValue(entityId, out var source))
            {
                continue;
            }

            if (!source.Spatialize)
            {
                continue;
            }

            var sourceTransform = _world.HasComponent<AudioTransformComponent>(entityId)
                ? _world.GetComponent<AudioTransformComponent>(entityId)
                : default;

            var result = _spatialCalculator.Calculate(
                sourceTransform.Position,
                sourceTransform.Velocity,
                listenerTransform.Position,
                listenerTransform.Forward,
                listenerTransform.Up,
                listenerTransform.Velocity,
                source.MinDistance,
                source.MaxDistance,
                source.RolloffMode,
                source.SpatialBlend,
                source.DopplerLevel,
                source.Spread);

            source.Volume = result.Volume * (1f - result.Occlusion);
            source.Pan = result.Pan;
            source.Pitch = result.Pitch;
        }
    }

    #endregion
}

using Gnosis.Core;
using Gnosis.ECS.System;
using Gnosis.Graphic.Light;
using Gnosis.Graphic.Material;
using Gnosis.Graphic.Pipeline;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Render;

public sealed class GenesisRenderSystem : IWorldSystem
{
    #region 字段

    private Gnosis.ECS.World.World? _world;
    private readonly MeshRenderer _meshRenderer;
    private readonly VoxelRenderer? _voxelRenderer;
    private readonly LightSystem _lightSystem;
    private readonly Dictionary<EntityId, RenderItem> _entityToRenderItem = [];
    private readonly Dictionary<EntityId, string> _entityToLightName = [];
    private ICamera? _mainCamera;

    #endregion

    #region 属性

    public SystemPhase Phase => SystemPhase.Render;

    public MeshRenderer MeshRenderer => _meshRenderer;

    public VoxelRenderer? VoxelRenderer => _voxelRenderer;

    public LightSystem LightSystem => _lightSystem;

    public ICamera? MainCamera => _mainCamera;

    #endregion

    #region 构造函数

    public GenesisRenderSystem(MeshRenderer meshRenderer, LightSystem lightSystem, VoxelRenderer? voxelRenderer = null)
    {
        _meshRenderer = meshRenderer;
        _lightSystem = lightSystem;
        _voxelRenderer = voxelRenderer;
    }

    #endregion

    #region 公开方法 - ISystem

    public void Initialize()
    {
    }

    public void Shutdown()
    {
        _meshRenderer.Clear();
        _lightSystem.Clear();
        _entityToRenderItem.Clear();
        _entityToLightName.Clear();
        _mainCamera = null;
    }

    public void Update(float delta)
    {
        if (_world == null)
        {
            return;
        }

        SyncMeshEntities();
        SyncVoxelEntities();
        SyncLightEntities();
        SyncCameraEntities();
    }

    public void SetWorld(Gnosis.ECS.World.World world)
    {
        _world = world;
    }

    #endregion

    #region 私有方法 - Mesh 同步

    private void SyncMeshEntities()
    {
        if (_world == null)
        {
            return;
        }

        var query = _world.CreateQuery()
            .All<MeshRef>()
            .Build();

        var currentEntities = new HashSet<EntityId>(query);

        var removedEntities = new List<EntityId>();
        foreach (var entityId in _entityToRenderItem.Keys)
        {
            if (!currentEntities.Contains(entityId))
            {
                removedEntities.Add(entityId);
            }
        }

        foreach (var entityId in removedEntities)
        {
            if (_entityToRenderItem.Remove(entityId, out var renderItem))
            {
                _meshRenderer.RemoveRenderItem(renderItem);
            }
        }

        foreach (var entityId in currentEntities)
        {
            var meshRef = _world.GetComponent<MeshRef>(entityId);

            if (meshRef.Mesh == null)
            {
                continue;
            }

            MaterialInstance? material = null;
            if (_world.HasComponent<MaterialRef>(entityId))
            {
                var materialRef = _world.GetComponent<MaterialRef>(entityId);
                material = materialRef.Material;
            }

            if (material == null)
            {
                continue;
            }

            if (_entityToRenderItem.TryGetValue(entityId, out var existingItem))
            {
                existingItem.Transform = meshRef.Transform;
                existingItem.Visible = meshRef.Visible;
                existingItem.CastShadows = meshRef.CastShadows;
                existingItem.Material = material;
            }
            else
            {
                var renderItem = _meshRenderer.AddRenderItem(meshRef.Mesh, material, meshRef.Transform);
                renderItem.Visible = meshRef.Visible;
                renderItem.CastShadows = meshRef.CastShadows;
                _entityToRenderItem[entityId] = renderItem;
            }
        }
    }

    #endregion

    #region 私有方法 - Voxel 同步

    private void SyncVoxelEntities()
    {
        if (_world == null || _voxelRenderer == null)
        {
            return;
        }

        var query = _world.CreateQuery()
            .All<VoxelChunkRef>()
            .Build();

        foreach (var entityId in query)
        {
            var voxelRef = _world.GetComponent<VoxelChunkRef>(entityId);

            if (voxelRef.Chunk == null)
            {
                continue;
            }

            voxelRef.Chunk.IsVisible = voxelRef.Visible;
        }

        _voxelRenderer.RebuildDirtyChunks();
    }

    #endregion

    #region 私有方法 - Light 同步

    private void SyncLightEntities()
    {
        if (_world == null)
        {
            return;
        }

        var query = _world.CreateQuery()
            .All<LightRef>()
            .Build();

        var currentEntities = new HashSet<EntityId>(query);

        var removedEntities = new List<EntityId>();
        foreach (var entityId in _entityToLightName.Keys)
        {
            if (!currentEntities.Contains(entityId))
            {
                removedEntities.Add(entityId);
            }
        }

        foreach (var entityId in removedEntities)
        {
            if (_entityToLightName.Remove(entityId, out var lightName))
            {
                _lightSystem.RemoveLight(lightName);
            }
        }

        foreach (var entityId in currentEntities)
        {
            var lightRef = _world.GetComponent<LightRef>(entityId);

            if (lightRef.Light == null)
            {
                continue;
            }

            if (!_entityToLightName.ContainsKey(entityId))
            {
                _lightSystem.AddLight(lightRef.Light);
                _entityToLightName[entityId] = lightRef.Light.Name;
            }
            else
            {
                lightRef.Light.IsEnabled = lightRef.Enabled;
            }
        }
    }

    #endregion

    #region 私有方法 - Camera 同步

    private void SyncCameraEntities()
    {
        if (_world == null)
        {
            return;
        }

        var query = _world.CreateQuery()
            .All<CameraRef>()
            .Build();

        foreach (var entityId in query)
        {
            var cameraRef = _world.GetComponent<CameraRef>(entityId);

            if (cameraRef.Camera == null)
            {
                continue;
            }

            if (cameraRef.IsMainCamera)
            {
                _mainCamera = cameraRef.Camera;
            }
        }
    }

    #endregion
}

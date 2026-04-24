using System.Text;
using Gnosis.Navigation.NavMesh;

namespace Gnosis.Navigation.NavMesh;

/// <summary>
/// 导航网格序列化器，支持导航网格的保存和加载。
/// 使用二进制格式以获得紧凑的存储和快速的读写速度。
/// </summary>
public sealed class NavMeshSerializer
{
    #region 内部常量

    private const uint MagicNumber = 0x4E4D5347;
    private const ushort CurrentVersion = 1;

    #endregion

    #region 公开方法

    /// <summary>
    /// 将导航网格序列化到流
    /// </summary>
    /// <param name="navMesh">导航网格</param>
    /// <param name="stream">目标流</param>
    public void Serialize(NavMesh navMesh, Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write(MagicNumber);
        writer.Write(CurrentVersion);

        WriteString(writer, navMesh.Name);
        writer.Write(navMesh.IsBuilt);

        WriteBuildSettings(writer, navMesh.Settings);

        var polygons = navMesh.Polygons;
        writer.Write(polygons.Count);

        foreach (var polygon in polygons)
        {
            WritePolygon(writer, polygon);
        }
    }

    /// <summary>
    /// 从流反序列化导航网格
    /// </summary>
    /// <param name="stream">源流</param>
    /// <returns>导航网格</returns>
    public NavMesh Deserialize(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var magic = reader.ReadUInt32();
        if (magic != MagicNumber)
        {
            throw new InvalidDataException("无效的导航网格文件格式");
        }

        var version = reader.ReadUInt16();
        if (version > CurrentVersion)
        {
            throw new InvalidDataException($"不支持的导航网格版本: {version}");
        }

        var name = ReadString(reader);
        var isBuilt = reader.ReadBoolean();

        var settings = ReadBuildSettings(reader);

        var navMesh = new NavMesh(name);

        if (isBuilt)
        {
            navMesh.Build(settings);
        }

        var polygonCount = reader.ReadInt32();
        var polygons = new List<NavMeshPolygon>(polygonCount);

        for (var i = 0; i < polygonCount; i++)
        {
            polygons.Add(ReadPolygon(reader));
        }

        navMesh.SetPolygons(polygons);

        return navMesh;
    }

    /// <summary>
    /// 将导航网格序列化到字节数组
    /// </summary>
    /// <param name="navMesh">导航网格</param>
    /// <returns>字节数组</returns>
    public byte[] SerializeToBytes(NavMesh navMesh)
    {
        using var stream = new MemoryStream();
        Serialize(navMesh, stream);
        return stream.ToArray();
    }

    /// <summary>
    /// 从字节数组反序列化导航网格
    /// </summary>
    /// <param name="data">字节数组</param>
    /// <returns>导航网格</returns>
    public NavMesh DeserializeFromBytes(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return Deserialize(stream);
    }

    #endregion

    #region 私有方法 - 写入

    private static void WriteBuildSettings(BinaryWriter writer, NavMeshBuildSettings settings)
    {
        writer.Write(settings.AgentRadius);
        writer.Write(settings.AgentHeight);
        writer.Write(settings.StepHeight);
        writer.Write(settings.SlopeAngle);
        writer.Write(settings.VoxelSize);
        writer.Write(settings.RegionMinArea);
    }

    private static void WritePolygon(BinaryWriter writer, NavMeshPolygon polygon)
    {
        writer.Write(polygon.Id);
        writer.Write(polygon.AreaId);
        writer.Write(polygon.AreaCost);

        var vertexCount = polygon.Vertices.Length / 3;
        writer.Write(vertexCount);

        for (var i = 0; i < polygon.Vertices.Length; i++)
        {
            writer.Write(polygon.Vertices[i]);
        }

        writer.Write(polygon.Neighbors.Length);
        foreach (var neighbor in polygon.Neighbors)
        {
            writer.Write(neighbor);
        }
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    #endregion

    #region 私有方法 - 读取

    private static NavMeshBuildSettings ReadBuildSettings(BinaryReader reader)
    {
        return new NavMeshBuildSettings
        {
            AgentRadius = reader.ReadSingle(),
            AgentHeight = reader.ReadSingle(),
            StepHeight = reader.ReadSingle(),
            SlopeAngle = reader.ReadSingle(),
            VoxelSize = reader.ReadSingle(),
            RegionMinArea = reader.ReadSingle()
        };
    }

    private static NavMeshPolygon ReadPolygon(BinaryReader reader)
    {
        var id = reader.ReadInt32();
        var areaId = reader.ReadInt32();
        var areaCost = reader.ReadSingle();

        var vertexCount = reader.ReadInt32();
        var vertices = new float[vertexCount * 3];

        for (var i = 0; i < vertices.Length; i++)
        {
            vertices[i] = reader.ReadSingle();
        }

        var neighborCount = reader.ReadInt32();
        var neighbors = new int[neighborCount];

        for (var i = 0; i < neighborCount; i++)
        {
            neighbors[i] = reader.ReadInt32();
        }

        return new NavMeshPolygon
        {
            Id = id,
            AreaId = areaId,
            AreaCost = areaCost,
            Vertices = vertices,
            Neighbors = neighbors
        };
    }

    private static string ReadString(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        var bytes = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    #endregion
}

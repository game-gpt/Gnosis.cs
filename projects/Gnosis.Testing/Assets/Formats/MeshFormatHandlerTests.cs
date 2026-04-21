using Gnosis.Assets.Formats;
using Gnosis.Assets.Formats.MeshOptimization;
using NUnit.Framework;

namespace Gnosis.Testing.Assets.Formats;

[TestFixture]
public class MeshFormatHandlerTests : TestBase
{
    #region 测试数据构建

    private static MeshData CreateCubeMesh()
    {
        var vertices = new List<MeshVertex>
        {
            new() { Position = new float[] { -1, -1, 1 }, Uv = new float[] { 0, 0 } },
            new() { Position = new float[] { 1, -1, 1 }, Uv = new float[] { 1, 0 } },
            new() { Position = new float[] { 1, 1, 1 }, Uv = new float[] { 1, 1 } },
            new() { Position = new float[] { -1, 1, 1 }, Uv = new float[] { 0, 1 } },
            new() { Position = new float[] { -1, -1, -1 }, Uv = new float[] { 1, 0 } },
            new() { Position = new float[] { 1, -1, -1 }, Uv = new float[] { 0, 0 } },
            new() { Position = new float[] { 1, 1, -1 }, Uv = new float[] { 0, 1 } },
            new() { Position = new float[] { -1, 1, -1 }, Uv = new float[] { 1, 1 } }
        };

        var indices = new List<int>
        {
            0, 1, 2, 0, 2, 3,
            5, 4, 7, 5, 7, 6,
            1, 5, 6, 1, 6, 2,
            4, 0, 3, 4, 3, 7,
            3, 2, 6, 3, 6, 7,
            4, 5, 1, 4, 1, 0
        };

        return new MeshData
        {
            Name = "TestCube",
            Vertices = vertices,
            Indices = indices
        };
    }

    private static MeshData CreateFlatQuadMesh()
    {
        var vertices = new List<MeshVertex>
        {
            new() { Position = new float[] { 0, 0, 0 }, Uv = new float[] { 0, 0 } },
            new() { Position = new float[] { 1, 0, 0 }, Uv = new float[] { 1, 0 } },
            new() { Position = new float[] { 1, 1, 0 }, Uv = new float[] { 1, 1 } },
            new() { Position = new float[] { 0, 1, 0 }, Uv = new float[] { 0, 1 } }
        };

        var indices = new List<int> { 0, 1, 2, 0, 2, 3 };

        return new MeshData
        {
            Name = "TestQuad",
            Vertices = vertices,
            Indices = indices
        };
    }

    private static MeshData CreateUvMappedQuadMesh()
    {
        var vertices = new List<MeshVertex>
        {
            new()
            {
                Position = new float[] { 0, 0, 0 },
                Normal = new float[] { 0, 0, 1 },
                Uv = new float[] { 0, 0 }
            },
            new()
            {
                Position = new float[] { 1, 0, 0 },
                Normal = new float[] { 0, 0, 1 },
                Uv = new float[] { 1, 0 }
            },
            new()
            {
                Position = new float[] { 1, 1, 0 },
                Normal = new float[] { 0, 0, 1 },
                Uv = new float[] { 1, 1 }
            },
            new()
            {
                Position = new float[] { 0, 1, 0 },
                Normal = new float[] { 0, 0, 1 },
                Uv = new float[] { 0, 1 }
            }
        };

        var indices = new List<int> { 0, 1, 2, 0, 2, 3 };

        return new MeshData
        {
            Name = "UvMappedQuad",
            Vertices = vertices,
            Indices = indices
        };
    }

    private static MeshData CreateInvalidMesh_AllZeroPositions()
    {
        var vertices = new List<MeshVertex>
        {
            new() { Position = new float[] { 0, 0, 0 } },
            new() { Position = new float[] { 0, 0, 0 } },
            new() { Position = new float[] { 0, 0, 0 } }
        };

        var indices = new List<int> { 0, 1, 2 };

        return new MeshData
        {
            Name = "InvalidMesh",
            Vertices = vertices,
            Indices = indices
        };
    }

    private static MeshData CreateInvalidMesh_OutOfRangeIndices()
    {
        var vertices = new List<MeshVertex>
        {
            new() { Position = new float[] { 0, 0, 1 } },
            new() { Position = new float[] { 1, 0, 0 } },
            new() { Position = new float[] { 0, 1, 0 } }
        };

        var indices = new List<int> { 0, 1, 99 };

        return new MeshData
        {
            Name = "InvalidIndices",
            Vertices = vertices,
            Indices = indices
        };
    }

    private static MeshData CreateInvalidMesh_NoTriangles()
    {
        var vertices = new List<MeshVertex>
        {
            new() { Position = new float[] { 1, 0, 0 } }
        };

        var indices = new List<int>();

        return new MeshData
        {
            Name = "NoTriangles",
            Vertices = vertices,
            Indices = indices
        };
    }

    #endregion

    #region Forsyth 优化测试

    [Test]
    public void ForsythOptimize_立方体网格_保持三角形数量不变()
    {
        var cube = CreateCubeMesh();
        var indices = cube.Indices.ToArray();

        var optimized = ForsythVertexCacheOptimizer.Optimize(indices);

        Assert.That(optimized.Length, Is.EqualTo(indices.Length));
    }

    [Test]
    public void ForsythOptimize_立方体网格_保持顶点索引集合不变()
    {
        var cube = CreateCubeMesh();
        var indices = cube.Indices.ToArray();

        var optimized = ForsythVertexCacheOptimizer.Optimize(indices);

        var originalTriangles = new HashSet<(int, int, int)>();
        for (int i = 0; i < indices.Length; i += 3)
        {
            var tri = (indices[i], indices[i + 1], indices[i + 2]);
            originalTriangles.Add(tri);
        }

        var optimizedTriangles = new HashSet<(int, int, int)>();
        for (int i = 0; i < optimized.Length; i += 3)
        {
            var tri = (optimized[i], optimized[i + 1], optimized[i + 2]);
            optimizedTriangles.Add(tri);
        }

        Assert.That(optimizedTriangles, Is.EquivalentTo(originalTriangles));
    }

    [Test]
    public void ForsythOptimize_空索引缓冲区_返回空数组()
    {
        var indices = Array.Empty<int>();

        var optimized = ForsythVertexCacheOptimizer.Optimize(indices);

        Assert.That(optimized, Is.Empty);
    }

    [Test]
    public void ForsythOptimize_单个三角形_保持不变()
    {
        var indices = new[] { 0, 1, 2 };

        var optimized = ForsythVertexCacheOptimizer.Optimize(indices);

        Assert.That(optimized, Is.EqualTo(indices));
    }

    #endregion

    #region 法线生成测试

    [Test]
    public void NormalGenerate_平面四边形_生成Z轴法线()
    {
        var quad = CreateFlatQuadMesh();

        var normals = NormalGenerator.Generate(quad.Vertices, quad.Indices);

        for (int i = 0; i < quad.Vertices.Count; i++)
        {
            Assert.That(normals[i][0], Is.EqualTo(0f).Within(0.001f), $"顶点 {i} 法线 X 分量应为 0");
            Assert.That(normals[i][1], Is.EqualTo(0f).Within(0.001f), $"顶点 {i} 法线 Y 分量应为 0");
            Assert.That(MathF.Abs(normals[i][2]), Is.EqualTo(1f).Within(0.001f), $"顶点 {i} 法线 Z 分量应为 ±1");
        }
    }

    [Test]
    public void NormalGenerate_法线已归一化()
    {
        var quad = CreateFlatQuadMesh();

        var normals = NormalGenerator.Generate(quad.Vertices, quad.Indices);

        for (int i = 0; i < quad.Vertices.Count; i++)
        {
            float length = MathF.Sqrt(normals[i][0] * normals[i][0]
                                      + normals[i][1] * normals[i][1]
                                      + normals[i][2] * normals[i][2]);
            Assert.That(length, Is.EqualTo(1f).Within(0.001f), $"顶点 {i} 法线长度应为 1");
        }
    }

    [Test]
    public void NormalGenerate_退化三角形_跳过不崩溃()
    {
        var vertices = new List<MeshVertex>
        {
            new() { Position = new float[] { 0, 0, 0 } },
            new() { Position = new float[] { 0, 0, 0 } },
            new() { Position = new float[] { 0, 0, 0 } }
        };

        var indices = new List<int> { 0, 1, 2 };

        Assert.DoesNotThrow(() => NormalGenerator.Generate(vertices, indices));
    }

    #endregion

    #region 切线生成测试

    [Test]
    public void TangentGenerate_UV映射四边形_生成X轴切线()
    {
        var quad = CreateUvMappedQuadMesh();

        var tangents = TangentGenerator.Generate(quad.Vertices, quad.Indices);

        for (int i = 0; i < quad.Vertices.Count; i++)
        {
            Assert.That(MathF.Abs(tangents[i][0]), Is.EqualTo(1f).Within(0.01f),
                $"顶点 {i} 切线 X 分量应为 ±1");
            Assert.That(tangents[i][1], Is.EqualTo(0f).Within(0.01f),
                $"顶点 {i} 切线 Y 分量应为 0");
            Assert.That(tangents[i][2], Is.EqualTo(0f).Within(0.01f),
                $"顶点 {i} 切线 Z 分量应为 0");
        }
    }

    [Test]
    public void TangentGenerate_切线与法线正交()
    {
        var quad = CreateUvMappedQuadMesh();

        var tangents = TangentGenerator.Generate(quad.Vertices, quad.Indices);

        for (int i = 0; i < quad.Vertices.Count; i++)
        {
            var normal = quad.Vertices[i].Normal;
            float dot = normal[0] * tangents[i][0]
                        + normal[1] * tangents[i][1]
                        + normal[2] * tangents[i][2];
            Assert.That(dot, Is.EqualTo(0f).Within(0.01f), $"顶点 {i} 切线与法线应正交");
        }
    }

    [Test]
    public void TangentGenerate_手性分量有效()
    {
        var quad = CreateUvMappedQuadMesh();

        var tangents = TangentGenerator.Generate(quad.Vertices, quad.Indices);

        for (int i = 0; i < quad.Vertices.Count; i++)
        {
            Assert.That(MathF.Abs(tangents[i][3]), Is.EqualTo(1f).Within(0.001f),
                $"顶点 {i} 切线手性分量应为 ±1");
        }
    }

    [Test]
    public void TangentGenerate_缺少UV_使用默认切线()
    {
        var vertices = new List<MeshVertex>
        {
            new() { Position = new float[] { 0, 0, 0 }, Normal = new float[] { 0, 0, 1 } },
            new() { Position = new float[] { 1, 0, 0 }, Normal = new float[] { 0, 0, 1 } },
            new() { Position = new float[] { 0, 1, 0 }, Normal = new float[] { 0, 0, 1 } }
        };

        var indices = new List<int> { 0, 1, 2 };

        var tangents = TangentGenerator.Generate(vertices, indices);

        for (int i = 0; i < vertices.Count; i++)
        {
            Assert.That(tangents[i][0], Is.EqualTo(1f), $"顶点 {i} 默认切线 X 应为 1");
            Assert.That(tangents[i][1], Is.EqualTo(0f), $"顶点 {i} 默认切线 Y 应为 0");
            Assert.That(tangents[i][2], Is.EqualTo(0f), $"顶点 {i} 默认切线 Z 应为 0");
        }
    }

    #endregion

    #region 边折叠简化测试

    [Test]
    public void EdgeCollapse_简化立方体_减少三角形数量()
    {
        var cube = CreateCubeMesh();
        int originalTriCount = cube.Indices.Count / 3;

        var (newVertices, newIndices) = EdgeCollapser.Simplify(
            cube.Vertices, cube.Indices, 0.5f);

        int newTriCount = newIndices.Count / 3;
        Assert.That(newTriCount, Is.LessThan(originalTriCount),
            $"简化后三角形数量应减少，原始 {originalTriCount}，简化后 {newTriCount}");
    }

    [Test]
    public void EdgeCollapse_简化目标为1_至少保留1个三角形()
    {
        var cube = CreateCubeMesh();

        var (newVertices, newIndices) = EdgeCollapser.Simplify(
            cube.Vertices, cube.Indices, 0.05f);

        int newTriCount = newIndices.Count / 3;
        Assert.That(newTriCount, Is.GreaterThanOrEqualTo(1),
            "简化后应至少保留 1 个三角形");
    }

    [Test]
    public void EdgeCollapse_简化后索引引用有效顶点()
    {
        var cube = CreateCubeMesh();

        var (newVertices, newIndices) = EdgeCollapser.Simplify(
            cube.Vertices, cube.Indices, 0.5f);

        foreach (int idx in newIndices)
        {
            Assert.That(idx, Is.GreaterThanOrEqualTo(0), "索引不应为负数");
            Assert.That(idx, Is.LessThan(newVertices.Count), "索引不应超出顶点范围");
        }
    }

    [Test]
    public void EdgeCollapse_目标比例1_保持不变()
    {
        var cube = CreateCubeMesh();

        var (newVertices, newIndices) = EdgeCollapser.Simplify(
            cube.Vertices, cube.Indices, 1.0f);

        Assert.That(newIndices.Count / 3, Is.EqualTo(cube.Indices.Count / 3));
    }

    #endregion

    #region OptimizeAsync 集成测试

    [Test]
    public async Task OptimizeAsync_生成法线_缺少法线的顶点获得法线()
    {
        var handler = new MeshFormatHandler();
        var cube = CreateCubeMesh();

        var result = await handler.OptimizeAsync(cube, new MeshOptimizationOptions
        {
            GenerateNormals = true,
            OptimizeIndices = false,
            OptimizeVertices = false
        });

        foreach (var vertex in result.Vertices)
        {
            Assert.That(vertex.Normal, Is.Not.Null, "顶点应有法线");
            Assert.That(vertex.Normal.Length, Is.GreaterThanOrEqualTo(3), "法线应有至少 3 个分量");
        }
    }

    [Test]
    public async Task OptimizeAsync_生成切线_有法线和UV的顶点获得切线()
    {
        var handler = new MeshFormatHandler();
        var quad = CreateUvMappedQuadMesh();

        var result = await handler.OptimizeAsync(quad, new MeshOptimizationOptions
        {
            GenerateTangents = true,
            OptimizeIndices = false,
            OptimizeVertices = false
        });

        foreach (var vertex in result.Vertices)
        {
            Assert.That(vertex.Tangent, Is.Not.Null, "顶点应有切线");
            Assert.That(vertex.Tangent.Length, Is.GreaterThanOrEqualTo(4), "切线应有至少 4 个分量");
        }
    }

    [Test]
    public async Task OptimizeAsync_索引优化_保持三角形数量()
    {
        var handler = new MeshFormatHandler();
        var cube = CreateCubeMesh();

        var result = await handler.OptimizeAsync(cube, new MeshOptimizationOptions
        {
            OptimizeIndices = true,
            OptimizeVertices = false
        });

        Assert.That(result.Indices.Count, Is.EqualTo(cube.Indices.Count));
    }

    [Test]
    public async Task OptimizeAsync_完整优化_保持网格有效性()
    {
        var handler = new MeshFormatHandler();
        var cube = CreateCubeMesh();

        var result = await handler.OptimizeAsync(cube, new MeshOptimizationOptions
        {
            GenerateNormals = true,
            GenerateTangents = true,
            OptimizeIndices = true,
            OptimizeVertices = true
        });

        foreach (int idx in result.Indices)
        {
            Assert.That(idx, Is.GreaterThanOrEqualTo(0), "索引不应为负数");
            Assert.That(idx, Is.LessThan(result.Vertices.Count), "索引不应超出顶点范围");
        }

        Assert.That(result.Indices.Count / 3, Is.EqualTo(cube.Indices.Count / 3),
            "优化后三角形数量应保持不变");
    }

    [Test]
    public async Task OptimizeAsync_简化_减少三角形数量()
    {
        var handler = new MeshFormatHandler();
        var cube = CreateCubeMesh();

        var result = await handler.OptimizeAsync(cube, new MeshOptimizationOptions
        {
            Simplify = true,
            SimplifyTarget = 0.5f,
            OptimizeIndices = false
        });

        Assert.That(result.Indices.Count / 3, Is.LessThan(cube.Indices.Count / 3));
    }

    [Test]
    public async Task OptimizeAsync_默认选项_不崩溃()
    {
        var handler = new MeshFormatHandler();
        var cube = CreateCubeMesh();

        var result = await handler.OptimizeAsync(cube);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Vertices.Count, Is.GreaterThan(0));
    }

    #endregion

    #region ValidateAsync 测试

    [Test]
    public void ValidateMeshData_有效网格_返回True()
    {
        var cube = CreateCubeMesh();
        var handler = new MeshFormatHandler();

        bool result = handler.ValidateAsync(string.Empty).GetAwaiter().GetResult();

        Assert.That(result, Is.True);
    }

    [Test]
    public void ValidateMeshData_全零位置_返回False()
    {
        var mesh = CreateInvalidMesh_AllZeroPositions();

        Assert.That(mesh.Indices.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(mesh.Vertices.Count, Is.GreaterThan(0));

        foreach (int idx in mesh.Indices)
        {
            Assert.That(idx, Is.GreaterThanOrEqualTo(0));
            Assert.That(idx, Is.LessThan(mesh.Vertices.Count));
        }

        bool hasNonZero = false;
        foreach (var v in mesh.Vertices)
        {
            if (v.Position is { Length: >= 3 })
            {
                float lenSq = v.Position[0] * v.Position[0]
                              + v.Position[1] * v.Position[1]
                              + v.Position[2] * v.Position[2];
                if (lenSq > 1e-10f)
                {
                    hasNonZero = true;
                }
            }
        }

        Assert.That(hasNonZero, Is.False, "测试网格应全为零位置");
    }

    [Test]
    public void ValidateMeshData_越界索引_返回False()
    {
        var mesh = CreateInvalidMesh_OutOfRangeIndices();

        bool hasInvalid = false;
        foreach (int idx in mesh.Indices)
        {
            if (idx < 0 || idx >= mesh.Vertices.Count)
            {
                hasInvalid = true;
            }
        }

        Assert.That(hasInvalid, Is.True, "测试网格应有越界索引");
    }

    [Test]
    public void ValidateMeshData_无三角形_返回False()
    {
        var mesh = CreateInvalidMesh_NoTriangles();

        Assert.That(mesh.Indices.Count, Is.LessThan(3), "测试网格应无三角形");
    }

    #endregion
}

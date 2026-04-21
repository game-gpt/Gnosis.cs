using Gnosis.ECS;
using Gnosis.ECS.Core;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Testing.ECS;

[TestFixture]
public class ComponentSerializerTests : TestBase
{
    private ComponentSerializer _serializer;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _serializer = new ComponentSerializer();
    }

    [Test]
    public void SerializeDeserialize_RoundTrip_Position()
    {
        var position = new Position(1.0f, 2.0f, 3.0f);
        var json = _serializer.Serialize(position);
        var result = _serializer.Deserialize<Position>(json);

        Assert.That(result, Is.EqualTo(position), "反序列化后的值应与原始值一致");
    }

    [Test]
    public void Serialize_ContainsTypeField()
    {
        var position = new Position(1.0f, 2.0f, 3.0f);
        var json = _serializer.Serialize(position);

        Assert.That(json, Does.Contain("$type"), "序列化结果应包含 $type 字段");
        Assert.That(json, Does.Contain(nameof(Position)), "序列化结果应包含类型名称");
    }

    [Test]
    public void Serialize_ContainsDataField()
    {
        var position = new Position(1.0f, 2.0f, 3.0f);
        var json = _serializer.Serialize(position);

        Assert.That(json, Does.Contain("data"), "序列化结果应包含 data 字段");
    }

    [Test]
    public void Deserialize_NonGeneric_ReturnsCorrectType()
    {
        var position = new Position(1.0f, 2.0f, 3.0f);
        var json = _serializer.Serialize(position);
        var result = _serializer.Deserialize(json, typeof(Position));

        Assert.That(result, Is.InstanceOf<Position>(), "非泛型反序列化应返回正确类型");
        Assert.That((Position)result, Is.EqualTo(position), "非泛型反序列化值应与原始值一致");
    }

    [Test]
    public void SerializeDeserialize_MultipleCycles_PreservesData()
    {
        var position = new Position(5.0f, -3.0f, 7.5f);

        var json1 = _serializer.Serialize(position);
        var result1 = _serializer.Deserialize<Position>(json1);

        var json2 = _serializer.Serialize(result1);
        var result2 = _serializer.Deserialize<Position>(json2);

        Assert.That(result2, Is.EqualTo(position), "多次序列化/反序列化后值应保持一致");
    }

    [Test]
    public void Serialize_ZeroValues()
    {
        var position = new Position(0.0f, 0.0f, 0.0f);
        var json = _serializer.Serialize(position);
        var result = _serializer.Deserialize<Position>(json);

        Assert.That(result, Is.EqualTo(position), "零值序列化/反序列化应正确");
    }

    [Test]
    public void Serialize_NegativeValues()
    {
        var position = new Position(-1.5f, -2.5f, -3.5f);
        var json = _serializer.Serialize(position);
        var result = _serializer.Deserialize<Position>(json);

        Assert.That(result, Is.EqualTo(position), "负值序列化/反序列化应正确");
    }
}

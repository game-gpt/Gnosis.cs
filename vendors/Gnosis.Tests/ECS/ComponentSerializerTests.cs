using Gnosis.ECS.Component;
using NUnit.Framework;

namespace Gnosis.ECS;

[TestFixture]
public class ComponentSerializerTests
{
    private ComponentSerializer _serializer;

    private struct TestPosition
    {
        public float X;
        public float Y;
        public float Z;

        public TestPosition(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    [SetUp]
    public void Setup()
    {
        _serializer = new ComponentSerializer();
    }

    [Test]
    public void SerializeDeserialize_RoundTrip_Position()
    {
        var position = new TestPosition(1.0f, 2.0f, 3.0f);
        var json = _serializer.Serialize(position);
        var result = _serializer.Deserialize<TestPosition>(json);

        Assert.That(result.X, Is.EqualTo(1.0f), "X 应为 1.0");
        Assert.That(result.Y, Is.EqualTo(2.0f), "Y 应为 2.0");
        Assert.That(result.Z, Is.EqualTo(3.0f), "Z 应为 3.0");
    }

    [Test]
    public void Serialize_ContainsTypeField()
    {
        var position = new TestPosition(1.0f, 2.0f, 3.0f);
        var json = _serializer.Serialize(position);

        Assert.That(json, Does.Contain("$type"), "序列化结果应包含 $type 字段");
        Assert.That(json, Does.Contain(nameof(TestPosition)), "序列化结果应包含类型名称");
    }

    [Test]
    public void Serialize_ContainsDataField()
    {
        var position = new TestPosition(1.0f, 2.0f, 3.0f);
        var json = _serializer.Serialize(position);

        Assert.That(json, Does.Contain("data"), "序列化结果应包含 data 字段");
    }

    [Test]
    public void Deserialize_NonGeneric_ReturnsCorrectType()
    {
        var position = new TestPosition(1.0f, 2.0f, 3.0f);
        var json = _serializer.Serialize(position);
        var result = _serializer.Deserialize(json, typeof(TestPosition));

        Assert.That(result, Is.InstanceOf<TestPosition>(), "非泛型反序列化应返回正确类型");
        var typed = (TestPosition)result;
        Assert.That(typed.X, Is.EqualTo(1.0f), "非泛型反序列化 X 应为 1.0");
    }

    [Test]
    public void SerializeDeserialize_MultipleCycles_PreservesData()
    {
        var position = new TestPosition(5.0f, -3.0f, 7.5f);

        var json1 = _serializer.Serialize(position);
        var result1 = _serializer.Deserialize<TestPosition>(json1);

        var json2 = _serializer.Serialize(result1);
        var result2 = _serializer.Deserialize<TestPosition>(json2);

        Assert.That(result2.X, Is.EqualTo(5.0f), "多次序列化后 X 应保持一致");
        Assert.That(result2.Y, Is.EqualTo(-3.0f), "多次序列化后 Y 应保持一致");
        Assert.That(result2.Z, Is.EqualTo(7.5f), "多次序列化后 Z 应保持一致");
    }

    [Test]
    public void Serialize_ZeroValues()
    {
        var position = new TestPosition(0.0f, 0.0f, 0.0f);
        var json = _serializer.Serialize(position);
        var result = _serializer.Deserialize<TestPosition>(json);

        Assert.That(result.X, Is.EqualTo(0.0f), "零值 X 应正确");
        Assert.That(result.Y, Is.EqualTo(0.0f), "零值 Y 应正确");
        Assert.That(result.Z, Is.EqualTo(0.0f), "零值 Z 应正确");
    }

    [Test]
    public void Serialize_NegativeValues()
    {
        var position = new TestPosition(-1.5f, -2.5f, -3.5f);
        var json = _serializer.Serialize(position);
        var result = _serializer.Deserialize<TestPosition>(json);

        Assert.That(result.X, Is.EqualTo(-1.5f), "负值 X 应正确");
        Assert.That(result.Y, Is.EqualTo(-2.5f), "负值 Y 应正确");
        Assert.That(result.Z, Is.EqualTo(-3.5f), "负值 Z 应正确");
    }
}

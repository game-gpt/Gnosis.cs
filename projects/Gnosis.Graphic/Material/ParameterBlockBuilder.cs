namespace Gnosis.Graphic.Material;

public sealed class ParameterBlockBuilder
{
    private readonly string _name;
    private readonly List<(string Name, MaterialPropertyType Type, int Size)> _entries = [];
    private int _currentOffset;

    public ParameterBlockBuilder(string name)
    {
        _name = name;
        _currentOffset = 0;
    }

    public ParameterBlockBuilder AddFloat(string name)
    {
        AddEntry(name, MaterialPropertyType.Float, sizeof(float));
        return this;
    }

    public ParameterBlockBuilder AddFloat2(string name)
    {
        AlignTo(sizeof(float) * 2);
        AddEntry(name, MaterialPropertyType.Float2, sizeof(float) * 2);
        return this;
    }

    public ParameterBlockBuilder AddFloat3(string name)
    {
        AlignTo(sizeof(float) * 4);
        AddEntry(name, MaterialPropertyType.Float3, sizeof(float) * 3);
        PadTo(sizeof(float) * 4);
        return this;
    }

    public ParameterBlockBuilder AddFloat4(string name)
    {
        AlignTo(sizeof(float) * 4);
        AddEntry(name, MaterialPropertyType.Float4, sizeof(float) * 4);
        return this;
    }

    public ParameterBlockBuilder AddInt(string name)
    {
        AlignTo(sizeof(int));
        AddEntry(name, MaterialPropertyType.Int, sizeof(int));
        return this;
    }

    public ParameterBlockBuilder AddMatrix4x4(string name)
    {
        AlignTo(sizeof(float) * 4 * 4);
        AddEntry(name, MaterialPropertyType.Matrix4x4, sizeof(float) * 4 * 4);
        return this;
    }

    public ParameterBlock Build()
    {
        AlignTo(16);
        var block = new ParameterBlock(_name, (uint)_currentOffset);

        foreach (var (name, type, _) in _entries)
        {
            var offset = 0;
            foreach (var (eName, _, _) in _entries)
            {
                if (eName == name)
                {
                    break;
                }

                offset += GetPaddedSize(type);
            }

            block.RegisterParameter(name, offset, type);
        }

        return block;
    }

    public static ParameterBlockBuilder FromTemplate(MaterialTemplate template)
    {
        var builder = new ParameterBlockBuilder($"ParamBlock_{template.Name}");

        foreach (var prop in template.Properties)
        {
            switch (prop.Type)
            {
                case MaterialPropertyType.Float:
                    builder.AddFloat(prop.Name);
                    break;
                case MaterialPropertyType.Float2:
                    builder.AddFloat2(prop.Name);
                    break;
                case MaterialPropertyType.Float3:
                    builder.AddFloat3(prop.Name);
                    break;
                case MaterialPropertyType.Float4:
                    builder.AddFloat4(prop.Name);
                    break;
                case MaterialPropertyType.Int:
                    builder.AddInt(prop.Name);
                    break;
                case MaterialPropertyType.Matrix4x4:
                    builder.AddMatrix4x4(prop.Name);
                    break;
            }
        }

        return builder;
    }

    private void AddEntry(string name, MaterialPropertyType type, int size)
    {
        _entries.Add((name, type, size));
        _currentOffset += GetPaddedSize(type);
    }

    private void AlignTo(int alignment)
    {
        var remainder = _currentOffset % alignment;
        if (remainder != 0)
        {
            _currentOffset += alignment - remainder;
        }
    }

    private void PadTo(int targetSize)
    {
        _currentOffset = Math.Max(_currentOffset, targetSize);
    }

    private static int GetPaddedSize(MaterialPropertyType type)
    {
        return type switch
        {
            MaterialPropertyType.Float => sizeof(float),
            MaterialPropertyType.Float2 => sizeof(float) * 2,
            MaterialPropertyType.Float3 => sizeof(float) * 4,
            MaterialPropertyType.Float4 => sizeof(float) * 4,
            MaterialPropertyType.Int => sizeof(int),
            MaterialPropertyType.Matrix4x4 => sizeof(float) * 16,
            _ => 0
        };
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnosis.ECS;

public class ComponentSerializer
{
    private readonly JsonSerializerOptions _options;

    public ComponentSerializer()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _options.Converters.Add(new ComponentJsonConverterFactory());
    }

    public string Serialize<T>(T component) where T : struct
    {
        var wrapper = new ComponentWrapper<T>
        {
            Type = typeof(T).AssemblyQualifiedName!,
            Data = component
        };
        return JsonSerializer.Serialize(wrapper, _options);
    }

    public T Deserialize<T>(string json) where T : struct
    {
        var wrapper = JsonSerializer.Deserialize<ComponentWrapper<T>>(json, _options);
        if (wrapper == null)
        {
            throw new InvalidOperationException("反序列化失败：JSON 为空");
        }
        return wrapper.Data;
    }

    public object Deserialize(string json, Type componentType)
    {
        var wrapperType = typeof(ComponentWrapper<>).MakeGenericType(componentType);
        var wrapper = JsonSerializer.Deserialize(json, wrapperType, _options);
        if (wrapper == null)
        {
            throw new InvalidOperationException("反序列化失败：JSON 为空");
        }
        var dataProperty = wrapperType.GetProperty("Data")!;
        return dataProperty.GetValue(wrapper)!;
    }

    private class ComponentWrapper<T>
    {
        [JsonPropertyName("$type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public T Data { get; set; } = default!;
    }

    private class ComponentJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ComponentWrapper<>);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var componentType = typeToConvert.GetGenericArguments()[0];
            var converterType = typeof(ComponentWrapperConverter<>).MakeGenericType(componentType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }

    private class ComponentWrapperConverter<T> : JsonConverter<ComponentWrapper<T>> where T : struct
    {
        public override ComponentWrapper<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var wrapper = new ComponentWrapper<T>();

            if (root.TryGetProperty("$type", out var typeProp))
            {
                wrapper.Type = typeProp.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("data", out var dataProp))
            {
                wrapper.Data = JsonSerializer.Deserialize<T>(dataProp.GetRawText(), options);
            }

            return wrapper;
        }

        public override void Write(Utf8JsonWriter writer, ComponentWrapper<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("$type", value.Type);
            writer.WritePropertyName("data");
            JsonSerializer.Serialize(writer, value.Data, options);
            writer.WriteEndObject();
        }
    }
}

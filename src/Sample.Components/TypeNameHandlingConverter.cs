using MassTransit;
using Newtonsoft.Json;

namespace Sample.Components;

/// <summary>
/// Serializes and deserializes non-typed objects with type information.
/// </summary>
public class TypeNameHandlingConverter : JsonConverter
{
    private readonly TypeNameHandling _typeNameHandling;
    private readonly JsonSerializer _serializer;

    /// <summary>
    /// The constructor.
    /// </summary>
    /// <param name="typeNameHandling">Specifies type name handling options. See <see cref="TypeNameHandling"/></param>
    public TypeNameHandlingConverter(TypeNameHandling typeNameHandling)
    {
        _typeNameHandling = typeNameHandling;
        _serializer = new JsonSerializer { TypeNameHandling = _typeNameHandling };
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        _serializer.Serialize(writer, value);
    }

    /// <inheritdoc/>
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return _serializer.Deserialize(reader, objectType);
    }

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType)
    {
        return !IsMassTransitOrSystemType(objectType);
    }

    static bool IsMassTransitOrSystemType(Type objectType)
    {
        return objectType.Assembly == typeof(IConsumer).Assembly || // MassTransit.Abstractions
               objectType.Assembly == typeof(MassTransitBus).Assembly || // MassTransit
               objectType.Assembly.IsDynamic ||
               objectType.Assembly == typeof(object).Assembly;
    }
}
using System.Text.Json;
using System.Text.Json.Serialization;


// === Convertidor genérico con $type ===

public class TypeDiscriminatorConverter<TBase> : JsonConverter<TBase> where TBase : class
{
    private readonly string _discriminator;
    private readonly Dictionary<string, Type> _typeMap;

    public TypeDiscriminatorConverter(string discriminator, Dictionary<string, Type> typeMap)
    {
        _discriminator = discriminator;
        _typeMap = typeMap;
    }

    public override TBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty(_discriminator, out var prop))
            throw new JsonException($"Falta el campo '{_discriminator}' en el JSON.");

        var discriminatorValue = prop.GetString();

        if (discriminatorValue == null || !_typeMap.TryGetValue(discriminatorValue, out var targetType))
            throw new NotSupportedException($"Tipo '{discriminatorValue}' no soportado.");

        return (TBase?)JsonSerializer.Deserialize(root.GetRawText(), targetType, options);
    }

    public override void Write(Utf8JsonWriter writer, TBase value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value!, value.GetType(), options);
    }
}

// === Programa CLI ===

class Program
{
    static void Main()
    {
        var tipoMap = new Dictionary<string, Type>
        {
            ["suma"] = typeof(OperacionSuma),
            ["resta"] = typeof(OperacionResta)
        };

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        options.Converters.Add(new TypeDiscriminatorConverter<Operacion>("$type", tipoMap));

        // Puedes cambiar entre los JSONs para probar ambos casos
        string json = """
        {
            "$type": "suma",
            "A": 10,
            "B": 5
        }
        """;

        var operacion = JsonSerializer.Deserialize<Operacion>(json, options);

        if (operacion != null)
        {
            Console.WriteLine($"Tipo: {operacion.GetType().Name}");
            Console.WriteLine($"Resultado: {operacion.Ejecutar()}");
        }
        else
        {
            Console.WriteLine("Error al deserializar la operación.");
        }
    }
}

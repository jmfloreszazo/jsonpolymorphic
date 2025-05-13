using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

BenchmarkRunner.Run<OperacionBenchmarks>();

// Ejecutar: dotnet run -c Release

// ==== MODELOS COMUNES ====
public abstract class Operacion
{
    public abstract int Ejecutar();
}

// Modelo base para JsonPolymorphic
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(OperacionSuma), "suma")]
[JsonDerivedType(typeof(OperacionResta), "resta")]
public abstract class OperacionConAtributo : Operacion { }

public class OperacionSuma : OperacionConAtributo
{
    public int A { get; set; }
    public int B { get; set; }
    public override int Ejecutar() => A + B;
}

public class OperacionResta : OperacionConAtributo
{
    public int A { get; set; }
    public int B { get; set; }
    public override int Ejecutar() => A - B;
}

// ==== JsonConverter manual con "Tipo" ====
public class OperacionConverter : JsonConverter<Operacion>
{
    public override Operacion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var tipo = root.GetProperty("Tipo").GetString();

        return tipo switch
        {
            "suma" => JsonSerializer.Deserialize<OperacionSuma>(root.GetRawText(), options),
            "resta" => JsonSerializer.Deserialize<OperacionResta>(root.GetRawText(), options),
            _ => throw new NotSupportedException()
        };
    }

    public override void Write(Utf8JsonWriter writer, Operacion value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}

// ==== Convertidor genérico con "$type" ====
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
            throw new JsonException($"Falta '{_discriminator}' en el JSON.");

        var value = prop.GetString();
        if (value == null || !_typeMap.TryGetValue(value, out var targetType))
            throw new NotSupportedException($"Tipo '{value}' no soportado.");

        return (TBase?)JsonSerializer.Deserialize(root.GetRawText(), targetType, options);
    }

    public override void Write(Utf8JsonWriter writer, TBase value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value!, value.GetType(), options);
    }
}

// ==== BENCHMARK ====
[MemoryDiagnoser]
public class OperacionBenchmarks
{
    private const string CustomJson = """
        {
            "Tipo": "suma",
            "A": 10,
            "B": 20
        }
        """;

    private const string PolymorphicJson = """
        {
            "$type": "suma",
            "A": 10,
            "B": 20
        }
        """;

    private readonly JsonSerializerOptions customOptions;
    private readonly JsonSerializerOptions resolverOptions;
    private readonly JsonSerializerOptions attributeOptions;
    private readonly JsonSerializerOptions genericOptions;

    public OperacionBenchmarks()
    {
        // JsonConverter personalizado (con "Tipo")
        customOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        customOptions.Converters.Add(new OperacionConverter());

        // PolymorphismOptions con resolver
        var resolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                ti =>
                {
                    if (ti.Type == typeof(Operacion))
                    {
                        ti.PolymorphismOptions = new JsonPolymorphismOptions
                        {
                            TypeDiscriminatorPropertyName = "$type",
                            DerivedTypes =
                            {
                                new JsonDerivedType(typeof(OperacionSuma), "suma"),
                                new JsonDerivedType(typeof(OperacionResta), "resta")
                            }
                        };
                    }
                }
            }
        };

        resolverOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = resolver
        };

        // JsonPolymorphic con atributos
        attributeOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Genérico con $type
        genericOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        genericOptions.Converters.Add(new TypeDiscriminatorConverter<Operacion>("$type", new Dictionary<string, Type>
        {
            ["suma"] = typeof(OperacionSuma),
            ["resta"] = typeof(OperacionResta)
        }));
    }

    [Benchmark]
    public Operacion Deserialize_CustomConverter()
        => JsonSerializer.Deserialize<Operacion>(CustomJson, customOptions)!;

    [Benchmark]
    public Operacion Deserialize_PolymorphismOptions()
        => JsonSerializer.Deserialize<Operacion>(PolymorphicJson, resolverOptions)!;

    [Benchmark]
    public Operacion Deserialize_JsonPolymorphicAttribute()
        => JsonSerializer.Deserialize<OperacionConAtributo>(PolymorphicJson, attributeOptions)!;

    [Benchmark]
    public Operacion Deserialize_GenericTypeConverter()
        => JsonSerializer.Deserialize<Operacion>(PolymorphicJson, genericOptions)!;

    [Benchmark]
    public string Serialize_CustomConverter()
        => JsonSerializer.Serialize<Operacion>(new OperacionSuma { A = 10, B = 20 }, customOptions);

    [Benchmark]
    public string Serialize_PolymorphismOptions()
        => JsonSerializer.Serialize<Operacion>(new OperacionSuma { A = 10, B = 20 }, resolverOptions);

    [Benchmark]
    public string Serialize_JsonPolymorphicAttribute()
    {
        OperacionConAtributo operacion = new OperacionSuma { A = 10, B = 20 };
        return JsonSerializer.Serialize(operacion, attributeOptions);
    }

    [Benchmark]
    public string Serialize_GenericTypeConverter()
        => JsonSerializer.Serialize<Operacion>(new OperacionSuma { A = 10, B = 20 }, genericOptions);
}
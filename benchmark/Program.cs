using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

BenchmarkRunner.Run<OperacionBenchmarks>();

// Ejecutar Test => dotnet run -c Release

// ==== CONVERTIDOR PERSONALIZADO ====
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
    private readonly JsonSerializerOptions polymorphicOptions;

    public OperacionBenchmarks()
    {
        customOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        customOptions.Converters.Add(new OperacionConverter());

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

        polymorphicOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = resolver
        };
    }

    [Benchmark]
    public Operacion Deserialize_CustomConverter()
    {
        return JsonSerializer.Deserialize<Operacion>(CustomJson, customOptions)!;
    }

    [Benchmark]
    public Operacion Deserialize_PolymorphismOptions()
    {
        return JsonSerializer.Deserialize<Operacion>(PolymorphicJson, polymorphicOptions)!;
    }

    [Benchmark]
    public string Serialize_CustomConverter()
    {
        var operacion = new OperacionSuma { A = 10, B = 20 };
        return JsonSerializer.Serialize<Operacion>(operacion, customOptions);
    }

    [Benchmark]
    public string Serialize_PolymorphismOptions()
    {
        var operacion = new OperacionSuma { A = 10, B = 20 };
        return JsonSerializer.Serialize<Operacion>(operacion, polymorphicOptions);
    }
}

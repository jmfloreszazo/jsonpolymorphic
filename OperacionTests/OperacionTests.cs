using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;


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
            _ => throw new NotSupportedException($"Tipo no soportado: {tipo}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Operacion value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}

public class OperacionTests
{
    [Fact]
    public void Estrategia_CustomConverter_OperacionSuma()
    {
        string json = """
        {
            "Tipo": "suma",
            "A": 5,
            "B": 7
        }
        """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new OperacionConverter() }
        };

        var operacion = JsonSerializer.Deserialize<Operacion>(json, options);
        Assert.NotNull(operacion);
        Assert.IsType<OperacionSuma>(operacion);
        Assert.Equal(12, operacion!.Ejecutar());
    }

    [Fact]
    public void Estrategia_CustomConverter_OperacionResta()
    {
        string json = """
        {
            "Tipo": "resta",
            "A": 10,
            "B": 4
        }
        """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new OperacionConverter() }
        };

        var operacion = JsonSerializer.Deserialize<Operacion>(json, options);
        Assert.NotNull(operacion);
        Assert.IsType<OperacionResta>(operacion);
        Assert.Equal(6, operacion!.Ejecutar());
    }

    [Fact]
    public void Polimorfismo_Resolver_OperacionSuma()
    {
        string json = """
        {
            "$type": "suma",
            "A": 20,
            "B": 10
        }
        """;

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

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = resolver,
            PropertyNameCaseInsensitive = true
        };

        var operacion = JsonSerializer.Deserialize<Operacion>(json, options);
        Assert.NotNull(operacion);
        Assert.IsType<OperacionSuma>(operacion);
        Assert.Equal(30, operacion!.Ejecutar());
    }

    [Fact]
    public void Polimorfismo_Resolver_OperacionResta()
    {
        string json = """
        {
            "$type": "resta",
            "A": 30,
            "B": 8
        }
        """;

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

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = resolver,
            PropertyNameCaseInsensitive = true
        };

        var operacion = JsonSerializer.Deserialize<Operacion>(json, options);
        Assert.NotNull(operacion);
        Assert.IsType<OperacionResta>(operacion);
        Assert.Equal(22, operacion!.Ejecutar());
    }
}

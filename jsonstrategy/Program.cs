using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

// Clase base con convertidor
[JsonConverter(typeof(OperacionConverter))]
public abstract class Operacion
{
    public string Tipo { get; set; }
    public abstract int Ejecutar();
}

// Estrategia 1
public class OperacionSuma : Operacion
{
    public int A { get; set; }
    public int B { get; set; }

    public override int Ejecutar() => A + B;
}

// Estrategia 2
public class OperacionResta : Operacion
{
    public int A { get; set; }
    public int B { get; set; }

    public override int Ejecutar() => A - B;
}

// Convertidor personalizado que actúa como despachador de estrategias
public class OperacionConverter : JsonConverter<Operacion>
{
    public override Operacion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        if (!root.TryGetProperty("Tipo", out var tipoProp))
            throw new JsonException("Falta propiedad 'Tipo' en el JSON.");

        string? tipo = tipoProp.GetString();

        return tipo switch
        {
            "suma" => JsonSerializer.Deserialize<OperacionSuma>(root.GetRawText(), options),
            "resta" => JsonSerializer.Deserialize<OperacionResta>(root.GetRawText(), options),
            _ => throw new NotSupportedException($"Tipo de operación no soportado: {tipo}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Operacion value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}

// Servicio de ejecución de operaciones
public class OperacionExecutor
{
    public int EjecutarOperacion(Operacion operacion)
    {
        return operacion.Ejecutar();
    }
}

// Aplicación CLI
class Program
{
    static void Main(string[] args)
    {
        // Configuración de inyección de dependencias
        var services = new ServiceCollection();
        services.AddSingleton<OperacionExecutor>();
        var provider = services.BuildServiceProvider();

        // JSON de entrada con tipo explícito
        string json = """
        {
          "Tipo": "suma",
          "A": 8,
          "B": 12
        }
        """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            Operacion operacion = JsonSerializer.Deserialize<Operacion>(json, options)!;

            var executor = provider.GetRequiredService<OperacionExecutor>();
            int resultado = executor.EjecutarOperacion(operacion);

            Console.WriteLine($"Resultado: {resultado}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

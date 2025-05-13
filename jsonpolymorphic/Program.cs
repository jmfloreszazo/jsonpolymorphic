using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;

// Servicio que ejecuta operaciones
public class OperacionExecutor
{
    public int EjecutarOperacion(Operacion operacion)
    {
        return operacion.Ejecutar();
    }
}

class Program
{
    static void Main(string[] args)
    {
        // Inyección de dependencias
        var services = new ServiceCollection();
        services.AddSingleton<OperacionExecutor>();
        var provider = services.BuildServiceProvider();

        // JSON de entrada
        string json = """
        {
          "$type": "suma",
          "A": 10,
          "B": 25
        }
        """;

        // Configurar JsonSerializerOptions con polimorfismo
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
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            TypeInfoResolver = resolver
        };

        try
        {
            var operacion = JsonSerializer.Deserialize<Operacion>(json, options);

            if (operacion == null)
            {
                Console.WriteLine("No se pudo deserializar la operación.");
                return;
            }

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

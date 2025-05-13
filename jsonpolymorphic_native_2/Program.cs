using System.Text.Json;
using System.Text.Json.Serialization;

// Clase base con discriminador de tipo
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(OperacionSuma), "suma")]
[JsonDerivedType(typeof(OperacionResta), "resta")]
public abstract class Operacion
{
    public abstract int Ejecutar();
}

// Implementación para suma
public class OperacionSuma : Operacion
{
    public int A { get; set; }
    public int B { get; set; }

    public override int Ejecutar() => A + B;
}

// Implementación para resta
public class OperacionResta : Operacion
{
    public int A { get; set; }
    public int B { get; set; }

    public override int Ejecutar() => A - B;
}

class Program
{
    static void Main(string[] args)
    {
        // JSON de ejemplo con discriminador de tipo
        string sumaJson = """
                          {
                              "$type": "suma",
                              "A": 12,
                              "B": 8
                          }
                          """;

        string restaJson = """
                           {
                               "$type": "resta",
                               "A": 20,
                               "B": 5
                           }
                           """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        // Puedes alternar entre sumaJson y restaJson
        var operacion = JsonSerializer.Deserialize<Operacion>(sumaJson, options);

        if (operacion != null)
        {
            Console.WriteLine($"Resultado: {operacion.Ejecutar()}");
        }
        else
        {
            Console.WriteLine("Error al deserializar la operación.");
        }
    }
}
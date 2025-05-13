// Modelos comunes
public abstract class Operacion
{
    public abstract int Ejecutar();
}

public class OperacionSuma : Operacion
{
    public int A { get; set; }
    public int B { get; set; }
    public override int Ejecutar() => A + B;
}

public class OperacionResta : Operacion
{
    public int A { get; set; }
    public int B { get; set; }
    public override int Ejecutar() => A - B;
}
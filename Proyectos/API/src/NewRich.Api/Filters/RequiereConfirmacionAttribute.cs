namespace NewRich.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequiereConfirmacionAttribute : Attribute
{
    public RequiereConfirmacionAttribute(params string[] acciones)
    {
        Acciones = acciones;
    }

    public IReadOnlyList<string> Acciones { get; }
}

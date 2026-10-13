namespace PlanningApp.Application.Common.Exceptions;

// Kayıt yok ya da başka kullanıcıya ait; ikisi de aynı şekilde "bulunamadı" sayılır (kaydın varlığı sızdırılmaz).
// WebApi'deki global middleware tarafından 404'e çevrilir.
public sealed class NotFoundException : Exception
{
    public string EntityName { get; }
    public object Key { get; }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} bulunamadı: {key}")
    {
        EntityName = entityName;
        Key = key;
    }
}

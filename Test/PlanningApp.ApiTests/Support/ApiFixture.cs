using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PlanningApp.Persistence;
using Testcontainers.PostgreSql;

namespace PlanningApp.ApiTests.Support;

// Tüm API testleri için tek paylaşılan PostgreSQL container'ı ve varsayılan uygulama örneği.
// Şema migration'lardan kurulur; JWT imzalama anahtarı her çalıştırmada rastgele üretilir (repoda sabit anahtar YOK).
public sealed class ApiFixture : IAsyncLifetime
{
    public const string Issuer = "planning-app-api-tests";
    public const string Audience = "planning-app-api-tests-clients";

    // Varsayılan uygulamada hız sınırı testleri etkilemesin diye yüksektir; hız sınırı testi kendi factory'sini kullanır
    private const int DefaultPermitLimit = 100_000;

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();
    private readonly List<PlanningApiFactory> _factories = [];

    public string SigningKey { get; } = NewSigningKey();

    // Testlerin ortak kullandığı uygulama örneği
    public PlanningApiFactory Default { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // EnsureCreated değil: migration'ların kendisi de kullanılır
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();

        Default = CreateFactory();
    }

    public async Task DisposeAsync()
    {
        foreach (var factory in _factories)
            await factory.DisposeAsync();

        await _container.DisposeAsync();
    }

    // Aynı container'a bağlı yeni bir uygulama örneği; ayarlar testin ihtiyacına göre değiştirilir
    public PlanningApiFactory CreateFactory(
        int permitLimit = DefaultPermitLimit,
        string? signingKey = null,
        bool includeSigningKey = true)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:PlanningDb"] = _container.GetConnectionString(),
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["RateLimiting:Auth:PermitLimit"] = permitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        if (includeSigningKey)
            settings["Jwt:SigningKey"] = signingKey ?? SigningKey;

        var factory = new PlanningApiFactory(settings);
        _factories.Add(factory);
        return factory;
    }

    // Doğrudan veritabanı erişimi gereken doğrulamalar için (tracking etkisini dışlamak üzere her çağrı yeni örnek)
    public PlanningDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new PlanningDbContext(options);
    }

    // 32 baytlık rastgele anahtar, base64
    public static string NewSigningKey(int byteLength = 32)
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength));
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}

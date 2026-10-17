using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PlanningApp.ApiTests.Support;

// Gerçek uygulamayı "Testing" ortamında ayağa kaldırır. Yapılandırma UseSetting ile verilir;
// bunun uygulamaya ulaştığı ApiConfigurationTests ile kanıtlanır.
public sealed class PlanningApiFactory(IReadOnlyDictionary<string, string?> settings)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        foreach (var (key, value) in settings)
            builder.UseSetting(key, value);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PlanningApp.Persistence;

// Yalnızca `dotnet ef` komutları için; migration üretimi veritabanına bağlanmaz.
// Buradaki bağlantı dizesi gerçek bir sır içermez.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PlanningDbContext>
{
    public PlanningDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseNpgsql("Host=localhost;Database=planning_design_time;Username=design_time;Password=design_time")
            .Options;

        return new PlanningDbContext(options);
    }
}

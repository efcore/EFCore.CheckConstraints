using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.TestUtilities;

public class AzureSqlTestHelpers : TestHelpers
{
    protected AzureSqlTestHelpers()
    {
    }

    public static AzureSqlTestHelpers Instance { get; } = new();

    public override IServiceCollection AddProviderServices(IServiceCollection services)
        => services.AddEntityFrameworkSqlServer();

    public override DbContextOptionsBuilder UseProviderOptions(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseAzureSql(new SqlConnection("Database=DummyDatabase"),
            providerOptions => { providerOptions.UseCompatibilityLevel(170); });
}

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.TestUtilities
{
    public class SqlServerTestHelpers : TestHelpers
    {
        protected SqlServerTestHelpers()
        {
        }

        public static SqlServerTestHelpers Instance { get; } = new();

        public override IServiceCollection AddProviderServices(IServiceCollection services)
            => services.AddEntityFrameworkSqlServer();

        public override void UseProviderOptions(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer(new SqlConnection("Database=DummyDatabase"));
    }
}

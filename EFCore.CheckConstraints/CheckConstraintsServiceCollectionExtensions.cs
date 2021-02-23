using EFCore.CheckConstraints.Internal;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    public static class NamingConventionsServiceCollectionExtensions
    {
        /// <summary>
        /// <para>
        /// Adds the services required for automatically applying certain check constraints in Entity Framework Core.
        /// You use this method when using dependency injection in your application, such as with ASP.NET.
        /// For more information on setting up dependency injection, see http://go.microsoft.com/fwlink/?LinkId=526890.
        /// </para>
        /// <para>
        /// You only need to use this functionality when you want Entity Framework to resolve the services it uses
        /// from an external dependency injection container. If you are not using an external
        /// dependency injection container, Entity Framework will take care of creating the services it requires.
        /// </para>
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>
        /// The same service collection so that multiple calls can be chained.
        /// </returns>
        public static IServiceCollection AddEntityFrameworkCheckConstraints(
            [NotNull] this IServiceCollection serviceCollection)
        {
            Check.NotNull(serviceCollection, nameof(serviceCollection));

            new EntityFrameworkServicesBuilder(serviceCollection)
                .TryAdd<IConventionSetPlugin, CheckConstraintsConventionSetPlugin>();

            return serviceCollection;
        }
    }
}

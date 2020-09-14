using System.Globalization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using JetBrains.Annotations;
using EFCore.CheckConstraints.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore
{
    public static class CheckConstraintsExtensions
    {
        public static DbContextOptionsBuilder UseEnumCheckConstraints(
            [NotNull] this DbContextOptionsBuilder optionsBuilder)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var extension = (optionsBuilder.Options.FindExtension<CheckConstraintsOptionsExtension>() ?? new CheckConstraintsOptionsExtension())
                .WithEnumCheckConstraintsEnabled(enumCheckConstraintsEnabled: true);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder UseDiscriminatorCheckConstraints(
            [NotNull] this DbContextOptionsBuilder optionsBuilder)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var extension = (optionsBuilder.Options.FindExtension<CheckConstraintsOptionsExtension>() ?? new CheckConstraintsOptionsExtension())
                .WithDiscriminatorCheckConstraintsEnabled(discriminatorCheckConstraintsEnabled: true);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder UseAllCheckConstraints(
            [NotNull] this DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseEnumCheckConstraints()
                .UseDiscriminatorCheckConstraints();

        public static DbContextOptionsBuilder<TContext> UseEnumCheckConstraints<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseEnumCheckConstraints((DbContextOptionsBuilder)optionsBuilder);

        public static DbContextOptionsBuilder<TContext> UseDiscriminatorCheckConstraints<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseDiscriminatorCheckConstraints((DbContextOptionsBuilder)optionsBuilder);
    }
}

using System;
using EFCore.CheckConstraints.Internal;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

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

        public static DbContextOptionsBuilder UseValidationCheckConstraints(
            [NotNull] this DbContextOptionsBuilder optionsBuilder,
            [CanBeNull] Action<ValidationCheckConstraintOptionsBuilder> validationCheckConstraintsOptionsAction = null)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var validationCheckConstraintsOptionsBuilder = new ValidationCheckConstraintOptionsBuilder();
            validationCheckConstraintsOptionsAction?.Invoke(validationCheckConstraintsOptionsBuilder);

            var extension = (optionsBuilder.Options.FindExtension<CheckConstraintsOptionsExtension>() ?? new CheckConstraintsOptionsExtension())
                .WithValidationCheckConstraintsOptions(validationCheckConstraintsOptionsBuilder.Options);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder UseAllCheckConstraints(
            [NotNull] this DbContextOptionsBuilder optionsBuilder,
            [CanBeNull] Action<ValidationCheckConstraintOptionsBuilder> validationCheckConstraintsOptionsAction = null)
            => optionsBuilder
                .UseEnumCheckConstraints()
                .UseDiscriminatorCheckConstraints()
                .UseValidationCheckConstraints(validationCheckConstraintsOptionsAction);

        public static DbContextOptionsBuilder<TContext> UseEnumCheckConstraints<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseEnumCheckConstraints((DbContextOptionsBuilder)optionsBuilder);

        public static DbContextOptionsBuilder<TContext> UseDiscriminatorCheckConstraints<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseDiscriminatorCheckConstraints((DbContextOptionsBuilder)optionsBuilder);

        public static DbContextOptionsBuilder<TContext> UseValidationCheckConstraints<TContext>(
            [NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder,
            [CanBeNull] Action<ValidationCheckConstraintOptionsBuilder> validationCheckConstraintsOptionsAction = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseValidationCheckConstraints((DbContextOptionsBuilder)optionsBuilder, validationCheckConstraintsOptionsAction);
    }
}

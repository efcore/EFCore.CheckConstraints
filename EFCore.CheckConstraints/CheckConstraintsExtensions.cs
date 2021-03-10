using System;
using EFCore.CheckConstraints;
using EFCore.CheckConstraints.Internal;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     Extension methods for adding check constraints to database tables.
    /// </summary>
    public static class CheckConstraintsExtensions
    {
        /// <summary>
        ///     Adds check constraints which enforce that columns mapped to .NET enums only contain values valid for those enums.
        /// </summary>
        /// <param name="optionsBuilder"> The builder being used to configure the context. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public static DbContextOptionsBuilder UseEnumCheckConstraints(
            [NotNull] this DbContextOptionsBuilder optionsBuilder)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var extension = (optionsBuilder.Options.FindExtension<CheckConstraintsOptionsExtension>() ?? new CheckConstraintsOptionsExtension())
                .WithEnumCheckConstraintsEnabled(enumCheckConstraintsEnabled: true);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        /// <summary>
        ///     Adds check constraints which enforce that discriminator columns in a type-per-hierarchy inheritance mapping only contains
        ///     valid discriminator values.
        /// </summary>
        /// <param name="optionsBuilder"> The builder being used to configure the context. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public static DbContextOptionsBuilder UseDiscriminatorCheckConstraints(
            [NotNull] this DbContextOptionsBuilder optionsBuilder)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var extension = (optionsBuilder.Options.FindExtension<CheckConstraintsOptionsExtension>() ?? new CheckConstraintsOptionsExtension())
                .WithDiscriminatorCheckConstraintsEnabled(discriminatorCheckConstraintsEnabled: true);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        /// <summary>
        ///     Adds check constraints which enforce various .NET standard validation attributes.
        /// </summary>
        /// <param name="optionsBuilder"> The builder being used to configure the context. </param>
        /// <param name="validationCheckConstraintsOptionsAction">
        ///     An optional action to configure the validation check constraints.
        /// </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
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

        /// <summary>
        ///     Adds all check constraints supported by this plugin.
        /// </summary>
        /// <param name="optionsBuilder"> The builder being used to configure the context. </param>
        /// <param name="validationCheckConstraintsOptionsAction">
        ///     An optional action to configure the validation check constraints.
        /// </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public static DbContextOptionsBuilder UseAllCheckConstraints(
            [NotNull] this DbContextOptionsBuilder optionsBuilder,
            [CanBeNull] Action<ValidationCheckConstraintOptionsBuilder> validationCheckConstraintsOptionsAction = null)
            => optionsBuilder
                .UseEnumCheckConstraints()
                .UseDiscriminatorCheckConstraints()
                .UseValidationCheckConstraints(validationCheckConstraintsOptionsAction);

        /// <summary>
        ///     Adds check constraints which enforce that columns mapped to .NET enums only contain values valid for those enums.
        /// </summary>
        /// <param name="optionsBuilder"> The builder being used to configure the context. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public static DbContextOptionsBuilder<TContext> UseEnumCheckConstraints<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseEnumCheckConstraints((DbContextOptionsBuilder)optionsBuilder);

        /// <summary>
        ///     Adds check constraints which enforce that discriminator columns in a type-per-hierarchy inheritance mapping only contains
        ///     valid discriminator values.
        /// </summary>
        /// <param name="optionsBuilder"> The builder being used to configure the context. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public static DbContextOptionsBuilder<TContext> UseDiscriminatorCheckConstraints<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseDiscriminatorCheckConstraints((DbContextOptionsBuilder)optionsBuilder);

        /// <summary>
        ///     Adds check constraints which enforce various .NET standard validation attributes.
        /// </summary>
        /// <param name="optionsBuilder"> The builder being used to configure the context. </param>
        /// <param name="validationCheckConstraintsOptionsAction">
        ///     Configures validation check constraint creation.
        /// </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public static DbContextOptionsBuilder<TContext> UseValidationCheckConstraints<TContext>(
            [NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder,
            [CanBeNull] Action<ValidationCheckConstraintOptionsBuilder> validationCheckConstraintsOptionsAction = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseValidationCheckConstraints((DbContextOptionsBuilder)optionsBuilder, validationCheckConstraintsOptionsAction);

        /// <summary>
        ///     Adds all check constraints supported by this plugin.
        /// </summary>
        /// <param name="optionsBuilder"> The builder being used to configure the context. </param>
        /// <param name="validationCheckConstraintsOptionsAction">
        ///     An optional action to configure the validation check constraints.
        /// </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public static DbContextOptionsBuilder<TContext> UseAllCheckConstraints<TContext>(
            [NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder,
            [CanBeNull] Action<ValidationCheckConstraintOptionsBuilder> validationCheckConstraintsOptionsAction = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseAllCheckConstraints((DbContextOptionsBuilder)optionsBuilder, validationCheckConstraintsOptionsAction);
    }
}

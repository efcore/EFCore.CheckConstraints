using System;
using EFCore.CheckConstraints.Internal;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     Extension methods for adding column check constraints to database tables.
    /// </summary>
    public static class CheckConstraintsExtensions
    {
        /// <summary>
        ///     Adds or updates a <see cref="CheckConstraintsOptionsExtension"/>
        ///     object with enum check constraints enabled
        ///     to the current <see cref="DbContextOptionsBuilder"/> object.
        /// </summary>
        /// <param name="optionsBuilder">
        ///     <see cref="DbContextOptionsBuilder"/> object having <see cref="CheckConstraintsOptionsExtension"/>
        ///     added or updated with enum check constraints enabled.
        /// </param>
        /// <returns>
        ///     The current <see cref="DbContextOptionsBuilder"/> object.
        /// </returns>
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
        ///     Adds or updates a <see cref="CheckConstraintsOptionsExtension"/>
        ///     object with discriminator check constraints enabled
        ///     to the current <see cref="DbContextOptionsBuilder"/> object.
        /// </summary>
        /// <param name="optionsBuilder">
        ///     <see cref="DbContextOptionsBuilder"/> object having <see cref="CheckConstraintsOptionsExtension"/>
        ///     added or updated with discriminator check constraints enabled.
        /// </param>
        /// <returns>
        ///     The current <see cref="DbContextOptionsBuilder"/> object.
        /// </returns>
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
        ///     Adds or updates a <see cref="CheckConstraintsOptionsExtension"/>
        ///     object with validation check constraints enabled
        ///     to the current <see cref="DbContextOptionsBuilder"/> object.
        /// </summary>
        /// <param name="optionsBuilder">
        ///     <see cref="DbContextOptionsBuilder"/> object having <see cref="CheckConstraintsOptionsExtension"/>
        ///     added or updated with validation check constraints enabled.
        /// </param>
        /// <param name="validationCheckConstraintsOptionsAction">
        ///     Configures validation check constraint creation.
        /// </param>
        /// <returns>
        ///     The current <see cref="DbContextOptionsBuilder"/> object.
        /// </returns>
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
        ///     Adds or updates a <see cref="CheckConstraintsOptionsExtension"/>
        ///     object with all kinds of check constraints enabled
        ///     to the current <see cref="DbContextOptionsBuilder"/> object.
        /// </summary>
        /// <param name="optionsBuilder">
        ///     <see cref="DbContextOptionsBuilder"/> object having <see cref="CheckConstraintsOptionsExtension"/>
        ///     added or updated with all kinds of check constraints enabled.
        /// </param>
        /// <param name="validationCheckConstraintsOptionsAction">
        ///     Configures validation check constraint creation.
        /// </param>
        /// <returns>
        ///     The current <see cref="DbContextOptionsBuilder"/> object.
        /// </returns>
        public static DbContextOptionsBuilder UseAllCheckConstraints(
            [NotNull] this DbContextOptionsBuilder optionsBuilder,
            [CanBeNull] Action<ValidationCheckConstraintOptionsBuilder> validationCheckConstraintsOptionsAction = null)
            => optionsBuilder
                .UseEnumCheckConstraints()
                .UseDiscriminatorCheckConstraints()
                .UseValidationCheckConstraints(validationCheckConstraintsOptionsAction);



        /// <summary>
        ///     Adds or updates a <see cref="CheckConstraintsOptionsExtension"/>
        ///     object with enum check constraints enabled
        ///     to the current <see cref="DbContextOptionsBuilder{TContext}"/> object.
        /// </summary>
        /// <typeparam name="TContext">
        ///     Entity Framework Core database context type.
        /// </typeparam>
        /// <param name="optionsBuilder">
        ///     <see cref="DbContextOptionsBuilder{TContext}"/> object having <see cref="CheckConstraintsOptionsExtension"/>
        ///     added or updated with enum check constraints enabled.
        /// </param>
        /// <returns>
        ///     The current <see cref="DbContextOptionsBuilder{TContext}"/> object.
        /// </returns>
        public static DbContextOptionsBuilder<TContext> UseEnumCheckConstraints<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseEnumCheckConstraints((DbContextOptionsBuilder)optionsBuilder);

        /// <summary>
        ///     Adds or updates a <see cref="CheckConstraintsOptionsExtension"/>
        ///     object with discriminator check constraints enabled
        ///     to the current <see cref="DbContextOptionsBuilder{TContext}"/> object.
        /// </summary>
        /// <typeparam name="TContext">
        ///     Entity Framework Core database context type.
        /// </typeparam>
        /// <param name="optionsBuilder">
        ///     <see cref="DbContextOptionsBuilder{TContext}"/> object having <see cref="CheckConstraintsOptionsExtension"/>
        ///     added or updated with discriminator check constraints enabled.
        /// </param>
        /// <returns>
        ///     The current <see cref="DbContextOptionsBuilder{TContext}"/> object.
        /// </returns>
        public static DbContextOptionsBuilder<TContext> UseDiscriminatorCheckConstraints<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseDiscriminatorCheckConstraints((DbContextOptionsBuilder)optionsBuilder);

        /// <summary>
        ///     Adds or updates a <see cref="CheckConstraintsOptionsExtension"/>
        ///     object with validation check constraints enabled
        ///     to the current <see cref="DbContextOptionsBuilder{TContext}"/> object.
        /// </summary>
        /// <typeparam name="TContext">
        ///     Entity Framework Core database context type.
        /// </typeparam>
        /// <param name="optionsBuilder">
        ///     <see cref="DbContextOptionsBuilder{TContext}"/> object having <see cref="CheckConstraintsOptionsExtension"/>
        ///     added or updated with validation check constraints enabled.
        /// </param>
        /// <param name="validationCheckConstraintsOptionsAction">
        ///     Configures validation check constraint creation.
        /// </param>
        /// <returns>
        ///     The current <see cref="DbContextOptionsBuilder{TContext}"/> object.
        /// </returns>
        public static DbContextOptionsBuilder<TContext> UseValidationCheckConstraints<TContext>(
            [NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder,
            [CanBeNull] Action<ValidationCheckConstraintOptionsBuilder> validationCheckConstraintsOptionsAction = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseValidationCheckConstraints((DbContextOptionsBuilder)optionsBuilder, validationCheckConstraintsOptionsAction);

        /// <summary>
        ///     Adds or updates a <see cref="CheckConstraintsOptionsExtension"/>
        ///     object with all check constraints enabled
        ///     to the current <see cref="DbContextOptionsBuilder{TContext}"/> object.
        /// </summary>
        /// <typeparam name="TContext">
        ///     Entity Framework Core database context type.
        /// </typeparam>
        /// <param name="optionsBuilder">
        ///     <see cref="DbContextOptionsBuilder{TContext}"/> object having <see cref="CheckConstraintsOptionsExtension"/>
        ///     added or updated with all check constraints enabled.
        /// </param>
        /// <param name="validationCheckConstraintsOptionsAction">
        ///     Configures validation check constraint creation.
        /// </param>
        /// <returns>
        ///     The current <see cref="DbContextOptionsBuilder{TContext}"/> object.
        /// </returns>
        public static DbContextOptionsBuilder<TContext> UseAllCheckConstraints<TContext>(
            [NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder,
            [CanBeNull] Action<ValidationCheckConstraintOptionsBuilder> validationCheckConstraintsOptionsAction = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseAllCheckConstraints((DbContextOptionsBuilder)optionsBuilder, validationCheckConstraintsOptionsAction);
    }
}

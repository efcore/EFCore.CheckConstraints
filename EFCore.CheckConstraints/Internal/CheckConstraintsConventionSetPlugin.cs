using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.CheckConstraints.Internal
{
    /// <summary>
    ///     Customizes the <see cref="ConventionSet"/> being used by adding
    ///     database table column check constraints according to data annotations.
    /// </summary>
    public class CheckConstraintsConventionSetPlugin : IConventionSetPlugin
    {
        private readonly IDbContextOptions _options;
        private readonly ISqlGenerationHelper _sqlGenerationHelper;
        private readonly IRelationalTypeMappingSource _relationalTypeMappingSource;
        private readonly IDatabaseProvider _databaseProvider;

        /// <summary>
        ///     Creates a new <see cref="CheckConstraintsConventionSetPlugin"/> object.
        /// </summary>
        /// <param name="options">
        ///     Collection of database context option extensions.
        /// </param>
        /// <param name="sqlGenerationHelper">
        ///     Service to help with generation of SQL commands.
        /// </param>
        /// <param name="relationalTypeMappingSource">
        ///     Relational type mapping interface for EF Core.
        /// </param>
        /// <param name="databaseProvider">
        ///     The current database provider.
        /// </param>
        public CheckConstraintsConventionSetPlugin(
            [NotNull] IDbContextOptions options,
            [NotNull] ISqlGenerationHelper sqlGenerationHelper,
            [NotNull] IRelationalTypeMappingSource relationalTypeMappingSource,
            [NotNull] IDatabaseProvider databaseProvider)
        {
            _options = options;
            _sqlGenerationHelper = sqlGenerationHelper;
            _relationalTypeMappingSource = relationalTypeMappingSource;
            _databaseProvider = databaseProvider;
        }

        /// <summary>
        ///     Modifies the given convention set by adding table
        ///     column check constraint conventions.
        /// </summary>
        /// <param name="conventionSet">
        ///     The <see cref="ConventionSet"/> object to modify.
        /// </param>
        /// <returns>
        ///     The modified <see cref="ConventionSet"/> object.
        /// </returns>
        public ConventionSet ModifyConventions(ConventionSet conventionSet)
        {
            var extension = _options.FindExtension<CheckConstraintsOptionsExtension>();

            if (extension.AreEnumCheckConstraintsEnabled)
            {
                conventionSet.ModelFinalizingConventions.Add(
                    new EnumCheckConstraintConvention(_sqlGenerationHelper));
            }

            if (extension.AreDiscriminatorCheckConstraintsEnabled)
            {
                conventionSet.ModelFinalizingConventions.Add(
                    new DiscriminatorCheckConstraintConvention(_sqlGenerationHelper));
            }

            if (extension.AreValidationCheckConstraintsEnabled)
            {
                conventionSet.ModelFinalizingConventions.Add(
                    new ValidationCheckConstraintConvention(
                        extension.ValidationCheckConstraintOptions, _sqlGenerationHelper, _relationalTypeMappingSource, _databaseProvider));
            }

            return conventionSet;
        }
    }
}

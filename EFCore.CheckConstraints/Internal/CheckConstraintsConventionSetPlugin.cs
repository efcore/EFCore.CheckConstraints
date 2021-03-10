using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.CheckConstraints.Internal
{
    public class CheckConstraintsConventionSetPlugin : IConventionSetPlugin
    {
        private readonly IDbContextOptions _options;
        private readonly ISqlGenerationHelper _sqlGenerationHelper;
        private readonly IRelationalTypeMappingSource _relationalTypeMappingSource;
        private readonly IDatabaseProvider _databaseProvider;

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

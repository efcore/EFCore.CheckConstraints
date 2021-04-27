using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.CheckConstraints.Internal
{
    public class CheckConstraintsConventionSetPlugin : IConventionSetPlugin
    {
        private readonly IDbContextOptions _options;
        private readonly IRelationalTypeMappingSource _typeMappingSource;
        private readonly ISqlGenerationHelper _sqlGenerationHelper;
        private readonly IRelationalTypeMappingSource _relationalTypeMappingSource;
        private readonly IDatabaseProvider _databaseProvider;

        public CheckConstraintsConventionSetPlugin(
            IDbContextOptions options,
            IRelationalTypeMappingSource typeMappingSource,
            ISqlGenerationHelper sqlGenerationHelper,
            IRelationalTypeMappingSource relationalTypeMappingSource,
            IDatabaseProvider databaseProvider)
        {
            _options = options;
            _typeMappingSource = typeMappingSource;
            _sqlGenerationHelper = sqlGenerationHelper;
            _relationalTypeMappingSource = relationalTypeMappingSource;
            _databaseProvider = databaseProvider;
        }

        public ConventionSet ModifyConventions(ConventionSet conventionSet)
        {
            var extension = _options.FindExtension<CheckConstraintsOptionsExtension>();

            if (extension is not null)
            {
                if (extension.AreEnumCheckConstraintsEnabled)
                {
                    conventionSet.ModelFinalizingConventions.Add(
                        new EnumCheckConstraintConvention(_typeMappingSource, _sqlGenerationHelper));
                }

                if (extension.AreDiscriminatorCheckConstraintsEnabled)
                {
                    conventionSet.ModelFinalizingConventions.Add(
                        new DiscriminatorCheckConstraintConvention(_typeMappingSource, _sqlGenerationHelper));
                }

                if (extension.AreValidationCheckConstraintsEnabled)
                {
                    conventionSet.ModelFinalizingConventions.Add(
                        new ValidationCheckConstraintConvention(
                            extension.ValidationCheckConstraintOptions!, _typeMappingSource, _sqlGenerationHelper,
                            _relationalTypeMappingSource, _databaseProvider));
                }
            }

            return conventionSet;
        }
    }
}

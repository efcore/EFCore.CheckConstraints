using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.CheckConstraints.Internal
{
    public class CheckConstraintsConventionSetPlugin : IConventionSetPlugin
    {
        readonly IDbContextOptions _options;
        readonly ISqlGenerationHelper _sqlGenerationHelper;

        public CheckConstraintsConventionSetPlugin(
            [NotNull] IDbContextOptions options,
            ISqlGenerationHelper sqlGenerationHelper)
        {
            _options = options;
            _sqlGenerationHelper = sqlGenerationHelper;
        }

        public ConventionSet ModifyConventions(ConventionSet conventionSet)
        {
            var extension = _options.FindExtension<CheckConstraintsOptionsExtension>();

            if (extension.AreEnumCheckConstraintsEnabled)
            {
                conventionSet.ModelFinalizingConventions.Add(new EnumCheckConstraintConvention(_sqlGenerationHelper));
            }

            if (extension.AreDiscriminatorCheckConstraintsEnabled)
            {
                conventionSet.ModelFinalizingConventions.Add(new DiscriminatorCheckConstraintConvention(_sqlGenerationHelper));
            }

            return conventionSet;
        }
    }
}

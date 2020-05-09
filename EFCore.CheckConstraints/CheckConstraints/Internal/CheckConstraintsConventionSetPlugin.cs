using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using JetBrains.Annotations;

namespace EFCore.CheckConstraints.Internal
{
    public class CheckConstraintsConventionSetPlugin : IConventionSetPlugin
    {
        readonly IDbContextOptions _options;
        public CheckConstraintsConventionSetPlugin([NotNull] IDbContextOptions options) => _options = options;

        public ConventionSet ModifyConventions(ConventionSet conventionSet)
        {
            var extension = _options.FindExtension<CheckConstraintsOptionsExtension>();

            if (extension.AreEnumCheckConstraintsEnabled)
            {
                conventionSet.ModelFinalizingConventions.Add(new EnumCheckConstraintConvention());
            }

            return conventionSet;
        }
    }
}

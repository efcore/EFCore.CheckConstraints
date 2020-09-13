using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.CheckConstraints.Internal
{
    /// <summary>
    ///     A convention that creates check constraints ensuring that (complete) discriminator columns only have
    ///     expected values.
    /// </summary>
    public class DiscriminatorCheckConstraintConvention : IModelFinalizingConvention
    {
        readonly ISqlGenerationHelper _sqlGenerationHelper;

        public DiscriminatorCheckConstraintConvention(ISqlGenerationHelper sqlGenerationHelper)
            => _sqlGenerationHelper = sqlGenerationHelper;

        /// <inheritdoc />
        public virtual void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
        {
            var sql = new StringBuilder();

            foreach (var (rootEntityType, discriminatorValues) in modelBuilder.Metadata
                .GetEntityTypes()
                .GroupBy(e => e.GetRootType())
                .Where(g => g.Key.GetDiscriminatorProperty() != null && g.Key.GetIsDiscriminatorMappingComplete())
                .Select(g => (g.Key, g.Select(e => e.GetDiscriminatorValue()))))
            {
                var discriminatorProperty = rootEntityType.GetDiscriminatorProperty();
                sql.Clear();

                sql.Append(_sqlGenerationHelper.DelimitIdentifier(discriminatorProperty.GetColumnName()));
                sql.Append(" IN (");
                foreach (var discriminatorValue in discriminatorValues)
                {
                    var value = ((RelationalTypeMapping)discriminatorProperty.FindTypeMapping())
                        .GenerateSqlLiteral(discriminatorValue);
                    sql.Append($"{value}, ");
                }

                sql.Remove(sql.Length - 2, 2);
                sql.Append(")");

                var constraintName = $"CK_{rootEntityType.GetTableName()}_Discriminator_Constraint";
                rootEntityType.AddCheckConstraint(constraintName, sql.ToString());
            }
        }
    }
}

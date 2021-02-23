using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
        private readonly ISqlGenerationHelper _sqlGenerationHelper;

        /// <summary>
        ///     Creates a new <see cref="DiscriminatorCheckConstraintConvention"/> object.
        /// </summary>
        /// <param name="sqlGenerationHelper">
        ///     Service to help with generation of SQL commands.
        /// </param>
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
                if (!(StoreObjectIdentifier.Create(rootEntityType, StoreObjectType.Table) is StoreObjectIdentifier tableIdentifier))
                {
                    continue;
                }

                var discriminatorProperty = rootEntityType.GetDiscriminatorProperty();

                if (!(discriminatorProperty.FindTypeMapping() is RelationalTypeMapping typeMapping)
                    || !(discriminatorProperty.GetColumnName(tableIdentifier) is string))
                {
                    continue;
                }

                sql.Clear();

                sql.Append(_sqlGenerationHelper.DelimitIdentifier(discriminatorProperty.GetColumnName(tableIdentifier)));
                sql.Append(" IN (");
                foreach (var discriminatorValue in discriminatorValues.Where(v => v != null))
                {
                    var value = typeMapping.GenerateSqlLiteral(discriminatorValue);
                    sql.Append($"{value}, ");
                }

                sql.Remove(sql.Length - 2, 2);
                sql.Append(")");

                var constraintName = $"CK_{tableIdentifier.Name}_Discriminator";
                rootEntityType.AddCheckConstraint(constraintName, sql.ToString());
            }
        }
    }
}

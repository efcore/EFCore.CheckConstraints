using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.CheckConstraints.Internal;

public class DiscriminatorCheckConstraintConvention : IModelFinalizingConvention
{
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly ISqlGenerationHelper _sqlGenerationHelper;

    public DiscriminatorCheckConstraintConvention(
        IRelationalTypeMappingSource typeMappingSource,
        ISqlGenerationHelper sqlGenerationHelper)
    {
        _typeMappingSource = typeMappingSource;
        _sqlGenerationHelper = sqlGenerationHelper;
    }

    public virtual void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        var sql = new StringBuilder();

        foreach (var (rootEntityType, discriminatorValues) in modelBuilder.Metadata
                     .GetEntityTypes()
                     .GroupBy(e => e.GetRootType())
                     .Where(g => g.Key.FindDiscriminatorProperty() != null && g.Key.GetIsDiscriminatorMappingComplete())
                     .Select(g => (g.Key, g.Where(e => !e.IsAbstract()).Select(e => e.GetDiscriminatorValue()))))
        {
            if (!(StoreObjectIdentifier.Create(rootEntityType, StoreObjectType.Table) is { } tableIdentifier))
            {
                continue;
            }

            var discriminatorProperty = rootEntityType.FindDiscriminatorProperty()!;

            var typeMapping = (RelationalTypeMapping?)discriminatorProperty.FindTypeMapping()
                ?? _typeMappingSource.FindMapping((IProperty)discriminatorProperty);

            if (typeMapping is null
                || !(discriminatorProperty.GetColumnName(tableIdentifier) is { } columnName))
            {
                continue;
            }

            sql.Clear();

            sql.Append(_sqlGenerationHelper.DelimitIdentifier(columnName));
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

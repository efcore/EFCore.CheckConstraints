using System;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.CheckConstraints.Internal;

public class EnumCheckConstraintConvention : IModelFinalizingConvention
{
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly ISqlGenerationHelper _sqlGenerationHelper;

    public EnumCheckConstraintConvention(
        IRelationalTypeMappingSource typeMappingSource,
        ISqlGenerationHelper sqlGenerationHelper)
    {
        _typeMappingSource = typeMappingSource;
        _sqlGenerationHelper = sqlGenerationHelper;
    }

    public virtual void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        var sql = new StringBuilder();

        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is null)
            {
                continue;
            }

            var tableIdentifier = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());

            foreach (var property in entityType.GetDeclaredProperties())
            {
                var typeMapping = (RelationalTypeMapping?)property.FindTypeMapping()
                    ?? _typeMappingSource.FindMapping((IProperty)property);
                var propertyType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (propertyType.IsEnum
                    && typeMapping != null
                    && !propertyType.IsDefined(typeof(FlagsAttribute), true)
                    && property.GetColumnName(tableIdentifier) is { } columnName
                    // Skip enums mapped to enums in the database, assuming that the database enforces correctness and a check constraint
                    // isn't needed. This is the case for PostgreSQL native enums.
                    && !(typeMapping.Converter?.ProviderClrType ?? typeMapping.ClrType).IsEnum)
                {
                    var enumValues = Enum.GetValues(propertyType);
                    if (enumValues.Length <= 0)
                    {
                        continue;
                    }

                    sql.Clear();

                    sql.Append(_sqlGenerationHelper.DelimitIdentifier(columnName));
                    sql.Append(" IN (");
                    foreach (var item in enumValues)
                    {
                        var value = typeMapping.GenerateSqlLiteral(item);
                        sql.Append($"{value}, ");
                    }

                    sql.Remove(sql.Length - 2, 2);
                    sql.Append(")");

                    var constraintName = $"CK_{tableName}_{columnName}_Enum";
                    entityType.AddCheckConstraint(constraintName, sql.ToString());
                }
            }
        }
    }
}

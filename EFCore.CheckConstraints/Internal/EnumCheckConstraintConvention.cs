using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.CheckConstraints.Internal;

public class EnumCheckConstraintConvention : IModelFinalizingConvention
{
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly ISqlGenerationHelper _sqlGenerationHelper;

    private readonly List<Type> _knownTypes = new()
    {
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(byte),
        typeof(sbyte),
        typeof(short),
        typeof(ushort),
        typeof(decimal)
    };

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
                if (!propertyType.IsEnum
                    || typeMapping is null
                    || propertyType.IsDefined(typeof(FlagsAttribute), true)
                    || property.GetColumnName(tableIdentifier) is not { } columnName)
                {
                    continue;
                }

                var values = Enum.GetValues(propertyType);
                if (values.Length == 0)
                {
                    continue;
                }

                sql.Clear();

                sql.Append(_sqlGenerationHelper.DelimitIdentifier(columnName));

                if (TryParseContiguousRange(values, typeMapping, out var minValue, out var maxValue))
                {
                    sql.Append(" BETWEEN ");
                    sql.Append(minValue);
                    sql.Append(" AND ");
                    sql.Append(maxValue);
                }
                else
                {
                    sql.Append(" IN (");
                    foreach (var item in values.Cast<object>().Select(typeMapping.GenerateSqlLiteral))
                    {
                        sql.Append(item);
                        sql.Append(", ");
                    }

                    sql.Remove(sql.Length - 2, 2);
                    sql.Append(')');
                }

                var constraintName = $"CK_{tableName}_{columnName}_Enum";
                entityType.AddCheckConstraint(constraintName, sql.ToString());
            }
        }
    }

    private bool TryParseContiguousRange(IEnumerable values, CoreTypeMapping typeMapping, out decimal minValue, out decimal maxValue)
    {
        if (typeMapping.Converter?.ProviderClrType is null || !_knownTypes.Contains(typeMapping.Converter.ProviderClrType))
        {
            minValue = 0;
            maxValue = 0;

            return false;
        }

        // we convert using decimal because it is a wider number type than all of the valid enum backing types
        var enumValues = values.Cast<object>().Select(x => decimal.Truncate(Convert.ToDecimal(x))).ToList();

        minValue = enumValues.Min();
        maxValue = enumValues.Max();

        return maxValue - minValue == enumValues.Count - 1;
    }
}

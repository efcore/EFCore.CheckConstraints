using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
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

                if (TryParseContiguousRange(typeMapping, values, out var minValue, out var maxValue))
                {
                    sql.Append(" BETWEEN ");
                    sql.Append(minValue);
                    sql.Append(" AND ");
                    sql.Append(maxValue);
                }
                else
                {
                    sql.Append(" IN (");
                    foreach (var item in values)
                    {
                        sql.Append(typeMapping.GenerateSqlLiteral(item));
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

    private bool TryParseContiguousRange(CoreTypeMapping typeMapping, IEnumerable values, out object? minValue, out object? maxValue)
    {
        minValue = default;
        maxValue = default;

        if (typeMapping.Converter?.ProviderClrType is null || !_knownTypes.Contains(typeMapping.Converter.ProviderClrType))
        {
            return false;
        }

        var underlyingType = Enum.GetUnderlyingType(typeMapping.ClrType);

        var parameters = new[] { values, minValue, maxValue };

        // Use reflection to turn `TryGetMinMax` into a generic invocation using the underlying type of the num
        var success = GetType().GetTypeInfo().GetDeclaredMethod(nameof(TryGetMinMax))!.MakeGenericMethod(underlyingType)
            .Invoke(null, parameters)!;

        minValue = parameters[1];
        maxValue = parameters[2];

        return (bool)success;
    }

    private static bool TryGetMinMax<T>(IEnumerable values, out T minValue, out T maxValue)
        where T : IBinaryInteger<T>, new()
    {
        var enumValues = values.Cast<T>().ToList();

        minValue = enumValues.Min()!;
        maxValue = enumValues.Max()!;

        var enumValuesCount = (T)Convert.ChangeType(enumValues.Count, typeof(T));

        return (maxValue - minValue) == (enumValuesCount + T.One);
    }
}

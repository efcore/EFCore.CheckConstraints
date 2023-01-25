using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EFCore.CheckConstraints.Internal;

public class EnumCheckConstraintConvention : IModelFinalizingConvention
{
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly ISqlGenerationHelper _sqlGenerationHelper;
    private readonly Type _iNumberType = typeof(INumber<>);
    private readonly Dictionary<Type, MethodInfo> _cachedTryGetMinMaxMethodInfos = new();
    private readonly Dictionary<RelationalTypeMapping, string> _cachedConstraints = new();

    public EnumCheckConstraintConvention(IRelationalTypeMappingSource typeMappingSource, ISqlGenerationHelper sqlGenerationHelper)
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
                var typeMapping = (RelationalTypeMapping?)property.FindTypeMapping() ?? _typeMappingSource.FindMapping((IProperty)property);
                var propertyType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (!propertyType.IsEnum
                    || typeMapping is null
                    || propertyType.IsDefined(typeof(FlagsAttribute), true)
                    || property.GetColumnName(tableIdentifier) is not { } columnName)
                {
                    continue;
                }

                if (!_cachedConstraints.TryGetValue(typeMapping, out var constraintSql))
                {
                    if (!TryGenerateCheckConstraint(typeMapping, out constraintSql))
                    {
                        continue;
                    }

                    _cachedConstraints[typeMapping] = sql.ToString();
                }

                entityType.AddCheckConstraint(
                    $"CK_{tableName}_{columnName}_Enum",
                    sql
                        .Clear()
                        .Append(_sqlGenerationHelper.DelimitIdentifier(columnName))
                        .Append(constraintSql)
                        .ToString());
            }
        }
    }

    private bool TryGenerateCheckConstraint(RelationalTypeMapping typeMapping, [NotNullWhen(true)] out string? constraintSql)
    {
        Check.DebugAssert(typeMapping.ClrType.IsEnum, "mapping.ClrType.IsEnum");

        var enumValues = Enum.GetValuesAsUnderlyingType(typeMapping.ClrType).Cast<object>().Distinct().ToArray();
        if (enumValues.Length == 0)
        {
            constraintSql = null;
            return false;
        }

        var sql = new StringBuilder();

        if (enumValues.Length > 2
            && TryParseContiguousRange(typeMapping.Converter, enumValues, out var minValue, out var maxValue))
        {
            sql.Append(" BETWEEN ");
            sql.Append(typeMapping.GenerateSqlLiteral(minValue));
            sql.Append(" AND ");
            sql.Append(typeMapping.GenerateSqlLiteral(maxValue));
        }
        else
        {
            sql.Append(" IN (");
            foreach (var item in enumValues)
            {
                var value = typeMapping.GenerateSqlLiteral(item);
                sql.Append($"{value}, ");
            }

            sql.Remove(sql.Length - 2, 2);
            sql.Append(')');
        }

        constraintSql = sql.ToString();
        return true;
    }

    private bool TryParseContiguousRange(ValueConverter? converter, IEnumerable values, out object? minValue, out object? maxValue)
    {
        // if the database destination type is not a type that implements INumber<>, we cannot do numeric operations
        if (converter?.ProviderClrType.GetInterfaces()
                .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == _iNumberType) is not true)
        {
            minValue = default;
            maxValue = default;
            return false;
        }

        var underlyingType = Enum.GetUnderlyingType(converter.ModelClrType);

        // ReSharper disable once InvertIf
        if (!_cachedTryGetMinMaxMethodInfos.TryGetValue(underlyingType, out var getMinMaxMethod))
        {
            getMinMaxMethod = typeof(EnumCheckConstraintConvention).GetTypeInfo()
                .GetDeclaredMethod(nameof(TryGetMinMax))!
                .MakeGenericMethod(underlyingType);

            _cachedTryGetMinMaxMethodInfos.Add(underlyingType, getMinMaxMethod);
        }

        var parameters = new object?[] { values, null, null };
        var success = (bool)getMinMaxMethod.Invoke(null, parameters)!;

        minValue = success ? parameters[1] : default;
        maxValue = success ? parameters[2] : default;

        return success;
    }

    private static bool TryGetMinMax<T>(object[] values, out T minValue, out T maxValue)
        where T : INumber<T>
    {
        var enumValues = values.Cast<T>().ToArray();

        minValue = enumValues.Min()!;
        maxValue = enumValues.Max()!;

        var enumValuesCount = (T)Convert.ChangeType(enumValues.Length, typeof(T));

        return (maxValue - minValue) == (enumValuesCount - T.One);
    }
}

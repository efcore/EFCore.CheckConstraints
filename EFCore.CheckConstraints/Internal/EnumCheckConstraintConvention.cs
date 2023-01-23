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
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EFCore.CheckConstraints.Internal;

public class EnumCheckConstraintConvention : IModelFinalizingConvention
{
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly ISqlGenerationHelper _sqlGenerationHelper;
    private readonly IReadOnlyDictionary<Type, MethodInfo> _supportedEnumValueTypes;

    public EnumCheckConstraintConvention(IRelationalTypeMappingSource typeMappingSource, ISqlGenerationHelper sqlGenerationHelper)
    {
        _typeMappingSource = typeMappingSource;
        _sqlGenerationHelper = sqlGenerationHelper;

        var method = GetType().GetTypeInfo().GetDeclaredMethod(nameof(TryGetMinMax))!;

        _supportedEnumValueTypes = new Dictionary<Type, MethodInfo>
        {
            { typeof(int), method.MakeGenericMethod(typeof(int)) },
            { typeof(uint), method.MakeGenericMethod(typeof(uint)) },
            { typeof(long), method.MakeGenericMethod(typeof(long)) },
            { typeof(ulong), method.MakeGenericMethod(typeof(ulong)) },
            { typeof(byte), method.MakeGenericMethod(typeof(byte)) },
            { typeof(sbyte), method.MakeGenericMethod(typeof(sbyte)) },
            { typeof(short), method.MakeGenericMethod(typeof(short)) },
            { typeof(ushort), method.MakeGenericMethod(typeof(ushort)) },
            { typeof(decimal), method.MakeGenericMethod(typeof(decimal)) },
        }.AsReadOnly();
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

                var values = Enum.GetValues(propertyType);
                if (values.Length == 0)
                {
                    continue;
                }

                sql.Clear();

                sql.Append(_sqlGenerationHelper.DelimitIdentifier(columnName));

                if (TryParseContiguousRange(typeMapping.Converter, values, out var minValue, out var maxValue))
                {
                    sql.Append(" BETWEEN ");
                    sql.Append(typeMapping.GenerateSqlLiteral(minValue));
                    sql.Append(" AND ");
                    sql.Append(typeMapping.GenerateSqlLiteral(maxValue));
                }
                else
                {
                    sql.Append(" IN (");
                    foreach (var item in values)
                    {
                        var value = typeMapping.GenerateSqlLiteral(item);
                        sql.Append($"{value}, ");
                    }

                    sql.Remove(sql.Length - 2, 2);
                    sql.Append(')');
                }

                var constraintName = $"CK_{tableName}_{columnName}_Enum";
                entityType.AddCheckConstraint(constraintName, sql.ToString());
            }
        }
    }

    private bool TryParseContiguousRange(ValueConverter? converter, IEnumerable values, out object? minValue, out object? maxValue)
    {
        if (converter?.ProviderClrType is null || !_supportedEnumValueTypes.ContainsKey(converter.ProviderClrType))
        {
            minValue = default;
            maxValue = default;
            return false;
        }

        var underlyingType = Enum.GetUnderlyingType(converter.ModelClrType);

        var parameters = new object?[] { values, null, null };

        var success = (bool)_supportedEnumValueTypes[underlyingType].Invoke(null, parameters)!;

        minValue = success ? parameters[1] : default;
        maxValue = success ? parameters[2] : default;

        return success;
    }

    private static bool TryGetMinMax<T>(IEnumerable values, out T minValue, out T maxValue)
        where T : INumber<T>
    {
        var enumValues = values.Cast<T>().Distinct().ToList();

        minValue = enumValues.Min()!;
        maxValue = enumValues.Max()!;

        var enumValuesCount = (T)Convert.ChangeType(enumValues.Count, typeof(T));

        return (maxValue - minValue) == (enumValuesCount - T.One);
    }
}

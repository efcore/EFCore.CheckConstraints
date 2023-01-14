using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    private readonly Regex _castRegex = new("CAST\\((\\d+) AS (?:tinyint|smallint|bigint)\\)", RegexOptions.Compiled | RegexOptions.Singleline);
    private readonly Regex _decimalRegex = new("(\\d+).0", RegexOptions.Compiled | RegexOptions.Singleline);

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

                var enumValues = Enum.GetValues(propertyType).Cast<object>().Select(typeMapping.GenerateSqlLiteral).ToList();
                if (enumValues.Count == 0)
                {
                    continue;
                }

                sql.Clear();

                sql.Append(_sqlGenerationHelper.DelimitIdentifier(columnName));

                if (TryParseMinAndMax(enumValues, out var minValue, out var maxValue))
                {
                    sql.Append(" BETWEEN ");
                    sql.Append(minValue);
                    sql.Append(" AND ");
                    sql.Append(maxValue);
                }
                else
                {
                    sql.Append(" IN (");
                    foreach (var item in enumValues)
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

    // we use decimal explicitly here because decimal is a wider number type than all of the valid enum backing types
    private bool TryParseMinAndMax(IReadOnlyCollection<string> enumValues, out decimal? minValue, out decimal? maxValue)
    {
        var parsedEnumValues = enumValues.Select<string, decimal?>(
                x =>
                {
                    decimal? result = null;

                    if (decimal.TryParse(x, out var parsed))
                    {
                        result = parsed;
                    }
                    else if (_decimalRegex.Match(x) is { Success: true } decimalMatch)
                    {
                        result = decimal.Parse(decimalMatch.Groups[1].Value);
                    }
                    else if (_castRegex.Match(x) is { Success: true } castMatch)
                    {
                        result = decimal.Parse(castMatch.Groups[1].Value);
                    }

                    //decimal.Truncate is necessary because `ulong`s will include a fractional part
                    return result.HasValue ? decimal.Truncate(result.Value) : null;
                })
            .ToList();

        if (parsedEnumValues.Any(x => x is null))
        {
            minValue = null;
            maxValue = null;
            return false;
        }

        minValue = parsedEnumValues.Min();
        maxValue = parsedEnumValues.Max();

        return maxValue - minValue == enumValues.Count - 1;
    }
}

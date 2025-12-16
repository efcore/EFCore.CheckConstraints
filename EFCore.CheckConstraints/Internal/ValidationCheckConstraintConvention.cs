using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.CheckConstraints.Internal;

public class ValidationCheckConstraintConvention : IModelFinalizingConvention
{
    public const string DefaultPhoneRegex = """^[\d\s+-.()]*\d[\d\s+-.()]*((ext\.|ext|x)\s*\d+)?\s*$""";

    public const string DefaultCreditCardRegex = """^[\d- ]*$""";

    public const string DefaultEmailAddressRegex = """^[^@]+@[^@]+$""";

    public const string DefaultUrlAddressRegex = """^(http://|https://|ftp://)""";

    public const string SqlServerDatabaseProviderName = "Microsoft.EntityFrameworkCore.SqlServer";
    public const string SqliteDatabaseProviderName = "Microsoft.EntityFrameworkCore.Sqlite";
    public const string PostgreSqlDatabaseProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";
    public const string MySqlDatabaseProviderName = "Pomelo.EntityFrameworkCore.MySql";

    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly ISqlGenerationHelper _sqlGenerationHelper;
    private readonly IDatabaseProvider _databaseProvider;
    private readonly IDbContextOptions _dbContextOptions;
    private readonly RelationalTypeMapping? _intTypeMapping;

    private readonly bool _useRegex, _isSqlServerNativeMethod;
    private readonly string _phoneRegex, _creditCardRegex, _emailAddressRegex, _urlRegex;

    public ValidationCheckConstraintConvention(
        ValidationCheckConstraintOptions options,
        IRelationalTypeMappingSource typeMappingSource,
        ISqlGenerationHelper sqlGenerationHelper,
        IRelationalTypeMappingSource relationalTypeMappingSource,
        IDatabaseProvider databaseProvider,
        IDbContextOptions dbContextOptions)
    {
        _typeMappingSource = typeMappingSource;
        _sqlGenerationHelper = sqlGenerationHelper;
        _databaseProvider = databaseProvider;
        _dbContextOptions = dbContextOptions;
        _intTypeMapping = relationalTypeMappingSource.FindMapping(typeof(int))!;

        _useRegex = options.UseRegex && SupportsRegex;
        _phoneRegex = options.PhoneRegex ?? DefaultPhoneRegex;
        _creditCardRegex = options.CreditCardRegex ?? DefaultCreditCardRegex;
        _emailAddressRegex = options.EmailAddressRegex ?? DefaultEmailAddressRegex;
        _urlRegex = options.UrlRegex ?? DefaultUrlAddressRegex;

        var sqlServerOptionsExtension  = _dbContextOptions.Extensions
            .Where(o => o.GetType().Name == "SqlServerOptionsExtension")
            .FirstOrDefault();

        if (sqlServerOptionsExtension != null)
        {
            var engineType = sqlServerOptionsExtension.GetType()?.GetProperty("EngineType")?.GetValue(sqlServerOptionsExtension);

            if (engineType?.ToString() is "SqlServer" or "AzureSql")
            {
                if (sqlServerOptionsExtension.GetType()?.GetProperty($"{engineType}CompatibilityLevel")?.GetValue(sqlServerOptionsExtension) is >= 170)
                {
                    _isSqlServerNativeMethod = true;
                }
            }
        }
    }

    public virtual void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        var sql = new StringBuilder();

        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            var (tableName, schema) = (entityType.GetTableName(), entityType.GetSchema());
            if (tableName is null)
            {
                continue;
            }

            foreach (var property in entityType.GetContainedProperties().Where(p => p.PropertyInfo != null || p.FieldInfo != null))
            {
                var memberInfo = (MemberInfo?)property.PropertyInfo ?? property.FieldInfo;
                if (memberInfo is null)
                {
                    continue;
                }

                var columnName = property.GetColumnName(StoreObjectIdentifier.Table(tableName, schema));
                if (columnName is null)
                {
                    continue;
                }

                int? minLength = null;
                int? maxLength = null;

                foreach (var attribute in memberInfo.GetCustomAttributes())
                {
                    switch (attribute)
                    {
                        case RangeAttribute a:
                            AddRangeConstraint(property, memberInfo, a, tableName, columnName, sql);
                            continue;

                        case MinLengthAttribute a when _intTypeMapping is not null:
                            minLength = minLength is null ? a.Length : Math.Max(a.Length, minLength.Value);
                            continue;

                        case StringLengthAttribute a when _intTypeMapping is not null:
                            minLength = minLength is null ? a.MinimumLength : Math.Max(a.MinimumLength, minLength.Value);
                            continue;

                        case RequiredAttribute { AllowEmptyStrings: false }
                            when _intTypeMapping is not null && memberInfo.GetMemberType() == typeof(string):
                            minLength = minLength is null ? 1 : Math.Max(1, minLength.Value);
                            continue;

                        case LengthAttribute a when _intTypeMapping is not null:
                            // Note: The max length should be enforced by the column schema definition in EF,
                            // see https://github.com/dotnet/efcore/issues/30754. While that isn't done, we enforce it via the check
                            // constraint.
                            minLength = minLength is null ? a.MinimumLength : Math.Max(a.MinimumLength, minLength.Value);
                            maxLength = maxLength is null ? a.MaximumLength : Math.Min(a.MaximumLength, maxLength.Value);
                            continue;

                        case AllowedValuesAttribute a:
                            AddListOfValuesConstraint(property, memberInfo, tableName, columnName, sql, a.Values, negated: false);
                            continue;

                        case DeniedValuesAttribute a:
                            AddListOfValuesConstraint(property, memberInfo, tableName, columnName, sql, a.Values, negated: true);
                            continue;
                    }

                    if (_useRegex)
                    {
                        switch (attribute)
                        {
                            case PhoneAttribute:
                                property.DeclaringType.ContainingEntityType.AddCheckConstraint(
                                    $"CK_{tableName}_{columnName}_Phone",
                                    GenerateRegexSql(columnName, _phoneRegex));
                                continue;

                            case CreditCardAttribute:
                                property.DeclaringType.ContainingEntityType.AddCheckConstraint(
                                    $"CK_{tableName}_{columnName}_CreditCard",
                                    GenerateRegexSql(columnName, _creditCardRegex));
                                continue;

                            case EmailAddressAttribute:
                                property.DeclaringType.ContainingEntityType.AddCheckConstraint(
                                    $"CK_{tableName}_{columnName}_EmailAddress",
                                    GenerateRegexSql(columnName, _emailAddressRegex));
                                continue;

                            case UrlAttribute:
                                property.DeclaringType.ContainingEntityType.AddCheckConstraint(
                                    $"CK_{tableName}_{columnName}_Url",
                                    GenerateRegexSql(columnName, _urlRegex));
                                continue;

                            case RegularExpressionAttribute a:
                                property.DeclaringType.ContainingEntityType.AddCheckConstraint(
                                    $"CK_{tableName}_{columnName}_RegularExpression",
                                    GenerateRegexSql(columnName, a.Pattern));
                                continue;
                        }
                    }
                }

                if (minLength is null)
                {
                    continue;
                }

                if (maxLength is not null)
                {
                    if (minLength.Value > maxLength.Value)
                    {
                        throw new InvalidOperationException(
                            $"The minimum length ({minLength}) specified for [{tableName}].[{columnName}] exceeds the maximum allowable length ({maxLength})."
                        );
                    }

                    AddStringLengthConstraint(property, memberInfo, tableName, columnName, sql, minLength.Value, maxLength.Value);
                }
                else
                {
                    AddMinimumLengthConstraint(property, memberInfo, tableName, columnName, sql, minLength.Value);
                }
            }
        }
    }

    protected virtual void AddRangeConstraint(
        IConventionProperty property,
        MemberInfo memberInfo,
        RangeAttribute attribute,
        string tableName,
        string columnName,
        StringBuilder sql)
    {
        var typeMapping = (RelationalTypeMapping?)property.FindTypeMapping() ?? _typeMappingSource.FindMapping((IProperty)property);

        if (typeMapping is null
            || attribute.Minimum.GetType() != typeMapping.ClrType
            || attribute.Maximum.GetType() != typeMapping.ClrType)
        {
            return;
        }

        sql.Clear();

        switch (attribute)
        {
            case { Minimum: int.MinValue, MinimumIsExclusive: false }:
                sql
                    .Append(_sqlGenerationHelper.DelimitIdentifier(columnName))
                    .Append(attribute.MaximumIsExclusive ? " < " : " <= ")
                    .Append(typeMapping.GenerateSqlLiteral(attribute.Maximum));
                break;

            case { Maximum: int.MaxValue, MaximumIsExclusive: false }:
                sql
                    .Append(_sqlGenerationHelper.DelimitIdentifier(columnName))
                    .Append(attribute.MinimumIsExclusive ? " > " : " >= ")
                    .Append(typeMapping.GenerateSqlLiteral(attribute.Minimum));
                break;

            case { MinimumIsExclusive: false, MaximumIsExclusive: false }:
                sql
                    .Append(_sqlGenerationHelper.DelimitIdentifier(columnName))
                    .Append(" BETWEEN ")
                    .Append(typeMapping.GenerateSqlLiteral(attribute.Minimum))
                    .Append(" AND ")
                    .Append(typeMapping.GenerateSqlLiteral(attribute.Maximum));
                break;

            default:
                sql
                    .Append(_sqlGenerationHelper.DelimitIdentifier(columnName))
                    .Append(attribute.MinimumIsExclusive ? " > " : " >= ")
                    .Append(typeMapping.GenerateSqlLiteral(attribute.Minimum))
                    .Append(" AND ")
                    .Append(_sqlGenerationHelper.DelimitIdentifier(columnName))
                    .Append(attribute.MaximumIsExclusive ? " < " : " <= ")
                    .Append(typeMapping.GenerateSqlLiteral(attribute.Maximum));
                break;
        }

        var constraintName = $"CK_{tableName}_{columnName}_Range";
        property.DeclaringType.ContainingEntityType.AddCheckConstraint(constraintName, sql.ToString());
    }

    protected virtual void AddMinimumLengthConstraint(
        IConventionProperty property,
        MemberInfo memberInfo,
        string tableName,
        string columnName,
        StringBuilder sql,
        int minLength)
    {
        var lengthFunctionName = _databaseProvider.Name switch
        {
            SqlServerDatabaseProviderName => "LEN",
            SqliteDatabaseProviderName => "LENGTH",
            PostgreSqlDatabaseProviderName => "LENGTH",
            MySqlDatabaseProviderName => "LENGTH",
            _ => null
        };

        if (lengthFunctionName is null || _intTypeMapping is null)
        {
            return;
        }

        sql.Clear();

        sql
            .Append(lengthFunctionName)
            .Append('(')
            .Append(_sqlGenerationHelper.DelimitIdentifier(columnName))
            .Append(')')
            .Append(" >= ")
            .Append(_intTypeMapping.GenerateSqlLiteral(minLength));

        var constraintName = $"CK_{tableName}_{columnName}_MinLength";
        property.DeclaringType.ContainingEntityType.AddCheckConstraint(constraintName, sql.ToString());
    }

    protected virtual void AddStringLengthConstraint(
        IConventionProperty property,
        MemberInfo memberInfo,
        string tableName,
        string columnName,
        StringBuilder sql,
        int minLength,
        int maxLength)
    {
        var lengthFunctionName = _databaseProvider.Name switch
        {
            SqlServerDatabaseProviderName => "LEN",
            SqliteDatabaseProviderName => "LENGTH",
            PostgreSqlDatabaseProviderName => "LENGTH",
            MySqlDatabaseProviderName => "LENGTH",
            _ => null
        };

        if (lengthFunctionName is null || _intTypeMapping is null)
        {
            return;
        }

        sql.Clear();

        sql
            .Append(lengthFunctionName)
            .Append('(')
            .Append(_sqlGenerationHelper.DelimitIdentifier(columnName))
            .Append(')')
            .Append(" BETWEEN ")
            .Append(_intTypeMapping.GenerateSqlLiteral(minLength))
            .Append(" AND ")
            .Append(_intTypeMapping.GenerateSqlLiteral(maxLength));

        var constraintName = $"CK_{tableName}_{columnName}_MinMaxLength";
        property.DeclaringType.ContainingEntityType.AddCheckConstraint(constraintName, sql.ToString());
    }

    protected virtual void AddListOfValuesConstraint(
            IConventionProperty property,
            MemberInfo memberInfo,
            string tableName,
            string columnName,
            StringBuilder sql,
            object?[] values,
            bool negated)
    {
        var typeMapping = (RelationalTypeMapping?)property.FindTypeMapping() ?? _typeMappingSource.FindMapping((IProperty)property);

        if (typeMapping is null)
        {
            return;
        }

        sql.Clear();

        sql
            .Append(_sqlGenerationHelper.DelimitIdentifier(columnName))
            .Append(negated ? " NOT IN (" : " IN (");

        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (value is not null && value.GetType() != typeMapping.ClrType)
            {
                return;
            }

            if (i > 0)
            {
                sql.Append(", ");
            }

            sql.Append(typeMapping.GenerateSqlLiteral(value));
        }

        sql.Append(')');

        var constraintName = $"CK_{tableName}_{columnName}_{(negated ? "Denied" : "Allowed")}Values";
        property.DeclaringType.ContainingEntityType.AddCheckConstraint(constraintName, sql.ToString());
    }

    protected virtual string GenerateRegexSql(string columnName, [RegexPattern] string regex)
    {
        return string.Format(
            _databaseProvider.Name switch
            {
                // For SQL Server, requires setup:
                // https://www.red-gate.com/simple-talk/sql/t-sql-programming/tsql-regular-expression-workbench/
                SqlServerDatabaseProviderName => _isSqlServerNativeMethod
                    ? "REGEXP_LIKE ({0}, '{1}')"
                    : "dbo.RegexMatch('{1}', {0}) > 0",
                SqliteDatabaseProviderName => "{0} REGEXP '{1}'",
                PostgreSqlDatabaseProviderName => "{0} ~ '{1}'",
                MySqlDatabaseProviderName => "{0} REGEXP '{1}'",
                _ => throw new InvalidOperationException($"Provider {_databaseProvider.Name} doesn't support regular expressions")
            }, _sqlGenerationHelper.DelimitIdentifier(columnName), regex);
    }

    protected virtual bool SupportsRegex
        => _databaseProvider.Name switch
        {
            SqlServerDatabaseProviderName => true,
            SqliteDatabaseProviderName => true,
            PostgreSqlDatabaseProviderName => true,
            MySqlDatabaseProviderName => true,
            _ => false
        };
}

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.CheckConstraints.Internal
{
    /// <summary>
    ///     A convention that creates check constraints for various validation attributes.
    /// </summary>
    public class ValidationCheckConstraintConvention : IModelFinalizingConvention
    {
        /// <summary>
        ///     Default regular expression pattern string used for phone numbers.
        /// </summary>
        public const string DefaultPhoneRegex = @"^[\d\s+-.()]*\d[\d\s+-.()]*((ext\.|ext|x)\s*\d+)?\s*$";

        /// <summary>
        ///     Default regular expression pattern string used for credit card numbers.
        /// </summary>
        public const string DefaultCreditCardRegex = @"^[\d- ]*$";

        /// <summary>
        ///     Default regular expression pattern string used for e-mail addresses.
        /// </summary>
        public const string DefaultEmailAddressRegex = @"^[^@]+@[^@]+$";

        /// <summary>
        ///     Default regular expression pattern string used for URLs.
        /// </summary>
        public const string DefaultUrlAddressRegex = @"^(http://|https://|ftp://)";



        private readonly ISqlGenerationHelper _sqlGenerationHelper;
        private readonly IDatabaseProvider _databaseProvider;
        private readonly RelationalTypeMapping _intTypeMapping;

        private readonly bool _useRegex;
        private readonly string _phoneRegex, _creditCardRegex, _emailAddressRegex, _urlRegex;


        /// <summary>
        ///     Creates a new <see cref="ValidationCheckConstraintConvention"/> object.
        /// </summary>
        /// <param name="options">
        ///     Configures how validation check constraints will be created.
        /// </param>
        /// <param name="sqlGenerationHelper">
        ///     Service to help with generation of SQL commands.
        /// </param>
        /// <param name="relationalTypeMappingSource">
        ///     Relational type mapping interface for EF Core.
        /// </param>
        /// <param name="databaseProvider">
        ///     The current database provider.
        /// </param>
        public ValidationCheckConstraintConvention(
            ValidationCheckConstraintOptions options,
            ISqlGenerationHelper sqlGenerationHelper,
            IRelationalTypeMappingSource relationalTypeMappingSource,
            IDatabaseProvider databaseProvider)
        {
            _sqlGenerationHelper = sqlGenerationHelper;
            _databaseProvider = databaseProvider;
            _intTypeMapping = relationalTypeMappingSource.FindMapping(typeof(int));

            _useRegex = options.UseRegex && SupportsRegex;
            _phoneRegex = options.PhoneRegex ?? DefaultPhoneRegex;
            _creditCardRegex = options.CreditCardRegex ?? DefaultCreditCardRegex;
            _emailAddressRegex = options.EmailAddressRegex ?? DefaultEmailAddressRegex;
            _urlRegex = options.UrlRegex ?? DefaultUrlAddressRegex;
        }

        /// <inheritdoc />
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

                foreach (var property in entityType.GetDeclaredProperties().Where(p => p.PropertyInfo != null || p.FieldInfo != null))
                {
                    var memberInfo = (MemberInfo)property.PropertyInfo ?? property.FieldInfo;
                    if (memberInfo is null)
                    {
                        continue;
                    }

                    var columnName = property.GetColumnName(StoreObjectIdentifier.Table(tableName, schema));
                    if (columnName is null)
                    {
                        continue;
                    }

                    ProcessRange(property, memberInfo, tableName, columnName, sql);
                    ProcessMinLength(property, memberInfo, tableName, columnName, sql);
                    ProcessStringLengthMinimumLength(property, memberInfo, tableName, columnName, sql);

                    if (_useRegex)
                    {
                        ProcessPhoneNumber(property, memberInfo, tableName, columnName, sql);
                        ProcessCreditCard(property, memberInfo, tableName, columnName, sql);
                        ProcessEmailAddress(property, memberInfo, tableName, columnName, sql);
                        ProcessUrl(property, memberInfo, tableName, columnName, sql);
                        ProcessRegularExpression(property, memberInfo, tableName, columnName, sql);
                    }
                }
            }
        }



        /// <summary>
        ///     Creates SQL check constraint clause for an entity property's
        ///     <see cref="RangeAttribute"/> data annotation.
        /// </summary>
        /// <param name="property">
        ///     Property to be examined.
        /// </param>
        /// <param name="memberInfo">
        ///     <see cref="MemberInfo"/> of property to be examined.
        /// </param>
        /// <param name="tableName">
        ///     Database table name.
        /// </param>
        /// <param name="columnName">
        ///     Database table column name.
        /// </param>
        /// <param name="sql">
        ///     <see cref="StringBuilder"/> to add SQL commands to.
        /// </param>
        protected virtual void ProcessRange(
            IConventionProperty property,
            MemberInfo memberInfo,
            string tableName,
            string columnName,
            StringBuilder sql)
        {
            if (!(memberInfo.GetCustomAttribute<RangeAttribute>() is RangeAttribute attribute)
                || !(property.FindTypeMapping() is RelationalTypeMapping typeMapping)
                || attribute.Minimum.GetType() != typeMapping.ClrType
                || attribute.Maximum.GetType() != typeMapping.ClrType)
            {
                return;
            }

            sql.Clear();

            sql
                .Append(_sqlGenerationHelper.DelimitIdentifier(columnName))
                .Append(" >= ")
                .Append(typeMapping.GenerateSqlLiteral(attribute.Minimum))
                .Append(" AND ")
                .Append(_sqlGenerationHelper.DelimitIdentifier(columnName))
                .Append(" <= ")
                .Append(typeMapping.GenerateSqlLiteral(attribute.Maximum));

            var constraintName = $"CK_{tableName}_{columnName}_Range";
            property.DeclaringEntityType.AddCheckConstraint(constraintName, sql.ToString());
        }

        /// <summary>
        ///     Creates SQL check constraint clause for an entity property's
        ///     <see cref="MinLengthAttribute"/> data annotation.
        /// </summary>
        /// <param name="property">
        ///     Property to be examined.
        /// </param>
        /// <param name="memberInfo">
        ///     <see cref="MemberInfo"/> of property to be examined.
        /// </param>
        /// <param name="tableName">
        ///     Database table name.
        /// </param>
        /// <param name="columnName">
        ///     Database table column name.
        /// </param>
        /// <param name="sql">
        ///     <see cref="StringBuilder"/> to add SQL commands to.
        /// </param>
        protected virtual void ProcessMinLength(
            IConventionProperty property,
            MemberInfo memberInfo,
            string tableName,
            string columnName,
            StringBuilder sql)
        {
            if (_intTypeMapping is not null && memberInfo.GetCustomAttribute<MinLengthAttribute>()?.Length is int minLength)
            {
                ProcessMinimumLengthInternal(property, memberInfo, tableName, columnName, sql, minLength);
            }
        }

        /// <summary>
        ///     Creates SQL check constraint clause for an entity property's
        ///     <see cref="StringLengthAttribute.MinimumLength"/> data
        ///     annotation attribute property.
        /// </summary>
        /// <param name="property">
        ///     Property to be examined.
        /// </param>
        /// <param name="memberInfo">
        ///     <see cref="MemberInfo"/> of property to be examined.
        /// </param>
        /// <param name="tableName">
        ///     Database table name.
        /// </param>
        /// <param name="columnName">
        ///     Database table column name.
        /// </param>
        /// <param name="sql">
        ///     <see cref="StringBuilder"/> to add SQL commands to.
        /// </param>
        protected virtual void ProcessStringLengthMinimumLength(
            IConventionProperty property,
            MemberInfo memberInfo,
            string tableName,
            string columnName,
            StringBuilder sql)
        {
            if (_intTypeMapping is not null && memberInfo.GetCustomAttribute<StringLengthAttribute>()?.MinimumLength is int minLength)
            {
                ProcessMinimumLengthInternal(property, memberInfo, tableName, columnName, sql, minLength);
            }
        }

        /// <summary>
        ///     Creates SQL check constraint clause for the provided
        ///     minimum length definition.
        /// </summary>
        /// <param name="property">
        ///     Property to be examined.
        /// </param>
        /// <param name="memberInfo">
        ///     <see cref="MemberInfo"/> of property to be examined.
        /// </param>
        /// <param name="tableName">
        ///     Database table name.
        /// </param>
        /// <param name="columnName">
        ///     Database table column name.
        /// </param>
        /// <param name="sql">
        ///     <see cref="StringBuilder"/> to add SQL commands to.
        /// </param>
        /// <param name="minLength">
        ///     Database table column's minimum length to be validated
        ///     by table column check constraint.
        /// </param>
        protected virtual void ProcessMinimumLengthInternal(
            IConventionProperty property,
            MemberInfo memberInfo,
            string tableName,
            string columnName,
            StringBuilder sql,
            int minLength)
        {
            var lengthFunctionName = _databaseProvider.Name switch
            {
                "Microsoft.EntityFrameworkCore.SqlServer" => "LEN",
                "Microsoft.EntityFrameworkCore.Sqlite" => "LENGTH",
                "Npgsql.EntityFrameworkCore.PostgreSQL" => "LENGTH",
                "Pomelo.EntityFrameworkCore.MySQL" => "LENGTH",
                _ => null
            };

            if (lengthFunctionName is null)
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
            property.DeclaringEntityType.AddCheckConstraint(constraintName, sql.ToString());
        }

        /// <summary>
        ///     Creates SQL check constraint clause for an entity property's
        ///     <see cref="PhoneAttribute"/> data annotation.
        /// </summary>
        /// <param name="property">
        ///     Property to be examined.
        /// </param>
        /// <param name="memberInfo">
        ///     <see cref="MemberInfo"/> of property to be examined.
        /// </param>
        /// <param name="tableName">
        ///     Database table name.
        /// </param>
        /// <param name="columnName">
        ///     Database table column name.
        /// </param>
        /// <param name="sql">
        ///     <see cref="StringBuilder"/> to add SQL commands to.
        /// </param>
        protected virtual void ProcessPhoneNumber(
            IConventionProperty property,
            MemberInfo memberInfo,
            string tableName,
            string columnName,
            StringBuilder sql)
        {
            if (memberInfo.GetCustomAttribute<PhoneAttribute>() != null)
            {
                property.DeclaringEntityType.AddCheckConstraint(
                    $"CK_{tableName}_{columnName}_Phone",
                    GenerateRegexSql(columnName, _phoneRegex));
            }
        }

        /// <summary>
        ///     Creates SQL check constraint clause for an entity property's
        ///     <see cref="CreditCardAttribute"/> data annotation.
        /// </summary>
        /// <param name="property">
        ///     Property to be examined.
        /// </param>
        /// <param name="memberInfo">
        ///     <see cref="MemberInfo"/> of property to be examined.
        /// </param>
        /// <param name="tableName">
        ///     Database table name.
        /// </param>
        /// <param name="columnName">
        ///     Database table column name.
        /// </param>
        /// <param name="sql">
        ///     <see cref="StringBuilder"/> to add SQL commands to.
        /// </param>
        protected virtual void ProcessCreditCard(
            IConventionProperty property,
            MemberInfo memberInfo,
            string tableName,
            string columnName,
            StringBuilder sql)
        {
            if (memberInfo.GetCustomAttribute<CreditCardAttribute>() != null)
            {
                property.DeclaringEntityType.AddCheckConstraint(
                    $"CK_{tableName}_{columnName}_CreditCard",
                    GenerateRegexSql(columnName, _creditCardRegex));
            }
        }

        /// <summary>
        ///     Creates SQL check constraint clause for an entity property's
        ///     <see cref="EmailAddressAttribute"/> data annotation.
        /// </summary>
        /// <param name="property">
        ///     Property to be examined.
        /// </param>
        /// <param name="memberInfo">
        ///     <see cref="MemberInfo"/> of property to be examined.
        /// </param>
        /// <param name="tableName">
        ///     Database table name.
        /// </param>
        /// <param name="columnName">
        ///     Database table column name.
        /// </param>
        /// <param name="sql">
        ///     <see cref="StringBuilder"/> to add SQL commands to.
        /// </param>
        protected virtual void ProcessEmailAddress(
            IConventionProperty property,
            MemberInfo memberInfo,
            string tableName,
            string columnName,
            StringBuilder sql)
        {
            if (memberInfo.GetCustomAttribute<EmailAddressAttribute>() != null)
            {
                property.DeclaringEntityType.AddCheckConstraint(
                    $"CK_{tableName}_{columnName}_EmailAddress",
                    GenerateRegexSql(columnName, _emailAddressRegex));
            }
        }

        /// <summary>
        ///     Creates SQL check constraint clause for an entity property's
        ///     <see cref="UrlAttribute"/> data annotation.
        /// </summary>
        /// <param name="property">
        ///     Property to be examined.
        /// </param>
        /// <param name="memberInfo">
        ///     <see cref="MemberInfo"/> of property to be examined.
        /// </param>
        /// <param name="tableName">
        ///     Database table name.
        /// </param>
        /// <param name="columnName">
        ///     Database table column name.
        /// </param>
        /// <param name="sql">
        ///     <see cref="StringBuilder"/> to add SQL commands to.
        /// </param>
        protected virtual void ProcessUrl(
            IConventionProperty property,
            MemberInfo memberInfo,
            string tableName,
            string columnName,
            StringBuilder sql)
        {
            if (memberInfo.GetCustomAttribute<UrlAttribute>() != null)
            {
                property.DeclaringEntityType.AddCheckConstraint(
                    $"CK_{tableName}_{columnName}_Url",
                    GenerateRegexSql(columnName, _urlRegex));
            }
        }

        /// <summary>
        ///     Creates SQL check constraint clause for an entity property's
        ///     <see cref="RegularExpressionAttribute"/> data annotation.
        /// </summary>
        /// <param name="property">
        ///     Property to be examined.
        /// </param>
        /// <param name="memberInfo">
        ///     <see cref="MemberInfo"/> of property to be examined.
        /// </param>
        /// <param name="tableName">
        ///     Database table name.
        /// </param>
        /// <param name="columnName">
        ///     Database table column name.
        /// </param>
        /// <param name="sql">
        ///     <see cref="StringBuilder"/> to add SQL commands to.
        /// </param>
        protected virtual void ProcessRegularExpression(
            IConventionProperty property,
            MemberInfo memberInfo,
            string tableName,
            string columnName,
            StringBuilder sql)
        {
            if (memberInfo.GetCustomAttribute<RegularExpressionAttribute>()?.Pattern is string pattern)
            {
                property.DeclaringEntityType.AddCheckConstraint(
                    $"CK_{tableName}_{columnName}_RegularExpression",
                    GenerateRegexSql(columnName, pattern));
            }
        }



        /// <summary>
        ///     Creates provider specific SQL constraint clause
        ///     for evaluating the provided regular expression
        ///     pattern string.
        /// </summary>
        /// <param name="columnName">
        ///     Database table column name.
        /// </param>
        /// <param name="regex">
        ///     Regular expression pattern string.
        /// </param>
        /// <returns>
        ///     Provider specific SQL constraint clause
        ///     for evaluating the provided regular expression.
        /// </returns>
        protected virtual string GenerateRegexSql(string columnName, [RegexPattern] string regex)
            => string.Format(
                _databaseProvider.Name switch
                {
                    // For SQL Server, requires setup:
                    // https://www.red-gate.com/simple-talk/sql/t-sql-programming/tsql-regular-expression-workbench/
                    "Microsoft.EntityFrameworkCore.SqlServer" => "dbo.RegexMatch('{1}', {0})",
                    "Microsoft.EntityFrameworkCore.Sqlite" => "{0} REGEXP '{1}'",
                    "Npgsql.EntityFrameworkCore.PostgreSQL" => "{0} ~ '{1}'",
                    "Pomelo.EntityFrameworkCore.MySQL" => "{0} REGEXP '{1}'",
                    _ => throw new InvalidOperationException($"Provider {_databaseProvider.Name} doesn't support regular expressions")
                }, _sqlGenerationHelper.DelimitIdentifier(columnName), regex);

        /// <summary>
        ///     <c>true</c> if the current database provider
        ///     supports regular expression check constraints;
        ///     <c>false</c> otherwise.
        /// </summary>
        protected virtual bool SupportsRegex
            => _databaseProvider.Name switch
            {
                "Microsoft.EntityFrameworkCore.SqlServer" => true,
                "Microsoft.EntityFrameworkCore.Sqlite" => true,
                "Npgsql.EntityFrameworkCore.PostgreSQL" => true,
                "Pomelo.EntityFrameworkCore.MySQL" => true,
                _ => false
            };
    }
}

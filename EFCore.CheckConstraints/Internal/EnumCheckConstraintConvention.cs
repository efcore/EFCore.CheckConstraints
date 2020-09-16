using System;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EFCore.CheckConstraints.Internal
{
    /// <summary>
    ///     A convention that creates check constraints for enum columns.
    /// </summary>
    public class EnumCheckConstraintConvention : IModelFinalizingConvention
    {
        private readonly ISqlGenerationHelper _sqlGenerationHelper;

        public EnumCheckConstraintConvention(ISqlGenerationHelper sqlGenerationHelper)
            => _sqlGenerationHelper = sqlGenerationHelper;

        /// <inheritdoc />
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

                foreach (var property in entityType.GetDeclaredProperties())
                {
                    var typeMapping = property.FindTypeMapping();
                    var propertyType = (property.PropertyInfo ?? (MemberInfo)property.FieldInfo)?.GetMemberType();
                    if ((propertyType?.IsEnum ?? false)
                        && typeMapping != null
                        && !propertyType.IsDefined(typeof(FlagsAttribute), true)
                        && property.GetColumnName() is string columnName)
                    {
                        var enumValues = Enum.GetValues(propertyType);
                        if (enumValues.Length <= 0)
                        {
                            continue;
                        }

                        sql.Clear();

                        sql.Append(_sqlGenerationHelper.DelimitIdentifier(property.GetColumnName()));
                        sql.Append(" IN (");
                        foreach (var item in enumValues)
                        {
                            var value = ((RelationalTypeMapping)typeMapping).GenerateSqlLiteral(item);
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
}

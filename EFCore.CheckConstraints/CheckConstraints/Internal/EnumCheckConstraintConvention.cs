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
    ///     A convention that creates check constraint for Enum column in a model.
    /// </summary>
    public class EnumCheckConstraintConvention : IModelFinalizingConvention
    {
        /// <inheritdoc />
        public virtual void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
        {
            var sql = new StringBuilder();

            foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
            {
                foreach (var property in entityType.GetDeclaredProperties())
                {
                    var typeMapping = property.FindTypeMapping();
                    var propertyType = (property.PropertyInfo ?? (MemberInfo)property.FieldInfo)?.GetMemberType();
                    if ((propertyType?.IsEnum ?? false)
                        && typeMapping != null
                        && !propertyType.IsDefined(typeof(FlagsAttribute), true))
                    {
                        var enumValues = Enum.GetValues(propertyType);
                        if (enumValues.Length <= 0)
                        {
                            continue;
                        }

                        sql.Clear();

                        sql.Append("[");
                        sql.Append(property.GetColumnName());
                        sql.Append("] IN ("); ;
                        foreach (var item in enumValues)
                        {
                            var value = ((RelationalTypeMapping)typeMapping).GenerateSqlLiteral(item);
                            sql.Append($"{value}, ");
                        }

                        sql.Remove(sql.Length - 2, 2);
                        sql.Append(")");

                        var constraintName = $"CK_{entityType.GetTableName()}_{property.GetColumnName()}_Enum_Constraint";
                        entityType.AddCheckConstraint(constraintName, sql.ToString());
                    }
                }
            }
        }
    }
}

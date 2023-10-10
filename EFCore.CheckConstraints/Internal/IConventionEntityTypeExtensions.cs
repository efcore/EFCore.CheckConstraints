using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EFCore.CheckConstraints.Internal;

internal static class IConventionEntityTypeExtensions
{
    public static IEnumerable<IConventionProperty> GetContainedProperties(this IConventionEntityType entityType)
    {
        var structuralTypes = new Stack<IConventionTypeBase>();
        structuralTypes.Push(entityType);
        while (structuralTypes.Count != 0)
        {
            var structuralType = structuralTypes.Pop();

            foreach (var property in structuralType.GetDeclaredProperties())
            {
                yield return property;
            }

            foreach (var complexProperty in structuralType.GetDeclaredComplexProperties())
            {
                structuralTypes.Push(complexProperty.ComplexType);
            }
        }
    }
}

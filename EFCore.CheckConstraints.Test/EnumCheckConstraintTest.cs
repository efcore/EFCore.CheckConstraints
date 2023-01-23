using System;
using System.Linq;
using EFCore.CheckConstraints.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global

namespace EFCore.CheckConstraints.Test;

public class EnumCheckConstraintConventionTest
{
    [Theory]
    [InlineData(typeof(ContiguousEnum), "0", "1")]
    [InlineData(typeof(ContiguousUIntEnum), "CAST(0 AS bigint)", "CAST(1 AS bigint)")]
    [InlineData(typeof(ContiguousLongEnum), "CAST(0 AS bigint)", "CAST(1 AS bigint)")]
    [InlineData(typeof(ContiguousULongEnum), "0.0", "1.0")]
    [InlineData(typeof(ContiguousShortEnum), "CAST(0 AS smallint)", "CAST(1 AS smallint)")]
    [InlineData(typeof(ContiguousUShortEnum), "0", "1")]
    [InlineData(typeof(ContiguousByteEnum), "CAST(0 AS tinyint)", "CAST(1 AS tinyint)")]
    [InlineData(typeof(ContiguousSByteEnum), "CAST(0 AS smallint)", "CAST(1 AS smallint)")]
    [InlineData(typeof(ContiguousEnum?), "0", "1")]
    [InlineData(typeof(ContiguousNegativeEnum), "-2", "-1")]
    [InlineData(typeof(ContiguousWithDuplicatesEnum), "0", "1")]
    [InlineData(typeof(ContiguousStartingAfterZeroEnum), "12", "16")]
    [InlineData(typeof(ContiguousOutOfOrderEnum), "0", "1")]
    public void Contiguous_enums(Type enumType, string minValue, string maxValue)
    {
        var entityType = BuildEntityType(e => e.Property(enumType, "Type"));

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints());
        Assert.NotNull(checkConstraint);
        Assert.Equal("CK_Customer_Type_Enum", checkConstraint.Name);
        Assert.Equal($"[Type] BETWEEN {minValue} AND {maxValue}", checkConstraint.Sql);
    }

    [Fact]
    public void Non_contiguous()
    {
        var entityType = BuildEntityType(e => e.Property<NonContiguousEnum>("Type"));

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints());
        Assert.NotNull(checkConstraint);
        Assert.Equal("CK_Customer_Type_Enum", checkConstraint.Name);
        Assert.Equal("[Type] IN (0, 2)", checkConstraint.Sql);
    }

    [Fact]
    public void Non_contiguous_with_duplicates()
    {
        var entityType = BuildEntityType(e => e.Property<NonContiguousWithDuplicatesEnum>("Type"));

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints());
        Assert.NotNull(checkConstraint);
        Assert.Equal("CK_Customer_Type_Enum", checkConstraint.Name);
        Assert.Equal("[Type] IN (0, 2)", checkConstraint.Sql);
    }

    [Fact]
    public void Contiguous_but_with_value_conversion_to_string()
    {
        var entityType = BuildEntityType(e => e.Property<ContiguousEnum>("Type").HasConversion<string>());

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints());
        Assert.NotNull(checkConstraint);
        Assert.Equal("CK_Customer_Type_Enum", checkConstraint.Name);
        Assert.Equal("[Type] IN (N'A', N'B')", checkConstraint.Sql);
    }

    [Fact]
    public void Constraint_not_created_for_empty_enum()
    {
        var entityType = BuildEntityType(e => e.Property<EmptyEnum>("Type"));

        Assert.Empty(entityType.GetCheckConstraints());
    }

    [Fact]
    public void Constraint_not_created_for_flags_enum()
    {
        var entityType = BuildEntityType(e => e.Property<FlagsEnum>("Type"));

        Assert.Empty(entityType.GetCheckConstraints());
    }

    [Fact]
    public void Constraint_not_created_for_View()
    {
        var entityType = BuildEntityType(e =>
        {
            e.ToView("CustomerView");
            e.Property<ContiguousEnum>("Type");
        });

        Assert.Empty(entityType.GetCheckConstraints());
    }

    [Fact]
    public void TPH()
    {
        var model = BuildModel(b =>
        {
            b.Entity("Customer", e =>
            {
                e.Property<int>("Id");
                e.Property<ContiguousEnum>("Type");
            });
            b.Entity("SpecialCustomer", e =>
            {
                e.HasBaseType("Customer");
                e.Property<ContiguousEnum>("AnotherType");
            });
        });

        var customerCheckConstraint = Assert.Single(model.FindEntityType("Customer")!.GetCheckConstraints());
        Assert.NotNull(customerCheckConstraint);
        Assert.Equal("CK_Customer_Type_Enum", customerCheckConstraint!.Name);

        Assert.Collection(model.FindEntityType("SpecialCustomer")!.GetCheckConstraints().OrderBy(ck => ck.Name),
            ck => Assert.Equal("CK_Customer_AnotherType_Enum", ck.Name),
            ck => Assert.Same(customerCheckConstraint, ck));
    }

    [Fact]
    public void TPT()
    {
        var model = BuildModel(b =>
        {
            b.Entity("Customer", e =>
            {
                e.Property<int>("Id");
                e.Property<ContiguousEnum>("Type");
            });
            b.Entity("SpecialCustomer", e =>
            {
                e.HasBaseType("Customer");
                e.ToTable("SpecialCustomer");
                e.Property<ContiguousEnum>("AnotherType");
            });
        });

        var customerCheckConstraint = Assert.Single(model.FindEntityType("Customer")!.GetCheckConstraints());
        Assert.NotNull(customerCheckConstraint);
        Assert.Equal("CK_Customer_Type_Enum", customerCheckConstraint!.Name);

        Assert.Collection(
            model.FindEntityType("SpecialCustomer")!.GetCheckConstraints().OrderBy(ck => ck.Name),
            ck => Assert.Same(customerCheckConstraint, ck),
            ck => Assert.Equal("CK_SpecialCustomer_AnotherType_Enum", ck.Name));
    }

    #region Test enums

    private enum ContiguousEnum
    {
        A = 0,
        B = 1
    }

    private enum ContiguousNegativeEnum
    {
        A = -1,
        B = -2
    }

    private enum ContiguousWithDuplicatesEnum
    {
        A = 0,
        B = 0,
        C = 1
    }

    private enum ContiguousUIntEnum : uint
    {
        A = 0,
        B = 1
    }

    private enum ContiguousLongEnum : long
    {
        A = 0,
        B = 1
    }

    private enum ContiguousULongEnum : ulong
    {
        A = 0,
        B = 1
    }

    private enum ContiguousByteEnum : byte
    {
        A = 0,
        B = 1
    }

    private enum ContiguousSByteEnum : sbyte
    {
        A = 0,
        B = 1
    }

    private enum ContiguousShortEnum : short
    {
        A = 0,
        B = 1
    }

    private enum ContiguousUShortEnum : ushort
    {
        A = 0,
        B = 1
    }

    private enum ContiguousStartingAfterZeroEnum
    {
        A = 12,
        B = 13,
        C = 14,
        D = 15,
        E = 16
    }

    private enum ContiguousOutOfOrderEnum
    {
        B = 1,
        A = 0
    }

    private enum NonContiguousEnum
    {
        A = 0,
        B = 2
    }

    private enum NonContiguousWithDuplicatesEnum
    {
        A = 0,
        B = 0,
        C = 2
    }

    private enum EmptyEnum {}

    [Flags]
    private enum FlagsEnum
    {
        A = 0,
        B = 1
    }

    #endregion Test enums

    #region Support

    private IModel BuildModel(Action<ModelBuilder> buildAction)
    {
        var serviceProvider = SqlServerTestHelpers.Instance.CreateContextServices();
        var conventionSet = serviceProvider.GetRequiredService<IConventionSetBuilder>().CreateConventionSet();

        conventionSet.ModelFinalizingConventions.Add(
            new EnumCheckConstraintConvention(
                serviceProvider.GetRequiredService<IRelationalTypeMappingSource>(),
                serviceProvider.GetRequiredService<ISqlGenerationHelper>()));

        var builder = new ModelBuilder(conventionSet);
        buildAction(builder);
        return builder.FinalizeModel();
    }

    private IEntityType BuildEntityType(Action<EntityTypeBuilder> buildAction)
        => BuildModel(b => buildAction(b.Entity("Customer"))).GetEntityTypes().Single();

    #endregion
}

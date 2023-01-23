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
    [InlineData(typeof(CustomerType))]
    [InlineData(typeof(CustomerType?))]
    [InlineData(typeof(CustomerTypeWithDuplicates))]
    [InlineData(typeof(CustomerTypeUShort))]
    [InlineData(typeof(CustomerTypeOutOfOrder))]
    public void Simple(Type enumType)
    {
        var entityType = BuildEntityType(e => e.Property(enumType, "Type"));

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints());
        Assert.NotNull(checkConstraint);
        Assert.Equal("CK_Customer_Type_Enum", checkConstraint.Name);
        Assert.Equal("[Type] BETWEEN 0 AND 1", checkConstraint.Sql);
    }

    [Theory]
    [InlineData(typeof(CustomerTypeLong), "bigint")]
    [InlineData(typeof(CustomerTypeUInt), "bigint")]
    [InlineData(typeof(CustomerTypeByte), "tinyint")]
    [InlineData(typeof(CustomerTypeSByte), "smallint")]
    [InlineData(typeof(CustomerTypeShort), "smallint")]
    public void Simple_WithCast(Type enumType, string destinationType)
    {
        var entityType = BuildEntityType(e => e.Property(enumType, "Type"));

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints());
        Assert.NotNull(checkConstraint);
        Assert.Equal("CK_Customer_Type_Enum", checkConstraint.Name);
        Assert.Equal($"[Type] BETWEEN CAST(0 AS {destinationType}) AND CAST(1 AS {destinationType})", checkConstraint.Sql);
    }

    [Fact]
    public void Simple_NonContiguous()
    {
        var entityType = BuildEntityType(e => e.Property<NonContiguousCustomerType>("Type"));

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints());
        Assert.NotNull(checkConstraint);
        Assert.Equal("CK_Customer_Type_Enum", checkConstraint.Name);
        Assert.Equal("[Type] IN (0, 2)", checkConstraint.Sql);
    }

    [Theory]
    [InlineData(typeof(CustomerTypeNegative), "-2", "-1")]
    [InlineData(typeof(CustomerTypeStartingAfterZero), "12", "16")]
    [InlineData(typeof(CustomerTypeULong), "0.0", "1.0")]
    public void Simple_Range(Type enumType, string minValue, string maxValue)
    {
        var entityType = BuildEntityType(e => e.Property(enumType, "Type"));

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints());
        Assert.NotNull(checkConstraint);
        Assert.Equal("CK_Customer_Type_Enum", checkConstraint.Name);
        Assert.Equal($"[Type] BETWEEN {minValue} AND {maxValue}", checkConstraint.Sql);
    }

    [Fact]
    public void Value_converter()
    {
        var entityType = BuildEntityType(e => e.Property<CustomerType>("Type").HasConversion<string>());

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints());
        Assert.NotNull(checkConstraint);
        Assert.Equal("CK_Customer_Type_Enum", checkConstraint.Name);
        Assert.Equal("[Type] IN (N'Standard', N'Premium')", checkConstraint.Sql);
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
            e.Property<CustomerType>("Type");
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
                e.Property<CustomerType>("Type");
            });
            b.Entity("SpecialCustomer", e =>
            {
                e.HasBaseType("Customer");
                e.Property<CustomerType>("AnotherType");
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
                e.Property<CustomerType>("Type");
            });
            b.Entity("SpecialCustomer", e =>
            {
                e.HasBaseType("Customer");
                e.ToTable("SpecialCustomer");
                e.Property<CustomerType>("AnotherType");
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

    #region Support

    private enum CustomerType
    {
        Standard = 0,
        Premium = 1
    }

    private enum CustomerTypeNegative
    {
        Standard = -1,
        Premium = -2
    }

    private enum CustomerTypeWithDuplicates
    {
        Basic = 0,
        Standard = 0,
        Premium = 1
    }

    private enum CustomerTypeUInt : uint
    {
        Standard = 0,
        Premium = 1
    }

    private enum CustomerTypeLong : long
    {
        Standard = 0,
        Premium = 1
    }

    private enum CustomerTypeULong : ulong
    {
        Standard = 0,
        Premium = 1
    }

    private enum CustomerTypeByte : byte
    {
        Standard = 0,
        Premium = 1
    }

    private enum CustomerTypeSByte : sbyte
    {
        Standard = 0,
        Premium = 1
    }

    private enum CustomerTypeShort : short
    {
        Standard = 0,
        Premium = 1
    }

    private enum CustomerTypeUShort : ushort
    {
        Standard = 0,
        Premium = 1
    }

    private enum NonContiguousCustomerType
    {
        Standard = 0,
        Premium = 2
    }

    private enum CustomerTypeStartingAfterZero
    {
        Basic = 12,
        Shared = 13,
        Standard = 14,
        Premium = 15,
        Enterprise = 16
    }

    private enum CustomerTypeOutOfOrder
    {
        Standard = 1,
        Premium = 0
    }

    private enum EmptyEnum {}

    [Flags]
    private enum FlagsEnum
    {
        Standard = 0,
        Premium = 1
    }

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

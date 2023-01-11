using System;
using System.Linq;
using EFCore.CheckConstraints.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
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
    [Fact]
    public void Simple()
    {
        var entityType = BuildEntityType(e => e.Property<CustomerType>("Type"));

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints());
        Assert.NotNull(checkConstraint);
        Assert.Equal("CK_Customer_Type_Enum", checkConstraint.Name);
        Assert.Equal("[Type] BETWEEN 0 AND 1", checkConstraint.Sql);
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

    [Fact]
    public void Simple_Range()
    {
        var entityType = BuildEntityType(e => e.Property<CustomerTypeWithManyValues>("Type"));

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints());
        Assert.NotNull(checkConstraint);
        Assert.Equal("CK_Customer_Type_Enum", checkConstraint.Name);
        Assert.Equal("[Type] BETWEEN 12 AND 16", checkConstraint.Sql);
    }

    [Fact]
    public void Nullable()
    {
        var entityType = BuildEntityType(e => e.Property<CustomerType?>("Type"));

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints());
        Assert.NotNull(checkConstraint);
        Assert.Equal("CK_Customer_Type_Enum", checkConstraint.Name);
        Assert.Equal("[Type] BETWEEN 0 AND 1", checkConstraint.Sql);
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

    private enum NonContiguousCustomerType
    {
        Standard = 0,
        Premium = 2
    }

    private enum CustomerTypeWithManyValues
    {
        Basic = 12,
        Shared = 13,
        Standard = 14,
        Premium = 15,
        Enterprise = 16
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

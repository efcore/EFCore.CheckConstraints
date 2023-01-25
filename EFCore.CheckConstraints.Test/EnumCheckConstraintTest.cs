using System;
using System.Linq;
using EFCore.CheckConstraints.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
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
        Assert.Equal("[Type] IN (0, 1)", checkConstraint.Sql);
    }

    [Fact]
    public void Nullable()
    {
        var entityType = BuildEntityType(e => e.Property<CustomerType?>("Type"));

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints());
        Assert.NotNull(checkConstraint);
        Assert.Equal("CK_Customer_Type_Enum", checkConstraint.Name);
        Assert.Equal("[Type] IN (0, 1)", checkConstraint.Sql);
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

    [Fact]
    public void No_check_constraint_for_database_enums()
    {
        // Simulate the PostgreSQL case, where we can map to database enums without any conversion.
        // No check constraint should be created.
        var entityType = BuildEntityType(e => e.Property<CustomerType>("Type"), new SqlServerTestHelpersWithEnumSupport());

        Assert.Empty(entityType.GetCheckConstraints());
    }

    #region Support

    private enum CustomerType
    {
        Standard = 0,
        Premium = 1
    }

    private enum EmptyEnum {}

    [Flags]
    private enum FlagsEnum
    {
        Standard = 0,
        Premium = 1
    }

    private IModel BuildModel(Action<ModelBuilder> buildAction, TestHelpers? testHelpers = null)
    {
        testHelpers ??= SqlServerTestHelpers.Instance;

        var serviceProvider = testHelpers.CreateContextServices();
        var conventionSet = serviceProvider.GetRequiredService<IConventionSetBuilder>().CreateConventionSet();

        conventionSet.ModelFinalizingConventions.Add(
            new EnumCheckConstraintConvention(
                serviceProvider.GetRequiredService<IRelationalTypeMappingSource>(),
                serviceProvider.GetRequiredService<ISqlGenerationHelper>()));

        var builder = new ModelBuilder(conventionSet);
        buildAction(builder);
        return builder.FinalizeModel();
    }

    private class SqlServerTestHelpersWithEnumSupport : SqlServerTestHelpers
    {
        public override IServiceCollection AddProviderServices(IServiceCollection services)
        {
            base.AddProviderServices(services);
            services.AddSingleton<IRelationalTypeMappingSource, SqlServerTypeMappingSourceWithEnumSupport>();
            return services;
        }
    }

#pragma warning disable EF1001
    class SqlServerTypeMappingSourceWithEnumSupport : SqlServerTypeMappingSource
    {
        public SqlServerTypeMappingSourceWithEnumSupport(
            TypeMappingSourceDependencies dependencies,
            RelationalTypeMappingSourceDependencies relationalDependencies)
            : base(dependencies, relationalDependencies)
        {
        }

        protected override RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
            => mappingInfo.ClrType?.IsEnum == true
                ? new FakeEnumTypeMapping("fake_enum", mappingInfo.ClrType)
                : base.FindMapping(in mappingInfo);

        private class FakeEnumTypeMapping : RelationalTypeMapping
        {
            public FakeEnumTypeMapping(string storeType, Type clrType)
                : base(storeType, clrType)
            {
            }

            private FakeEnumTypeMapping(RelationalTypeMappingParameters parameters)
                : base(parameters)
            {
            }

            protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
                => new FakeEnumTypeMapping(parameters);
        }
    }
#pragma warning restore EF1001

    private IEntityType BuildEntityType(Action<EntityTypeBuilder> buildAction, TestHelpers? testHelpers = null)
        => BuildModel(b => buildAction(b.Entity("Customer")), testHelpers).GetEntityTypes().Single();

    #endregion
}

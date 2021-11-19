using System;
using EFCore.CheckConstraints.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EFCore.CheckConstraints.Test
{
    public class DiscriminatorCheckConstraintTest
    {
        [Fact]
        public void Generate_check_constraint_with_all_enum_names()
        {
            var model = BuildModel(b =>
            {
                b.Entity("Parent", e =>
                {
                    e.Property<int>("Id");
                    e.Property<string>("Discriminator");
                });
                b.Entity("Child", e =>
                {
                    e.HasBaseType("Parent");
                    e.Property<int>("ChildProperty");
                });
            });

            var checkConstraint = Assert.Single(model.FindEntityType("Parent")!.GetCheckConstraints());
            Assert.NotNull(checkConstraint);
            Assert.Equal("CK_Parent_Discriminator", checkConstraint!.Name);
            Assert.Equal("[Discriminator] IN (N'Child', N'Parent')", checkConstraint.Sql);
        }

        [Fact]
        public void Generate_check_constraint_skips_abstract_types()
        {
            var model = BuildModel(b =>
            {
                b.Entity<Base>();
                b.Entity<Intermediate>();
                b.Entity<Derived>();
            });

            var checkConstraint = Assert.Single(model.FindEntityType(typeof(Base))!.GetCheckConstraints());
            Assert.NotNull(checkConstraint);
            Assert.Equal("CK_Base_Discriminator", checkConstraint.Name);
            Assert.Equal("[Discriminator] IN (N'Base', N'Derived')", checkConstraint.Sql);
        }

        private class Base
        {
            public int Id { get; set; }

        }

        private abstract class Intermediate : Base
        {
            public string Value { get; set; }  = null!;
        }

        private class Derived : Intermediate
        {
            public string Property { get; set; } = null!;
        }

        #region Support

        private IModel BuildModel(Action<ModelBuilder> buildAction)
        {
            var serviceProvider = SqlServerTestHelpers.Instance.CreateContextServices();
            var conventionSet = serviceProvider.GetRequiredService<IConventionSetBuilder>().CreateConventionSet();

            conventionSet.ModelFinalizingConventions.Add(
                new DiscriminatorCheckConstraintConvention(
                    serviceProvider.GetRequiredService<IRelationalTypeMappingSource>(),
                    serviceProvider.GetRequiredService<ISqlGenerationHelper>()));

            var modelBuilder = new ModelBuilder(conventionSet);
            buildAction(modelBuilder);
            return modelBuilder.FinalizeModel();
        }

        #endregion
    }
}

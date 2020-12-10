using System;
using EFCore.CheckConstraints.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
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

            var checkConstraint = Assert.Single(model.FindEntityType("Parent").GetCheckConstraints());
            Assert.NotNull(checkConstraint);
            Assert.Equal("CK_Parent_Discriminator", checkConstraint.Name);
            Assert.Equal("[Discriminator] IN (N'Child', N'Parent')", checkConstraint.Sql);
        }

        #region Support

        private IModel BuildModel(Action<ModelBuilder> buildAction)
        {
            var serviceProvider = SqlServerTestHelpers.Instance.CreateContextServices();

            var conventionSet = SqlServerTestHelpers.Instance.CreateConventionSetBuilder().CreateConventionSet();
            ConventionSet.Remove(conventionSet.ModelFinalizedConventions, typeof(ValidatingConvention));
            conventionSet.ModelFinalizingConventions.Add(
                new DiscriminatorCheckConstraintConvention(serviceProvider.GetRequiredService<ISqlGenerationHelper>()));

            var builder = new ModelBuilder(conventionSet);
            buildAction(builder);
            return builder.FinalizeModel();
        }

        #endregion
    }
}

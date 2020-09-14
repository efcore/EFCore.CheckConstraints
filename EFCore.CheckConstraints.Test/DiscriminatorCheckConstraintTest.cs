using EFCore.CheckConstraints.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
// ReSharper disable ClassNeverInstantiated.Local

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global

namespace EFCore.CheckConstraints.Test
{
    public class DiscriminatorCheckConstraintTest
    {
        [Fact]
        public void Generate_check_constraint_with_all_enum_names()
        {
            var builder = CreateBuilder();
            builder.Entity<Parent>();
            builder.Entity<Child>();

            var model = builder.FinalizeModel();

            var checkConstraint = Assert.Single(model.FindEntityType(typeof(Parent)).GetCheckConstraints());
            Assert.NotNull(checkConstraint);
            Assert.Equal("CK_Parent_Discriminator_Constraint", checkConstraint.Name);
            Assert.Equal("[Discriminator] IN (N'Child', N'Parent')", checkConstraint.Sql);
        }

        private class Parent
        {
            public int Id { get; set; }
            public string Discriminator { get; set; }
        }

        private class Child : Parent
        {
            public string ChildProperty { get; set; }
        }

        private ModelBuilder CreateBuilder()
        {
            var serviceProvider = SqlServerTestHelpers.Instance.CreateContextServices();
            var conventionSet = serviceProvider.GetRequiredService<IConventionSetBuilder>().CreateConventionSet();

            conventionSet.ModelFinalizingConventions.Add(
                new DiscriminatorCheckConstraintConvention(serviceProvider.GetRequiredService<ISqlGenerationHelper>()));

            return new ModelBuilder(conventionSet);
        }
    }
}

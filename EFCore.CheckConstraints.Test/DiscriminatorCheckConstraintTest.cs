using EFCore.CheckConstraints.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
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
            var builder = CreateBuilder();
            builder.Entity<Parent>();
            builder.Entity<Child>();

            var model = builder.FinalizeModel();

            var checkConstraint = Assert.Single(model.FindEntityType(typeof(Parent)).GetCheckConstraints());
            Assert.NotNull(checkConstraint);
            Assert.Equal("CK_Parent_Discriminator_Constraint", checkConstraint.Name);
            Assert.Equal("[Discriminator] IN (N'Child', N'Parent')", checkConstraint.Sql);
        }

        class Parent
        {
            public int Id { get; set; }
            public string Discriminator { get; set; }
        }

        class Child : Parent
        {
            public string ChildProperty { get; set; }
        }

        private ModelBuilder CreateBuilder()
        {
            var conventionSet = SqlServerTestHelpers.Instance.CreateContextServices()
                .GetRequiredService<IConventionSetBuilder>()
                .CreateConventionSet();

            conventionSet.ModelFinalizingConventions.Add(
                new DiscriminatorCheckConstraintConvention(
                    new SqlServerSqlGenerationHelper(
                        new RelationalSqlGenerationHelperDependencies())));

            return new ModelBuilder(conventionSet);
        }
    }
}

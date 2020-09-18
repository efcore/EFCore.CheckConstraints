using System.ComponentModel.DataAnnotations;
using EFCore.CheckConstraints.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EFCore.CheckConstraints.Test
{
    public class ValidationCheckConstraintTest
    {
        [Fact]
        public virtual void Range()
        {
            var builder = CreateBuilder();
            builder.Entity<Blog>();

            var model = builder.FinalizeModel();

            var checkConstraint = Assert.Single(
                model.FindEntityType(typeof(Blog)).GetCheckConstraints(),
                c => c.Name == "CK_Blog_Rating_Range");
            Assert.NotNull(checkConstraint);
            Assert.Equal("[Rating] >= 1 AND [Rating] <= 5", checkConstraint.Sql);
        }

        [Fact]
        public void MinLength()
        {
            var builder = CreateBuilder();
            builder.Entity<Blog>();

            var model = builder.FinalizeModel();

            var checkConstraint = Assert.Single(
                model.FindEntityType(typeof(Blog)).GetCheckConstraints(),
                c => c.Name == "CK_Blog_Name_MinLength");
            Assert.NotNull(checkConstraint);
            Assert.Equal("LEN([Name]) >= 4", checkConstraint.Sql);
        }

        [Fact]
        public virtual void Phone()
        {
            var builder = CreateBuilder();
            builder.Entity<Blog>();

            var model = builder.FinalizeModel();

            var checkConstraint = Assert.Single(
                model.FindEntityType(typeof(Blog)).GetCheckConstraints(),
                c => c.Name == "CK_Blog_PhoneNumber_Phone");
            Assert.NotNull(checkConstraint);
            Assert.Equal(
                $"dbo.RegexMatch('{ValidationCheckConstraintConvention.DefaultPhoneRegex}', [PhoneNumber])",
                checkConstraint.Sql);
        }

        [Fact]
        public virtual void CreditCard()
        {
            var builder = CreateBuilder();
            builder.Entity<Blog>();

            var model = builder.FinalizeModel();

            var checkConstraint = Assert.Single(
                model.FindEntityType(typeof(Blog)).GetCheckConstraints(),
                c => c.Name == "CK_Blog_CreditCard_CreditCard");
            Assert.NotNull(checkConstraint);
            Assert.Equal(
                $"dbo.RegexMatch('{ValidationCheckConstraintConvention.DefaultCreditCardRegex}', [CreditCard])",
                checkConstraint.Sql);
        }

        [Fact]
        public virtual void EmailAddress()
        {
            var builder = CreateBuilder();
            builder.Entity<Blog>();

            var model = builder.FinalizeModel();

            var checkConstraint = Assert.Single(
                model.FindEntityType(typeof(Blog)).GetCheckConstraints(),
                c => c.Name == "CK_Blog_Email_EmailAddress");
            Assert.NotNull(checkConstraint);
            Assert.Equal(
                $"dbo.RegexMatch('{ValidationCheckConstraintConvention.DefaultEmailAddressRegex}', [Email])",
                checkConstraint.Sql);
        }

        [Fact]
        public virtual void Url()
        {
            var builder = CreateBuilder();
            builder.Entity<Blog>();

            var model = builder.FinalizeModel();

            var checkConstraint = Assert.Single(
                model.FindEntityType(typeof(Blog)).GetCheckConstraints(),
                c => c.Name == "CK_Blog_Address_Url");
            Assert.NotNull(checkConstraint);
            Assert.Equal(
                $"dbo.RegexMatch('{ValidationCheckConstraintConvention.DefaultUrlAddressRegex}', [Address])",
                checkConstraint.Sql);
        }

        [Fact]
        public virtual void RegularExpression()
        {
            var builder = CreateBuilder();
            builder.Entity<Blog>();

            var model = builder.FinalizeModel();

            var checkConstraint = Assert.Single(
                model.FindEntityType(typeof(Blog)).GetCheckConstraints(),
                c => c.Name == "CK_Blog_StartsWithA_RegularExpression");
            Assert.NotNull(checkConstraint);
            Assert.Equal("dbo.RegexMatch('^A', [StartsWithA])", checkConstraint.Sql);
        }

        class Blog
        {
            public int Id { get; set; }
            [Range(1, 5)]
            public int Rating { get; set; }
            [MinLength(4)]
            public string Name { get; set; }
            [Phone]
            public string PhoneNumber { get; set; }
            [CreditCard]
            public string CreditCard { get; set; }
            [EmailAddress]
            public string Email { get; set; }
            [Url]
            public string Address { get; set; }
            [RegularExpression("^A")]
            public string StartsWithA { get; set; }
        }

        private ModelBuilder CreateBuilder()
        {
            var serviceProvider = SqlServerTestHelpers.Instance.CreateContextServices();
            var conventionSet = serviceProvider.GetRequiredService<IConventionSetBuilder>().CreateConventionSet();

            conventionSet.ModelFinalizingConventions.Add(
                new ValidationCheckConstraintConvention(
                    new ValidationCheckConstraintOptions(),
                    serviceProvider.GetRequiredService<ISqlGenerationHelper>(),
                    serviceProvider.GetRequiredService<IRelationalTypeMappingSource>(),
                    serviceProvider.GetRequiredService<IDatabaseProvider>()));

            return new ModelBuilder(conventionSet);
        }
    }
}

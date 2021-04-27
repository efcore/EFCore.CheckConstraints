using System;
using System.ComponentModel.DataAnnotations;
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

namespace EFCore.CheckConstraints.Test
{
    public class ValidationCheckConstraintTest
    {
        [Fact]
        public virtual void Range()
        {
            var entityType = BuildEntityType<Blog>();

            var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_Rating_Range");
            Assert.NotNull(checkConstraint);
            Assert.Equal("[Rating] >= 1 AND [Rating] <= 5", checkConstraint.Sql);
        }

        [Fact]
        public void MinLength()
        {
            var entityType = BuildEntityType<Blog>();

            var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_Name_MinLength");
            Assert.NotNull(checkConstraint);
            Assert.Equal("LEN([Name]) >= 4", checkConstraint.Sql);
        }

        [Fact]
        public void StringLengthMinimumLength()
        {
            var entityType = BuildEntityType<Blog>();

            var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_Required_MinLength");
            Assert.NotNull(checkConstraint);
            Assert.Equal("LEN([Required]) >= 1", checkConstraint.Sql);
        }

        [Fact]
        public void NoRegex()
        {
            var entityType = BuildEntityType<Blog>(useRegex: false);

            Assert.DoesNotContain(entityType.GetCheckConstraints(), c => c.Name.StartsWith("dbo.RegexMatch("));
        }

        [Fact]
        public virtual void Phone()
        {
            var entityType = BuildEntityType<Blog>();

            var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_PhoneNumber_Phone");
            Assert.NotNull(checkConstraint);
            Assert.Equal(
                $"dbo.RegexMatch('{ValidationCheckConstraintConvention.DefaultPhoneRegex}', [PhoneNumber])",
                checkConstraint.Sql);
        }

        [Fact]
        public virtual void CreditCard()
        {
            var entityType = BuildEntityType<Blog>();

            var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_CreditCard_CreditCard");
            Assert.NotNull(checkConstraint);
            Assert.Equal(
                $"dbo.RegexMatch('{ValidationCheckConstraintConvention.DefaultCreditCardRegex}', [CreditCard])",
                checkConstraint.Sql);
        }

        [Fact]
        public virtual void EmailAddress()
        {
            var entityType = BuildEntityType<Blog>();

            var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_Email_EmailAddress");
            Assert.NotNull(checkConstraint);
            Assert.Equal(
                $"dbo.RegexMatch('{ValidationCheckConstraintConvention.DefaultEmailAddressRegex}', [Email])",
                checkConstraint.Sql);
        }

        [Fact]
        public virtual void Url()
        {
            var entityType = BuildEntityType<Blog>();

            var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_Address_Url");
            Assert.NotNull(checkConstraint);
            Assert.Equal(
                $"dbo.RegexMatch('{ValidationCheckConstraintConvention.DefaultUrlAddressRegex}', [Address])",
                checkConstraint.Sql);
        }

        [Fact]
        public virtual void RegularExpression()
        {
            var entityType = BuildEntityType<Blog>();

            var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_StartsWithA_RegularExpression");
            Assert.NotNull(checkConstraint);
            Assert.Equal("dbo.RegexMatch('^A', [StartsWithA])", checkConstraint.Sql);
        }

        #region Support

        // ReSharper disable UnusedMember.Local
        class Blog
        {
            public int Id { get; set; }

            [Range(1, 5)]
            public int Rating { get; set; }

            [MinLength(4)]
            public string Name { get; set; } = null!;

            [StringLength(100, MinimumLength = 1)]
            public string Required { get; set; } = null!;

            [Phone]
            public string PhoneNumber { get; set; } = null!;

            [CreditCard]
            public string CreditCard { get; set; } = null!;

            [EmailAddress]
            public string Email { get; set; } = null!;

            [Url]
            public string Address { get; set; } = null!;

            [RegularExpression("^A")]
            public string StartsWithA { get; set; } = null!;
        }
        // ReSharper restore UnusedMember.Local

        private IModel BuildModel(Action<ModelBuilder> buildAction, bool useRegex)
        {
            var serviceProvider = SqlServerTestHelpers.Instance.CreateContextServices();
            var conventionSet = serviceProvider.GetRequiredService<IConventionSetBuilder>().CreateConventionSet();

            conventionSet.ModelFinalizingConventions.Add(
                new ValidationCheckConstraintConvention(
                    new ValidationCheckConstraintOptions { UseRegex = useRegex },
                    serviceProvider.GetRequiredService<IRelationalTypeMappingSource>(),
                    serviceProvider.GetRequiredService<ISqlGenerationHelper>(),
                    serviceProvider.GetRequiredService<IRelationalTypeMappingSource>(),
                    serviceProvider.GetRequiredService<IDatabaseProvider>()));

            var builder = new ModelBuilder(conventionSet);
            buildAction(builder);
            return builder.FinalizeModel();
        }

        private IEntityType BuildEntityType<TEntity>(Action<EntityTypeBuilder<TEntity>>? buildAction = null, bool useRegex = true)
            where TEntity : class
        {
            return BuildModel(buildAction is null
                ? b => b.Entity<TEntity>()
                : b => buildAction(b.Entity<TEntity>()),
                useRegex).GetEntityTypes().Single();
        }

        #endregion
    }
}

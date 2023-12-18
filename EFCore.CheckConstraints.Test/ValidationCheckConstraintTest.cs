using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

namespace EFCore.CheckConstraints.Test;

public class ValidationCheckConstraintTest
{
    [Fact]
    public virtual void Range()
    {
        var entityType = BuildEntityType<Blog>();

        var ratingCheckConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_Rating_Range");
        Assert.NotNull(ratingCheckConstraint);
        Assert.Equal("[Rating] >= 1 AND [Rating] <= 5", ratingCheckConstraint.Sql);

        var longitudeCheckConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_Location_Longitude_Range");
        Assert.Equal("[Location_Longitude] >= -180.0E0 AND [Location_Longitude] <= 180.0E0", longitudeCheckConstraint.Sql);

        var latitudeCheckConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_Location_Latitude_Range");
        Assert.Equal("[Location_Latitude] >= -90.0E0 AND [Location_Latitude] <= 90.0E0", latitudeCheckConstraint.Sql);
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
    public void RequiredInt()
    {
        var entityType = BuildEntityType<Blog>();

        Assert.DoesNotContain(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_RequiredInt_MinLength");
    }

    [Fact]
    public void RequiredIntWithAllowEmptyStringsFalse()
    {
        var entityType = BuildEntityType<Blog>();

        Assert.DoesNotContain(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_RequiredIntWithAllowEmptyStringsFalse_MinLength");
    }

    [Fact]
    public void RequiredAllowEmptyStringsFalse()
    {
        var entityType = BuildEntityType<Blog>();

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_RequiredAllowEmptyStringsFalse_MinLength");
        Assert.NotNull(checkConstraint);
        Assert.Equal("LEN([RequiredAllowEmptyStringsFalse]) >= 1", checkConstraint.Sql);
    }

    [Fact]
    public void RequiredAllowEmptyStringsTrue()
    {
        var entityType = BuildEntityType<Blog>();

        Assert.DoesNotContain(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_RequiredAllowEmptyStringsTrue_MinLength");
    }

    [Fact]
    public void NoRegex()
    {
        var entityType = BuildEntityType<Blog>(useRegex: false);

        Assert.DoesNotContain(entityType.GetCheckConstraints(), c => c.Name!.StartsWith("dbo.RegexMatch("));
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

        [Required]
        public int RequiredInt { get; set; }

        [Required(AllowEmptyStrings = false)]
        public int RequiredIntWithAllowEmptyStringsFalse { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string RequiredAllowEmptyStringsFalse { get; set; } = null!;

        [Required(AllowEmptyStrings = true)]
        public string RequiredAllowEmptyStringsTrue { get; set; } = null!;

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

        public required Location Location { get; set; }
    }
    // ReSharper restore UnusedMember.Local

    [ComplexType]
    public class Location
    {
        [Range(-180.0, 180.0)]
        public double Longitude { get; set; }

        [Range(-90.0, 90.0)]
        public double Latitude { get; set; }
    }

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

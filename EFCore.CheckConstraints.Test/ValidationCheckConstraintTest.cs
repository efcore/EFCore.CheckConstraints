using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using EFCore.CheckConstraints.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
        Assert.Equal("[Rating] BETWEEN 1 AND 5", ratingCheckConstraint.Sql);
    }

    [Fact]
    public virtual void RangeWithExclusiveMinimum()
    {
        var entityType = BuildEntityType<Blog>();

        var ratingCheckConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_RangeWithExclusiveMinimum_Range");
        Assert.NotNull(ratingCheckConstraint);
        Assert.Equal("[RangeWithExclusiveMinimum] > 1 AND [RangeWithExclusiveMinimum] <= 5", ratingCheckConstraint.Sql);
    }

    [Fact]
    public virtual void RangeWithExclusiveMaximum()
    {
        var entityType = BuildEntityType<Blog>();

        var ratingCheckConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_RangeWithExclusiveMaximum_Range");
        Assert.NotNull(ratingCheckConstraint);
        Assert.Equal("[RangeWithExclusiveMaximum] >= 1 AND [RangeWithExclusiveMaximum] < 5", ratingCheckConstraint.Sql);
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
    public void StringWithLength()
    {
        var entityType = BuildEntityType<Blog>();

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_StringWithLength_MinMaxLength");
        Assert.NotNull(checkConstraint);
        Assert.Equal("LEN([StringWithLength]) BETWEEN 2 AND 5", checkConstraint.Sql);
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
    public void RequiredValues()
    {
        var entityType = BuildEntityType<Blog>();

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_AllowedValues_AllowedValues");
        Assert.NotNull(checkConstraint);
        Assert.Equal("[AllowedValues] IN (N'foo', N'bar')", checkConstraint.Sql);
    }

    [Fact]
    public void DeniedValues()
    {
        var entityType = BuildEntityType<Blog>();

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_DeniedValues_DeniedValues");
        Assert.NotNull(checkConstraint);
        Assert.Equal("[DeniedValues] NOT IN (N'foo', N'bar')", checkConstraint.Sql);
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
            $"dbo.RegexMatch('{ValidationCheckConstraintConvention.DefaultPhoneRegex}', [PhoneNumber]) > 0",
            checkConstraint.Sql);
    }

    [Fact]
    public virtual void CreditCard()
    {
        var entityType = BuildEntityType<Blog>();

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_CreditCard_CreditCard");
        Assert.NotNull(checkConstraint);
        Assert.Equal(
            $"dbo.RegexMatch('{ValidationCheckConstraintConvention.DefaultCreditCardRegex}', [CreditCard]) > 0",
            checkConstraint.Sql);
    }

    [Fact]
    public virtual void EmailAddress()
    {
        var entityType = BuildEntityType<Blog>();

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_Email_EmailAddress");
        Assert.NotNull(checkConstraint);
        Assert.Equal(
            $"dbo.RegexMatch('{ValidationCheckConstraintConvention.DefaultEmailAddressRegex}', [Email]) > 0",
            checkConstraint.Sql);
    }

    [Fact]
    public virtual void Url()
    {
        var entityType = BuildEntityType<Blog>();

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_Address_Url");
        Assert.NotNull(checkConstraint);
        Assert.Equal(
            $"dbo.RegexMatch('{ValidationCheckConstraintConvention.DefaultUrlAddressRegex}', [Address]) > 0",
            checkConstraint.Sql);
    }

    [Fact]
    public virtual void RegularExpression()
    {
        var entityType = BuildEntityType<Blog>();

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_StartsWithA_RegularExpression");
        Assert.NotNull(checkConstraint);
        Assert.Equal("dbo.RegexMatch('^A', [StartsWithA]) > 0", checkConstraint.Sql);
    }

    [Fact]
    public virtual void RegularExpressionNavtiveMethod()
    {
        var entityType = BuildEntityType<Blog>(isAzureSql: true);

        var checkConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_StartsWithA_RegularExpression");
        Assert.NotNull(checkConstraint);
        Assert.Equal("REGEXP_LIKE ([StartsWithA], '^A')", checkConstraint.Sql);
    }

    [Fact]
    public virtual void Properties_on_complex_type()
    {
        var entityType = BuildEntityType<Blog>();

        var longitudeCheckConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_Location_Longitude_Range");
        Assert.Equal("[Location_Longitude] BETWEEN -180.0E0 AND 180.0E0", longitudeCheckConstraint.Sql);

        var latitudeCheckConstraint = Assert.Single(entityType.GetCheckConstraints(), c => c.Name == "CK_Blog_Location_Latitude_Range");
        Assert.Equal("[Location_Latitude] BETWEEN -90.0E0 AND 90.0E0", latitudeCheckConstraint.Sql);
    }

    #region Support

    // ReSharper disable UnusedMember.Local
    class Blog
    {
        public int Id { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [Range(1, 5, MinimumIsExclusive = true)]
        public int RangeWithExclusiveMinimum { get; set; }

        [Range(1, 5, MaximumIsExclusive = true)]
        public int RangeWithExclusiveMaximum { get; set; }

        [MinLength(4)]
        public string Name { get; set; } = null!;

        [Length(2, 5)]
        public required string StringWithLength { get; set; }

        [StringLength(100, MinimumLength = 1)]
        public required string Required { get; set; }

        [Required(AllowEmptyStrings = false)]
        public required string RequiredAllowEmptyStringsFalse { get; set; }

        [Required(AllowEmptyStrings = true)]
        public required string RequiredAllowEmptyStringsTrue { get; set; }

        [AllowedValues("foo", "bar")]
        public required string AllowedValues { get; set; }

        [DeniedValues("foo", "bar")]
        public required string DeniedValues { get; set; }

        [Phone]
        public required string PhoneNumber { get; set; }

        [CreditCard]
        public required string CreditCard { get; set; }

        [EmailAddress]
        public required string Email { get; set; }

        [Url]
        public required string Address { get; set; }

        [RegularExpression("^A")]
        public required string StartsWithA { get; set; }

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

    private IModel BuildModel(Action<ModelBuilder> buildAction, bool useRegex, bool isAzureSql)
    {
        var serviceProvider = SqlServerTestHelpers.Instance.CreateContextServices();

        var dbContextOptions = serviceProvider.GetRequiredService<IDbContextOptions>();

        var sqlServerOptionsExtension = dbContextOptions.Extensions
            .Where(o => o.GetType().Name == "SqlServerOptionsExtension")
            .FirstOrDefault();

        sqlServerOptionsExtension!.GetType()
            .GetField("_azureSql", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(sqlServerOptionsExtension, isAzureSql);

        var conventionSet = serviceProvider.GetRequiredService<IConventionSetBuilder>().CreateConventionSet();

        conventionSet.ModelFinalizingConventions.Add(
            new ValidationCheckConstraintConvention(
                new ValidationCheckConstraintOptions { UseRegex = useRegex },
                serviceProvider.GetRequiredService<IRelationalTypeMappingSource>(),
                serviceProvider.GetRequiredService<ISqlGenerationHelper>(),
                serviceProvider.GetRequiredService<IRelationalTypeMappingSource>(),
                serviceProvider.GetRequiredService<IDatabaseProvider>(),
                dbContextOptions));

        var builder = new ModelBuilder(conventionSet);
        buildAction(builder);
        return builder.FinalizeModel();
    }

    private IEntityType BuildEntityType<TEntity>(Action<EntityTypeBuilder<TEntity>>? buildAction = null, bool useRegex = true, bool isAzureSql = false)
        where TEntity : class
    {
        return BuildModel(buildAction is null
                ? b => b.Entity<TEntity>()
                : b => buildAction(b.Entity<TEntity>()),
            useRegex, isAzureSql).GetEntityTypes().Single();
    }

    #endregion
}

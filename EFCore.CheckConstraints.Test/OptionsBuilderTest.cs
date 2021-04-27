using System.Linq;
using EFCore.CheckConstraints.Internal;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EFCore.CheckConstraints.Test
{
    public class OptionsBuilderTest
    {
        [Fact]
        public void DiscriminatorCheckConstraintOptionsTest()
        {
            var optionsBuilder = new DbContextOptionsBuilder();

            optionsBuilder.UseDiscriminatorCheckConstraints();

            var extensions = optionsBuilder.Options.Extensions.ToArray();

            Assert.Single(extensions);

            var options = extensions[0] as CheckConstraintsOptionsExtension;

            Assert.NotNull(options);
            Assert.True(options!.AreDiscriminatorCheckConstraintsEnabled);
            Assert.False(options.AreEnumCheckConstraintsEnabled);
            Assert.False(options.AreValidationCheckConstraintsEnabled);
            Assert.Equal("using check constraints (discriminators)", options.Info.LogFragment);
            Assert.Null(options.ValidationCheckConstraintOptions);
        }

        [Fact]
        public void EnumCheckConstraintOptionsTest()
        {
            var optionsBuilder = new DbContextOptionsBuilder();

            optionsBuilder.UseEnumCheckConstraints();

            var extensions = optionsBuilder.Options.Extensions.ToArray();

            Assert.Single(extensions);

            var options = extensions[0] as CheckConstraintsOptionsExtension;

            Assert.NotNull(options);
            Assert.False(options!.AreDiscriminatorCheckConstraintsEnabled);
            Assert.True(options.AreEnumCheckConstraintsEnabled);
            Assert.False(options.AreValidationCheckConstraintsEnabled);
            Assert.Equal("using check constraints (enums)", options.Info.LogFragment);
            Assert.Null(options.ValidationCheckConstraintOptions);
        }

        [Fact]
        public void ValidationCheckConstraintOptionsTest()
        {
            var optionsBuilder = new DbContextOptionsBuilder();

            optionsBuilder.UseValidationCheckConstraints();

            var extensions = optionsBuilder.Options.Extensions.ToArray();

            Assert.Single(extensions);

            var options = extensions[0] as CheckConstraintsOptionsExtension;

            Assert.NotNull(options);
            Assert.False(options!.AreDiscriminatorCheckConstraintsEnabled);
            Assert.False(options.AreEnumCheckConstraintsEnabled);
            Assert.True(options.AreValidationCheckConstraintsEnabled);
            Assert.Equal("using check constraints (validation)", options.Info.LogFragment);
            Assert.NotNull(options.ValidationCheckConstraintOptions);

            var validationOptions = options.ValidationCheckConstraintOptions;

            Assert.NotNull(validationOptions);
            Assert.Null(validationOptions!.CreditCardRegex);
            Assert.Null(validationOptions.EmailAddressRegex);
            Assert.Null(validationOptions.PhoneRegex);
            Assert.Null(validationOptions.UrlRegex);
            Assert.True(validationOptions.UseRegex);
        }

        [Fact]
        public void ValidationCheckConstraintOptionsNoRegexTest()
        {
            var optionsBuilder = new DbContextOptionsBuilder();

            optionsBuilder.UseValidationCheckConstraints(options => options.UseRegex(false));

            var extensions = optionsBuilder.Options.Extensions.ToArray();

            Assert.Single(extensions);

            var options = extensions[0] as CheckConstraintsOptionsExtension;

            Assert.NotNull(options);
            Assert.False(options!.AreDiscriminatorCheckConstraintsEnabled);
            Assert.False(options.AreEnumCheckConstraintsEnabled);
            Assert.True(options.AreValidationCheckConstraintsEnabled);
            Assert.Equal("using check constraints (validation)", options.Info.LogFragment);
            Assert.NotNull(options.ValidationCheckConstraintOptions);

            var validationOptions = options.ValidationCheckConstraintOptions;

            Assert.NotNull(validationOptions);
            Assert.Null(validationOptions!.CreditCardRegex);
            Assert.Null(validationOptions.EmailAddressRegex);
            Assert.Null(validationOptions.PhoneRegex);
            Assert.Null(validationOptions.UrlRegex);
            Assert.False(validationOptions.UseRegex);
        }

        [Fact]
        public void AllCheckConstraintOptionsTest()
        {
            var optionsBuilder = new DbContextOptionsBuilder();

            optionsBuilder.UseAllCheckConstraints();

            var extensions = optionsBuilder.Options.Extensions.ToArray();

            Assert.Single(extensions);

            var options = extensions[0] as CheckConstraintsOptionsExtension;

            Assert.NotNull(options);
            Assert.True(options!.AreDiscriminatorCheckConstraintsEnabled);
            Assert.True(options.AreEnumCheckConstraintsEnabled);
            Assert.True(options.AreValidationCheckConstraintsEnabled);
            Assert.NotNull(options.ValidationCheckConstraintOptions);

            var validationOptions = options.ValidationCheckConstraintOptions;

            Assert.NotNull(validationOptions);
            Assert.Null(validationOptions!.CreditCardRegex);
            Assert.Null(validationOptions.EmailAddressRegex);
            Assert.Null(validationOptions.PhoneRegex);
            Assert.Null(validationOptions.UrlRegex);
            Assert.True(validationOptions.UseRegex);
        }

        [Fact]
        public void AllCheckConstraintOptionsNoRegexTest()
        {
            var optionsBuilder = new DbContextOptionsBuilder();

            optionsBuilder.UseAllCheckConstraints(options => options.UseRegex(false));

            var extensions = optionsBuilder.Options.Extensions.ToArray();

            Assert.Single(extensions);

            var options = extensions[0] as CheckConstraintsOptionsExtension;

            Assert.NotNull(options);
            Assert.True(options!.AreDiscriminatorCheckConstraintsEnabled);
            Assert.True(options.AreEnumCheckConstraintsEnabled);
            Assert.True(options.AreValidationCheckConstraintsEnabled);
            Assert.NotNull(options.ValidationCheckConstraintOptions);

            var validationOptions = options.ValidationCheckConstraintOptions;

            Assert.NotNull(validationOptions);
            Assert.Null(validationOptions!.CreditCardRegex);
            Assert.Null(validationOptions.EmailAddressRegex);
            Assert.Null(validationOptions.PhoneRegex);
            Assert.Null(validationOptions.UrlRegex);
            Assert.False(validationOptions.UseRegex);
        }
    }
}

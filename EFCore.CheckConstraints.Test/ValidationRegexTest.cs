// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EFCore.CheckConstraints.Internal;
using Xunit;

namespace EFCore.CheckConstraints.Test;

// https://github.com/dotnet/runtime/blob/33dba9518b4eb7fbc487fadc9718c408f95a826c/src/libraries/System.ComponentModel.Annotations/tests/System/ComponentModel/DataAnnotations/PhoneAttributeTests.cs
public class ValidationRegexTest
{
    [Theory]
    [InlineData("425-555-1212")]
    [InlineData("+1 425-555-1212")]
    [InlineData("(425)555-1212")]
    [InlineData("+44 (3456)987654")]
    [InlineData("+777.456.789.123")]
    [InlineData("425-555-1212 x123")]
    [InlineData("425-555-1212 x 123")]
    [InlineData("425-555-1212 ext123")]
    [InlineData("425-555-1212 ext 123")]
    [InlineData("425-555-1212 ext.123")]
    [InlineData("425-555-1212 ext. 123")]
    [InlineData("1")]
    [InlineData("+4+2+5+-+5+5+5+-+1+2++1+2++")]
    [InlineData("425-555-1212    ")]
    [InlineData(" \r \n 1  \t ")]
    [InlineData("1-.()")]
    [InlineData("(425555-1212")]
    [InlineData(")425555-1212")]
    public virtual void Phone_valid(string phone)
        => Assert.Matches(ValidationCheckConstraintConvention.DefaultPhoneRegex, phone);

    [Theory]
    [InlineData("")]
    [InlineData("abcdefghij")]
    [InlineData("425-555-1212 ext 123 ext 456")]
    [InlineData("425-555-1212 x")]
    [InlineData("425-555-1212 ext")]
    [InlineData("425-555-1212 ext.")]
    [InlineData("425-555-1212 x abc")]
    [InlineData("425-555-1212 ext def")]
    [InlineData("425-555-1212 ext. xyz")]
    [InlineData("-.()")]
    [InlineData("ext.123 1")]
    public virtual void Phone_invalid(string phone)
        => Assert.DoesNotMatch(ValidationCheckConstraintConvention.DefaultPhoneRegex, phone);

    [Theory]
    [InlineData("0000000000000000")]
    [InlineData("1234567890123452")]
    [InlineData("  1 2 3 4 5 6 7 8 9 0  1 2 34 5 2    ")]
    [InlineData("--1-2-3-4-5-6-7-8-9-0--1-2-34-5-2----")]
    [InlineData(" - 1- -  2 3 --4 5 6 7 -8- -9- -0 - -1 -2 -3-4- --5-- 2    ")]
    [InlineData("1234-5678-9012-3452")]
    [InlineData("1234 5678 9012 3452")]
    public virtual void CreditCard_valid(string creditCard)
        => Assert.Matches(ValidationCheckConstraintConvention.DefaultCreditCardRegex, creditCard);

    [Theory]
    [InlineData("000%000000000001")]
    [InlineData("1234567890123452a")]
    [InlineData("1234567890123452\0")]
    public virtual void CreditCard_invalid(string creditCard)
        => Assert.DoesNotMatch(ValidationCheckConstraintConvention.DefaultCreditCardRegex, creditCard);

    [Theory]
    [InlineData("someName@someDomain.com")]
    [InlineData("1234@someDomain.com")]
    [InlineData("firstName.lastName@someDomain.com")]
    [InlineData("\u00A0@someDomain.com")]
    [InlineData("!#$%&'*+-/=?^_`|~@someDomain.com")]
    [InlineData("\"firstName.lastName\"@someDomain.com")]
    [InlineData("someName@some~domain.com")]
    [InlineData("someName@some_domain.com")]
    [InlineData("someName@1234.com")]
    [InlineData("someName@someDomain\uFFEF.com")]
    public virtual void EmailAddress_valid(string emailAddress)
        => Assert.Matches(ValidationCheckConstraintConvention.DefaultEmailAddressRegex, emailAddress);

    [Theory]
    [InlineData("0")]
    [InlineData("")]
    [InlineData(" \r \t \n" )]
    [InlineData("@someDomain.com")]
    [InlineData("@someDomain@abc.com")]
    [InlineData("someName")]
    [InlineData("someName@")]
    [InlineData("someName@a@b.com")]
    public virtual void EmailAddress_invalid(string emailAddress)
        => Assert.DoesNotMatch(ValidationCheckConstraintConvention.DefaultEmailAddressRegex, emailAddress);

    [Theory]
    [InlineData("http://foo.bar")]
    [InlineData("https://foo.bar")]
    [InlineData("ftp://foo.bar")]
    public virtual void Url_valid(string url)
        => Assert.Matches(ValidationCheckConstraintConvention.DefaultUrlAddressRegex, url);

    [Theory]
    [InlineData("file:///foo.bar")]
    [InlineData("foo.png")]
    [InlineData("")]
    public virtual void Url_invalid(string url)
        => Assert.DoesNotMatch(ValidationCheckConstraintConvention.DefaultUrlAddressRegex, url);
}
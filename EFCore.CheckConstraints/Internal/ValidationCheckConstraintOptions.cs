// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace EFCore.CheckConstraints.Internal
{
    public class ValidationCheckConstraintOptions : IEquatable<ValidationCheckConstraintOptions>
    {
        public ValidationCheckConstraintOptions()
        {
        }

        public ValidationCheckConstraintOptions(ValidationCheckConstraintOptions copyFrom)
        {
            UseRegex = copyFrom.UseRegex;
            PhoneRegex = copyFrom.PhoneRegex;
            CreditCardRegex = copyFrom.CreditCardRegex;
            EmailAddressRegex = copyFrom.EmailAddressRegex;
            UrlRegex = copyFrom.UrlRegex;
        }

        public bool UseRegex { get; set; }
        public string PhoneRegex { get; set; }
        public string CreditCardRegex { get; set; }
        public string EmailAddressRegex { get; set; }
        public string UrlRegex { get; set; }

        public override bool Equals(object obj)
            => obj is ValidationCheckConstraintOptions other && Equals(other);

        public bool Equals(ValidationCheckConstraintOptions other)
            => other != null
                && UseRegex == other.UseRegex
                && PhoneRegex == other.PhoneRegex
                && CreditCardRegex == other.CreditCardRegex
                && EmailAddressRegex == other.EmailAddressRegex
                && UrlRegex == other.UrlRegex;

        public override int GetHashCode()
            => HashCode.Combine(UseRegex, PhoneRegex, CreditCardRegex, EmailAddressRegex, UrlRegex);
    }
}

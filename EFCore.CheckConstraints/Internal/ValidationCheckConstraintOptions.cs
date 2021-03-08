// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace EFCore.CheckConstraints.Internal
{
    /// <summary>
    ///     Validation check constraint options.
    /// </summary>
    public class ValidationCheckConstraintOptions : IEquatable<ValidationCheckConstraintOptions>
    {
        /// <summary>
        ///     Creates a new <see cref="ValidationCheckConstraintOptions"/> object.
        /// </summary>
        public ValidationCheckConstraintOptions() {}

        /// <summary>
        ///     Creates a new <see cref="ValidationCheckConstraintOptions"/> object
        ///     from a given <see cref="ValidationCheckConstraintOptions"/> object.
        /// </summary>
        /// <param name="copyFrom">
        ///     <see cref="ValidationCheckConstraintOptions"/> object to copy.
        /// </param>
        public ValidationCheckConstraintOptions(ValidationCheckConstraintOptions copyFrom)
        {
            UseRegex = copyFrom.UseRegex;
            PhoneRegex = copyFrom.PhoneRegex;
            CreditCardRegex = copyFrom.CreditCardRegex;
            EmailAddressRegex = copyFrom.EmailAddressRegex;
            UrlRegex = copyFrom.UrlRegex;
        }



        /// <summary>
        ///     <c>true</c>, if validation check constraints based on regular
        ///     expressions should be used; <c>false</c> otherwise.
        /// </summary>
        public bool UseRegex { get; set; } = true;

        /// <summary>
        ///     Regular expression pattern string to be used for validating phone numbers.
        /// </summary>
        public string PhoneRegex { get; set; }

        /// <summary>
        ///     Regular expression pattern string to be used for validating credit card numbers.
        /// </summary>
        public string CreditCardRegex { get; set; }

        /// <summary>
        ///     Regular expression pattern string to be used for validating e-mail addresses.
        /// </summary>
        public string EmailAddressRegex { get; set; }

        /// <summary>
        ///     Regular expression pattern string to be used for validating URLs.
        /// </summary>
        public string UrlRegex { get; set; }



        /// <inheritdoc />
        public override bool Equals(object obj)
            => obj is ValidationCheckConstraintOptions other && Equals(other);

        /// <inheritdoc />
        public bool Equals(ValidationCheckConstraintOptions other)
            => other != null
                && UseRegex == other.UseRegex
                && PhoneRegex == other.PhoneRegex
                && CreditCardRegex == other.CreditCardRegex
                && EmailAddressRegex == other.EmailAddressRegex
                && UrlRegex == other.UrlRegex;

        /// <inheritdoc />
        public override int GetHashCode()
            => HashCode.Combine(UseRegex, PhoneRegex, CreditCardRegex, EmailAddressRegex, UrlRegex);
    }
}

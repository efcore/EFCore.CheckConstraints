// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EFCore.CheckConstraints.Internal;
using JetBrains.Annotations;

namespace EFCore.CheckConstraints
{
    /// <summary>
    ///     Configures validation check constraints.
    /// </summary>
    public class ValidationCheckConstraintOptionsBuilder
    {
        private readonly ValidationCheckConstraintOptions _options = new();

        /// <summary>
        ///     Current validation check constraint configuration.
        /// </summary>
        public virtual ValidationCheckConstraintOptions Options => _options;

        /// <summary>
        ///     Enables or disables creation of check constraints which use regular expressions.
        /// </summary>
        /// <param name="useRegex">
        ///     <c>true</c> if check constraints which use regular expressions should be created; <c>false</c> otherwise.
        ///     <c>true</c> is the default value.
        /// </param>
        /// <returns>
        ///     The current <see cref="ValidationCheckConstraintOptionsBuilder"/> object.
        /// </returns>
        public virtual ValidationCheckConstraintOptionsBuilder UseRegex(bool useRegex)
        {
            _options.UseRegex = useRegex;
            return this;
        }

        /// <summary>
        ///     Sets the regular expression pattern to use when validating phone numbers.
        /// </summary>
        /// <param name="phoneRegex">
        ///     Regular expression pattern string to use when validating phone numbers.
        ///     The default pattern is <c>^[\d\s+-.()]*\d[\d\s+-.()]*((ext\.|ext|x)\s*\d+)?\s*$</c>.
        /// </param>
        /// <returns>
        ///     The current <see cref="ValidationCheckConstraintOptionsBuilder"/> object.
        /// </returns>
        public virtual ValidationCheckConstraintOptionsBuilder UsePhoneRegex(string? phoneRegex)
        {
            _options.PhoneRegex = phoneRegex;
            return this;
        }

        /// <summary>
        ///     Sets the regular expression pattern to use when validating credit card numbers.
        /// </summary>
        /// <param name="creditCardRegex">
        ///     Regular expression pattern string to use when validating credit card numbers.
        ///     The default pattern is <c>^[\d- ]*$</c>.
        /// </param>
        /// <returns>
        ///     The current <see cref="ValidationCheckConstraintOptionsBuilder"/> object.
        /// </returns>
        public virtual ValidationCheckConstraintOptionsBuilder UseCreditCardRegex(string? creditCardRegex)
        {
            _options.CreditCardRegex = creditCardRegex;
            return this;
        }

        /// <summary>
        ///     Sets the regular expression pattern to use when validating e-mail addresses.
        /// </summary>
        /// <param name="emailRegex">
        ///     Regular expression pattern string to use when validating e-mail addresses.
        ///     The default pattern is <c>^[^@]+@[^@]+$</c>.
        /// </param>
        /// <returns>
        ///     The current <see cref="ValidationCheckConstraintOptionsBuilder"/> object.
        /// </returns>
        public virtual ValidationCheckConstraintOptionsBuilder UseEmailRegex(string? emailRegex)
        {
            _options.EmailAddressRegex = emailRegex;
            return this;
        }

        /// <summary>
        ///     Sets the regular expression pattern to use when validating URLs.
        /// </summary>
        /// <param name="urlRegex">
        ///     Regular expression pattern string to use when validating URLs.
        ///     The default pattern is <c>^(http://|https://|ftp://)</c>.
        /// </param>
        /// <returns>
        ///     The current <see cref="ValidationCheckConstraintOptionsBuilder"/> object.
        /// </returns>
        public virtual ValidationCheckConstraintOptionsBuilder UseUrlRegex(string? urlRegex)
        {
            _options.UrlRegex = urlRegex;
            return this;
        }
    }
}

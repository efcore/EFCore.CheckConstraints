// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;

namespace EFCore.CheckConstraints.Internal
{
    /// <summary>
    ///     Sets configuration options for <see cref="Microsoft.EntityFrameworkCore.CheckConstraintsExtensions"/> methods.
    /// </summary>
    public class ValidationCheckConstraintOptionsBuilder
    {
        private readonly ValidationCheckConstraintOptions _options = new ValidationCheckConstraintOptions();


        /// <summary>
        ///     Current validation check constraint configuration.
        /// </summary>
        public virtual ValidationCheckConstraintOptions Options => _options;



        /// <summary>
        ///     Enable or disable creation of regular expression constraints.
        /// </summary>
        /// <param name="useRegex">
        ///     <c>true</c> if regular expression constraints should be created; <c>false</c> otherwise.
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
        ///     Set regular expression pattern to match phone numbers against.
        /// </summary>
        /// <param name="phoneRegex">
        ///     Regular expression pattern string to match phone numbers against.
        ///     The default pattern string is @"^[\d\s+-.()]*\d[\d\s+-.()]*((ext\.|ext|x)\s*\d+)?\s*$".
        /// </param>
        /// <returns>
        ///     The current <see cref="ValidationCheckConstraintOptionsBuilder"/> object.
        /// </returns>
        public virtual ValidationCheckConstraintOptionsBuilder UsePhoneRegex([CanBeNull] string phoneRegex)
        {
            _options.PhoneRegex = phoneRegex;
            return this;
        }

        /// <summary>
        ///     Set regular expression pattern to match credit card numbers against.
        /// </summary>
        /// <param name="creditCardRegex">
        ///     Regular expression pattern string to match credit card numbers against.
        ///     The default pattern string is @"^[\d- ]*$".
        /// </param>
        /// <returns>
        ///     The current <see cref="ValidationCheckConstraintOptionsBuilder"/> object.
        /// </returns>
        public virtual ValidationCheckConstraintOptionsBuilder UseCreditCardRegex([CanBeNull] string creditCardRegex)
        {
            _options.CreditCardRegex = creditCardRegex;
            return this;
        }

        /// <summary>
        ///     Set regular expression pattern to match e-mail addresses against.
        /// </summary>
        /// <param name="emailRegex">
        ///     Regular expression pattern string to match e-mail addresses against.
        ///     The default pattern string is @"^[^@]+@[^@]+$".
        /// </param>
        /// <returns>
        ///     The current <see cref="ValidationCheckConstraintOptionsBuilder"/> object.
        /// </returns>
        public virtual ValidationCheckConstraintOptionsBuilder UseEmailRegex([CanBeNull] string emailRegex)
        {
            _options.EmailAddressRegex = emailRegex;
            return this;
        }

        /// <summary>
        ///     Set regular expression pattern to match URLs against.
        /// </summary>
        /// <param name="urlRegex">
        ///     Regular expression pattern string to match URLs against.
        ///     The default pattern string is @"^(http://|https://|ftp://)".
        /// </param>
        /// <returns>
        ///     The current <see cref="ValidationCheckConstraintOptionsBuilder"/> object.
        /// </returns>
        public virtual ValidationCheckConstraintOptionsBuilder UseUrlRegex([CanBeNull] string urlRegex)
        {
            _options.UrlRegex = urlRegex;
            return this;
        }
    }
}

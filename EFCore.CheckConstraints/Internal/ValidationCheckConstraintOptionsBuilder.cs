// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;

namespace EFCore.CheckConstraints.Internal
{
    public class ValidationCheckConstraintOptionsBuilder
    {
        private readonly ValidationCheckConstraintOptions _options = new ValidationCheckConstraintOptions();

        public virtual ValidationCheckConstraintOptions Options => _options;

        public virtual ValidationCheckConstraintOptionsBuilder UsePhoneRegex([CanBeNull] string phoneRegex)
        {
            _options.PhoneRegex = phoneRegex;
            return this;
        }

        public virtual ValidationCheckConstraintOptionsBuilder UseCreditCardRegex([CanBeNull] string creditCardRegex)
        {
            _options.CreditCardRegex = creditCardRegex;
            return this;
        }

        public virtual ValidationCheckConstraintOptionsBuilder UseEmailRegex([CanBeNull] string emailRegex)
        {
            _options.EmailAddressRegex = emailRegex;
            return this;
        }

        public virtual ValidationCheckConstraintOptionsBuilder UseUrlRegex([CanBeNull] string urlRegex)
        {
            _options.UrlRegex = urlRegex;
            return this;
        }
    }
}

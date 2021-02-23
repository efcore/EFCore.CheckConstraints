using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.CheckConstraints.Internal
{
    /// <summary>
    ///     Entity Framework Core database context options extension
    ///     configuring how table column check constraints will
    ///     be created from data annotations.
    /// </summary>
    public class CheckConstraintsOptionsExtension : IDbContextOptionsExtension
    {
        private DbContextOptionsExtensionInfo _info;
        private bool _enumCheckConstraintsEnabled;
        private bool _discriminatorCheckConstraintsEnabled;
        private ValidationCheckConstraintOptions _validationCheckConstraintsOptions;



        /// <summary>
        ///     Creates a new <see cref="CheckConstraintsOptionsExtension"/> object.
        /// </summary>
        public CheckConstraintsOptionsExtension() {}

        /// <summary>
        ///     Creates a new <see cref="CheckConstraintsOptionsExtension"/> object
        ///     from a given <see cref="CheckConstraintsOptionsExtension"/> object.
        /// </summary>
        /// <param name="copyFrom">
        ///     <see cref="CheckConstraintsOptionsExtension"/> object to copy.
        /// </param>
        protected CheckConstraintsOptionsExtension([NotNull] CheckConstraintsOptionsExtension copyFrom)
        {
            _enumCheckConstraintsEnabled = copyFrom._enumCheckConstraintsEnabled;
            _discriminatorCheckConstraintsEnabled = copyFrom._discriminatorCheckConstraintsEnabled;
            _validationCheckConstraintsOptions = copyFrom._validationCheckConstraintsOptions is null
                ? null
                : new ValidationCheckConstraintOptions(copyFrom._validationCheckConstraintsOptions);
        }



        /// <summary>
        ///     Information/metadata about the Entity Framework Core
        ///     database context options extension, used for logging/debugging.
        /// </summary>
        public virtual DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

        /// <summary>
        ///     Creates a cloned copy from the current
        ///     <see cref="CheckConstraintsOptionsExtension"/> object.
        /// </summary>
        /// <returns>
        ///     A new <see cref="CheckConstraintsOptionsExtension"/> object, cloned
        ///     from the current <see cref="CheckConstraintsOptionsExtension"/> object.
        /// </returns>
        protected virtual CheckConstraintsOptionsExtension Clone() => new CheckConstraintsOptionsExtension(this);



        /// <summary>
        ///     <c>true</c>, if enum check constraints are enabled; <c>false</c> otherwise.
        /// </summary>
        public virtual bool AreEnumCheckConstraintsEnabled => _enumCheckConstraintsEnabled;

        /// <summary>
        ///     <c>true</c>, if discriminator check constraints are enabled; <c>false</c> otherwise.
        /// </summary>
        public virtual bool AreDiscriminatorCheckConstraintsEnabled => _discriminatorCheckConstraintsEnabled;

        /// <summary>
        ///     <c>true</c>, if validation check constraints are enabled; <c>false</c> otherwise.
        /// </summary>
        public virtual bool AreValidationCheckConstraintsEnabled => _validationCheckConstraintsOptions != null;

        /// <summary>
        ///     The currently configured <see cref="ValidationCheckConstraintOptions"/>. May be <c>null</c>.
        /// </summary>
        public virtual ValidationCheckConstraintOptions ValidationCheckConstraintOptions => _validationCheckConstraintsOptions;



        /// <summary>
        ///     Creates a new <see cref="CheckConstraintsOptionsExtension"/> object
        ///     having enum check constraints enabled or disabled.
        /// </summary>
        /// <param name="enumCheckConstraintsEnabled">
        ///     <c>true</c>, if enum check constraints are enabled; <c>false</c> otherwise.
        /// </param>
        /// <returns>
        ///     New <see cref="CheckConstraintsOptionsExtension"/> object,
        ///     having enum check constraints enabled or disabled.
        /// </returns>
        public virtual CheckConstraintsOptionsExtension WithEnumCheckConstraintsEnabled(
            bool enumCheckConstraintsEnabled)
        {
            var clone = Clone();
            clone._enumCheckConstraintsEnabled = enumCheckConstraintsEnabled;
            return clone;
        }

        /// <summary>
        ///     Creates a new <see cref="CheckConstraintsOptionsExtension"/> object
        ///     having discriminator check constraints enabled or disabled.
        /// </summary>
        /// <param name="discriminatorCheckConstraintsEnabled">
        ///     <c>true</c>, if discriminator check constraints are enabled; <c>false</c> otherwise.
        /// </param>
        /// <returns>
        ///     New <see cref="CheckConstraintsOptionsExtension"/> object,
        ///     having discriminator check constraints enabled or disabled.
        /// </returns>
        public virtual CheckConstraintsOptionsExtension WithDiscriminatorCheckConstraintsEnabled(
            bool discriminatorCheckConstraintsEnabled)
        {
            var clone = Clone();
            clone._discriminatorCheckConstraintsEnabled = discriminatorCheckConstraintsEnabled;
            return clone;
        }

        /// <summary>
        ///     Creates a new <see cref="CheckConstraintsOptionsExtension"/> object
        ///     having validation check constraints configured.
        /// </summary>
        /// <param name="validationCheckConstraintsOptions">
        ///     <see cref="ValidationCheckConstraintOptions"/> to be applied to the new
        ///     <see cref="CheckConstraintsOptionsExtension"/> object. Set to <c>null</c>
        ///     to disable validation check constraints.
        /// </param>
        /// <returns>
        ///     New <see cref="CheckConstraintsOptionsExtension"/> object,
        ///     having validation check constraints configured.
        /// </returns>
        public virtual CheckConstraintsOptionsExtension WithValidationCheckConstraintsOptions(
            ValidationCheckConstraintOptions validationCheckConstraintsOptions)
        {
            var clone = Clone();
            clone._validationCheckConstraintsOptions = validationCheckConstraintsOptions;
            return clone;
        }



        /// <summary>
        ///     Checks if all options are valid.
        /// </summary>
        /// <param name="options">
        ///     Collection of Entity Framework Core database context option extensions.
        /// </param>
        public void Validate(IDbContextOptions options) {}


        /// <summary>
        ///     Adds the table column check constraint service to the list of
        ///     Entity Framework Core database services.
        /// </summary>
        /// <param name="services">
        ///     The collection to add services to.
        /// </param>
        public void ApplyServices(IServiceCollection services)
            => services.AddEntityFrameworkCheckConstraints();



        /// <summary>
        ///     Information/metadata about the Entity Framework Core
        ///     database context options extension, used for logging/debugging.
        /// </summary>
        internal sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            private string _logFragment;

            public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension) {}

            private new CheckConstraintsOptionsExtension Extension
                => (CheckConstraintsOptionsExtension)base.Extension;

            public override bool IsDatabaseProvider => false;

            public override string LogFragment
            {
                get
                {
                    if (_logFragment == null)
                    {
                        var builder = new StringBuilder("using check constraints (");
                        var isFirst = true;

                        if (Extension.AreEnumCheckConstraintsEnabled)
                        {
                            builder.Append("enums");
                            isFirst = false;
                        }

                        if (Extension.AreDiscriminatorCheckConstraintsEnabled)
                        {
                            if (!isFirst)
                            {
                                builder.Append(", ");
                            }

                            builder.Append("discriminators");
                            isFirst = false;
                        }

                        if (Extension.AreValidationCheckConstraintsEnabled)
                        {
                            if (!isFirst)
                            {
                                builder.Append(", ");
                            }

                            builder.Append("validation");
                        }

                        builder.Append(')');

                        _logFragment = builder.ToString();
                    }

                    return _logFragment;
                }
            }

            public override long GetServiceProviderHashCode()
                => HashCode.Combine(
                    Extension._enumCheckConstraintsEnabled.GetHashCode(),
                    Extension._discriminatorCheckConstraintsEnabled.GetHashCode(),
                    Extension.ValidationCheckConstraintOptions?.GetHashCode());

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                debugInfo["CheckConstraints:Enums"]
                    = Extension._enumCheckConstraintsEnabled.GetHashCode().ToString(CultureInfo.InvariantCulture);
                debugInfo["CheckConstraints:Discriminators"]
                    = Extension._discriminatorCheckConstraintsEnabled.GetHashCode().ToString(CultureInfo.InvariantCulture);
                debugInfo["CheckConstraints:Validation"]
                    = Extension._validationCheckConstraintsOptions?.GetHashCode().ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}

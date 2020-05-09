using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using JetBrains.Annotations;

namespace EFCore.CheckConstraints.Internal
{
    public class CheckConstraintsOptionsExtension : IDbContextOptionsExtension
    {
        private DbContextOptionsExtensionInfo _info;
        private bool _enumCheckConstraintsEnabled;

        public CheckConstraintsOptionsExtension() {}

        protected CheckConstraintsOptionsExtension([NotNull] CheckConstraintsOptionsExtension copyFrom)
            => _enumCheckConstraintsEnabled = copyFrom._enumCheckConstraintsEnabled;

        public virtual DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

        protected virtual CheckConstraintsOptionsExtension Clone() => new CheckConstraintsOptionsExtension(this);

        public virtual bool AreEnumCheckConstraintsEnabled => _enumCheckConstraintsEnabled;

        public virtual CheckConstraintsOptionsExtension WithEnumCheckConstraintsEnabled(bool enumCheckConstraintsEnabled)
        {
            var clone = Clone();
            clone._enumCheckConstraintsEnabled = enumCheckConstraintsEnabled;
            return clone;
        }

        public void Validate(IDbContextOptions options) {}

        public void ApplyServices(IServiceCollection services)
            => services.AddEntityFrameworkCheckConstraints();

        sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            string _logFragment;

            public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension) {}

            new CheckConstraintsOptionsExtension Extension
                => (CheckConstraintsOptionsExtension)base.Extension;

            public override bool IsDatabaseProvider => false;

            public override string LogFragment
            {
                get
                {
                    if (_logFragment == null)
                    {
                        var builder = new StringBuilder("using check constraints");

                        if (Extension._enumCheckConstraintsEnabled)
                        {
                            builder
                                .Append(" (")
                                .Append("enums")
                                .Append(")");
                        }

                        _logFragment = builder.ToString();
                    }

                    return _logFragment;
                }
            }

            public override long GetServiceProviderHashCode()
                => Extension._enumCheckConstraintsEnabled.GetHashCode();

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
                => debugInfo["CheckConstraints:Enums"]
                    = Extension._enumCheckConstraintsEnabled.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }
    }
}

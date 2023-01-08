using System;
using System.Collections.Generic;
using System.Text;

namespace PX.Generators.DacGenerators.BqlFieldsGeneration
{
    internal class BqlTableInfo : IEquatable<BqlTableInfo>
    {
        public List<string>? BaseClasses { get; init; }
        public string Name { get; init; }
        public string? Namespace { get; init; }

        private string? _fullName;
        public string FullName
        {
            get
            {
                if (_fullName != null)
                    return _fullName;

                var sb = new StringBuilder();
                if (Namespace != null)
                    sb.Append(Namespace).Append('.');
                if (BaseClasses != null)
                {
                    foreach (var baseClass in BaseClasses)
                    {
                        sb.Append(baseClass).Append('+');
                    }
                }
                sb.Append(Name);
                return _fullName = sb.ToString();
            }
        }
        public IReadOnlyCollection<BqlFieldInfo>? Fields { get; init; }

        public bool Equals(BqlTableInfo? other)
        {
            // should check fields?
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(FullName, other.FullName, StringComparison.InvariantCulture);
        }

        public override int GetHashCode()
        {
            return StringComparer.InvariantCulture.GetHashCode(FullName);
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as BqlTableInfo);
        }

        public override string ToString() => FullName;
    }
}
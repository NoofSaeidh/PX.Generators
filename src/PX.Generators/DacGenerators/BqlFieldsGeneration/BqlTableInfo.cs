using System;
using System.Collections.Generic;
using System.Text;

namespace PX.Generators.DacGenerators.BqlFieldsGeneration
{
    internal class BqlTableInfo : IEquatable<BqlTableInfo>
    {
        public string Name { get; init; }
        public string? Namespace { get; init; }
        public string FullName => string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";
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
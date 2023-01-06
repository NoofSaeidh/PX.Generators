using System;

namespace PX.Generators.DacGenerators.BqlFieldsGeneration
{
    internal class BqlFieldInfo : IEquatable<BqlFieldInfo>
    {
        public string Name { get; init; }
        public bool IsHidingBaseClass { get; init; }
        public BqlFieldPropertyType Type { get; init; }

        public bool Equals(BqlFieldInfo? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Name, other.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((BqlFieldInfo) obj);
        }

        public override int GetHashCode()
        {
            return StringComparer.InvariantCultureIgnoreCase.GetHashCode(Name);
        }

        public override string ToString() => $"{Type} {Name}";


        public static bool operator ==(BqlFieldInfo? left, BqlFieldInfo? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BqlFieldInfo? left, BqlFieldInfo? right)
        {
            return !Equals(left, right);
        }
    }
}
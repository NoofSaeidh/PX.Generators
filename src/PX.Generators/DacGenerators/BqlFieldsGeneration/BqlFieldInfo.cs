using System;
using System.Globalization;

namespace PX.Generators.DacGenerators.BqlFieldsGeneration
{
    internal class BqlFieldInfo : IEquatable<BqlFieldInfo>
    {
        public string Name { get; init; }
        public bool IsHidingBaseClass { get; init; }
        public BqlFieldPropertyType Type { get; init; }

        public string ClassName => GetClassName(Name);

        public static string GetClassName(string name)
        {
            return string.IsNullOrEmpty(name)
                ? string.Empty
                : char.ToLowerInvariant(name[0]) + name.Substring(1, name.Length - 1);
        }

        public string? ClassType => BqlFieldClassType.TryMap(Type, out var classType) ? classType : null;

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
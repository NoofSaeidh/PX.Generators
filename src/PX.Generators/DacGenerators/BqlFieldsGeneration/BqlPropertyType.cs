using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PX.Generators.DacGenerators.BqlFieldsGeneration
{
    internal readonly struct BqlPropertyType : IEquatable<BqlPropertyType>
    {
        public string Value { get; }

        private BqlPropertyType(string value) => Value = value;

        private static BqlPropertyType FromType(Type type) => new (type.FullName!);

        public static bool TryParse(string fullName, out BqlPropertyType result)
        {
            return Items.Value.TryGetValue(fullName, out result);
        }

        public static BqlPropertyType String => FromType(typeof(System.String));
        public static BqlPropertyType Int => FromType(typeof(System.Int32));
        public static BqlPropertyType Short => FromType(typeof(System.Int16));
        public static BqlPropertyType Long => FromType(typeof(System.Int64));
        public static BqlPropertyType Double => FromType(typeof(System.Double));
        public static BqlPropertyType Decimal => FromType(typeof(System.Decimal));
        public static BqlPropertyType DateTime => FromType(typeof(System.DateTime));
        public static BqlPropertyType Guid => FromType(typeof(System.Guid));

        public static BqlPropertyType ByteArray => FromType(typeof(System.Byte[]));
        // todo other types


        public bool Equals(BqlPropertyType other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is BqlPropertyType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString() => Value;


        public static bool operator ==(BqlPropertyType left, BqlPropertyType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BqlPropertyType left, BqlPropertyType right)
        {
            return !left.Equals(right);
        }


        private static readonly Lazy<Dictionary<string, BqlPropertyType>> Items = new (() =>
        {
            var result = new Dictionary<string, BqlPropertyType>();
            foreach (var property in typeof(BqlPropertyType).GetProperties(
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
            {
                try
                {
                    var value = (BqlPropertyType) property.GetValue(null);
                    result[value.Value] = value;
                }
                catch (Exception e)
                {
                    Debugger.Break();
                }
            }

            return result;
        });
    }
}
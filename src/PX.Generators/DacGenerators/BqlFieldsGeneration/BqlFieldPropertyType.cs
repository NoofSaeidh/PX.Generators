using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PX.Generators.DacGenerators.BqlFieldsGeneration
{
    internal readonly struct BqlFieldPropertyType : IEquatable<BqlFieldPropertyType>
    {
        public string Value { get; }

        private BqlFieldPropertyType(string value) => Value = value;

        private static BqlFieldPropertyType FromType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return new(type.GenericTypeArguments[0].FullName + '?');

            return new (type.FullName!);
        }

        public static bool TryParse(string fullName, out BqlFieldPropertyType result)
        {
            return Items.Value.TryGetValue(fullName, out result);
        }

        public static BqlFieldPropertyType String => FromType(typeof(System.String));
        public static BqlFieldPropertyType Int => FromType(typeof(System.Int32?));
        public static BqlFieldPropertyType Short => FromType(typeof(System.Int16?));
        public static BqlFieldPropertyType Long => FromType(typeof(System.Int64?));
        public static BqlFieldPropertyType Double => FromType(typeof(System.Double?));
        public static BqlFieldPropertyType Decimal => FromType(typeof(System.Decimal?));
        public static BqlFieldPropertyType DateTime => FromType(typeof(System.DateTime?));
        public static BqlFieldPropertyType Guid => FromType(typeof(System.Guid?));

        public static BqlFieldPropertyType ByteArray => FromType(typeof(System.Byte[]));
        // todo other types


        public bool Equals(BqlFieldPropertyType other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is BqlFieldPropertyType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString() => Value;


        public static bool operator ==(BqlFieldPropertyType left, BqlFieldPropertyType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BqlFieldPropertyType left, BqlFieldPropertyType right)
        {
            return !left.Equals(right);
        }


        private static readonly Lazy<Dictionary<string, BqlFieldPropertyType>> Items = new (() =>
        {
            var result = new Dictionary<string, BqlFieldPropertyType>();
            foreach (var property in typeof(BqlFieldPropertyType).GetProperties(
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
            {
                try
                {
                    var value = (BqlFieldPropertyType) property.GetValue(null);
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
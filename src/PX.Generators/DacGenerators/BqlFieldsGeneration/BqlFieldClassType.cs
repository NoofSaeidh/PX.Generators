using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PX.Generators.DacGenerators.BqlFieldsGeneration
{
    internal readonly struct BqlFieldClassType
    {
        public string Value { get; }
        public BqlFieldPropertyType BqlFieldPropertyType { get; }

        private BqlFieldClassType(string value, BqlFieldPropertyType bqlFieldPropertyType)
        {
            Value = value;
            BqlFieldPropertyType = bqlFieldPropertyType;
        }

        public static bool TryMap(BqlFieldPropertyType bqlFieldPropertyType, out string? bqlFieldClassName)
        {
            if (Items.Value.TryGetValue(bqlFieldPropertyType, out var value))
            {
                bqlFieldClassName = value.Value;
                return true;
            }
            bqlFieldClassName = null;
            return false;
        }

        public static BqlFieldClassType BqlString => new("BqlString", BqlFieldPropertyType.String);
        public static BqlFieldClassType BqlInt => new("BqlInt", BqlFieldPropertyType.Int);
        public static BqlFieldClassType BqlShort => new("BqlShort", BqlFieldPropertyType.Short);
        public static BqlFieldClassType BqlLong => new("BqlLong", BqlFieldPropertyType.Long);
        public static BqlFieldClassType BqlDouble => new("BqlDouble", BqlFieldPropertyType.Double);
        public static BqlFieldClassType BqlDecimal => new("BqlDecimal", BqlFieldPropertyType.Decimal);
        public static BqlFieldClassType BqlDateTime => new("BqlDateTime", BqlFieldPropertyType.DateTime);
        public static BqlFieldClassType BqlGuid => new("BqlGuid", BqlFieldPropertyType.Guid);

        public static BqlFieldClassType BqlByteArray => new("BqlByteArray", BqlFieldPropertyType.ByteArray);
        // todo other types


        public bool Equals(BqlFieldClassType other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is BqlFieldClassType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString() => Value;


        public static bool operator ==(BqlFieldClassType left, BqlFieldClassType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BqlFieldClassType left, BqlFieldClassType right)
        {
            return !left.Equals(right);
        }


        private static readonly Lazy<Dictionary<BqlFieldPropertyType, BqlFieldClassType>> Items = new (() =>
        {
            var result = new Dictionary<BqlFieldPropertyType, BqlFieldClassType>();
            foreach (var property in typeof(BqlFieldClassType).GetProperties(
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
            {
                try
                {
                    var value = (BqlFieldClassType) property.GetValue(null);
                    result[value.BqlFieldPropertyType] = value;
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

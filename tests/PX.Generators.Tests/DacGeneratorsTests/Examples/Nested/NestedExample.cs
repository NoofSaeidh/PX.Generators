using PX.Data.BQL;
using PX.Data;
using System;

namespace PX.Generators.Tests.DacGeneratorsTests.Examples.Nested
{
    public partial class NestedExample
    {
        public partial class Nested : IBqlTable
        {
            [PXDBString]
            public virtual string Field1 { get; set; }

            [PXInt]
            public int? Field2 { get; set; }

            public string NonField { get; set; }

            public abstract class field3 : BqlString.Field<field3> { }

            [PXDBString]
            public virtual string Field3 { get; set; }

            [PXGuid]
            public virtual Guid? Field4 { get; set; }

            [PXShort]
            public virtual short? Field5 { get; set; }
        }

        public partial class Inner
        {
            public partial class InnerNested : IBqlTable
            {
                [PXDBString]
                public virtual string Field1 { get; set; }
            }
        }
    }
}

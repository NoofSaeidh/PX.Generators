using System;
using PX.Data;
using PX.Data.BQL;

namespace PX.Generators.Tests.DacGeneratorsTests.Examples.Inheritance
{
    public partial class InheritanceExample : InheritanceExample_Base
    {
        [PXDBString]
        public override string Field1 { get; set; }

        [PXDBEmail]
        public virtual string Field6 { get; set; }

        public new abstract class field3 : BqlString.Field<field3> { }

        public new abstract class field4 : BqlGuid.Field<field4> { }
    }

    public partial class InheritanceExample_Base : IBqlTable
    {
        public abstract class field1 : BqlString.Field<field1> { }

        public abstract class field2 : BqlInt.Field<field2> { }

        public abstract class field3 : BqlString.Field<field3> { }

        public abstract class field4 : BqlGuid.Field<field4> { }

        public abstract class field5 : BqlShort.Field<field5> { }

        [PXDBString]
        public virtual string Field1 { get; set; }

        [PXInt]
        public int? Field2 { get; set; }

        public string NonField { get; set; }


        [PXDBString]
        public virtual string Field3 { get; set; }

        [PXGuid]
        public virtual Guid? Field4 { get; set; }

        [PXShort]
        public virtual short? Field5 { get; set; }
    }
}
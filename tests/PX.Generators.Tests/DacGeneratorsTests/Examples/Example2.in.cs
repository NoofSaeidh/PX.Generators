using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Data.BQL;

namespace PX.Generators.Tests.Examples
{
	public partial class Example2 : Example2_Base
	{
		[PXDBString]
		public override string Field1 { get; set; }

		[PXDBEmail]
		public virtual string Field6 { get; set; }

		public new abstract class field3 : BqlString.Field<field3> {}
		public new abstract class field4 : BqlGuid.Field<field4> {}
	}

	public partial class Example2_Base : IBqlTable
	{
		public abstract class field1 : PX.Data.BQL.BqlString.Field<field1> { }
		public abstract class field2 : PX.Data.BQL.BqlInt.Field<field2> { }
		public abstract class field3 : BqlString.Field<field3> { }
		public abstract class field4 : PX.Data.BQL.BqlGuid.Field<field4> { }
		public abstract class field5 : PX.Data.BQL.BqlShort.Field<field5> { }

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

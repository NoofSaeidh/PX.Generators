using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Data.BQL;

namespace PX.Generators.Tests.Examples
{
    public partial class Example1 : IBqlTable
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
}
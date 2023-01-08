using System;
using PX.Data;
using PX.Data.BQL;

namespace PX.Generators.Tests.DacGeneratorsTests.Examples.CacheExtension
{
    public partial class CacheExtensionExample : PXCacheExtension<CacheExtensionExample_BqlTable>
    {
        [PXString]
        public string Field1 { get; set; }
        [PXGuid]
        public Guid? Field2 { get; set; }
    }

    public sealed partial class CacheExtensionOfExtensionExample
        : PXCacheExtension<CacheExtensionExample, CacheExtensionExample_BqlTable>
    {
        [PXDBGuid]
        public Guid? Field2 { get; set; }
        
        [PXDBInt]
        public int? Field3 { get; set; }
    }

    public class CacheExtensionExample_BqlTable : IBqlTable
    {
        [PXString]
        public string Field1 { get; set; }
        public abstract class field1 : IBqlField { }
    }
}
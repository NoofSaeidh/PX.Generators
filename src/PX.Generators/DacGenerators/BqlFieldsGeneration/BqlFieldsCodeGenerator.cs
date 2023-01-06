using System;
using System.Collections.Generic;
using System.Text;
using PX.Generators.Common;

namespace PX.Generators.DacGenerators.BqlFieldsGeneration
{
    internal class BqlFieldsCodeGenerator
    {
        public static readonly string Indentation = "\t";
        public static BqlFieldsCodeGenerator Instance { get; } = new();

        public (string? FileName, string? Code) Generate(BqlTableInfo bqlTable)
        {
            if (bqlTable.Fields?.Count is not > 0)
                return default;

            bool fieldAdded = false;
            var cb = new CodeBuilder(Indentation);
            using (string.IsNullOrEmpty(bqlTable.Namespace)
                ? cb.EmptyIndentation
                : cb.AppendLineWithBrackets($"namespace {bqlTable.Namespace}"))
            {
                using (cb.AppendLineWithBrackets($"partial class {bqlTable.Name}"))
                {
                    foreach (var bqlField in bqlTable.Fields)
                    {
                        if (BqlFieldClassType.TryMap(bqlField.Type, out var className) is false)
                            continue;

                        string addNew = bqlField.IsHidingBaseClass ? "new" : string.Empty;
                        cb.AppendLine(
                            $"public static {addNew} class : PX.Data.BQL.{className}.Field<{className}> {{ }}");
                        fieldAdded = true;
                    }
                }

                cb.Append("ha1");
                cb.AppendLine("ha2");
            }

            cb.Append("ha3");
            cb.AppendLine("ha4");
            
            if (fieldAdded is false)
                return default;

            return ($"{bqlTable.FullName}.g.cs", cb.ToString());
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PX.Generators.Common
{
    internal interface ICodeGenerator<in TInput>
    {
        CodeGenerationResult GenerateCode(TInput input, CancellationToken cancellationToken);
    }
}

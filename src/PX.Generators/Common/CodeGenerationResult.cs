using Microsoft.CodeAnalysis.Text;

namespace PX.Generators.Common
{
    public readonly struct CodeGenerationResult
    {
        public static readonly CodeGenerationResult Unsuccessful = default;

        private CodeGenerationResult(string fileName, SourceText code, bool isSuccess)
        {
            FileName  = fileName;
            Code      = code;
            IsSuccess = isSuccess;
        }

        public CodeGenerationResult(string fileName, SourceText code) : this(fileName, code, true) { }
        public string FileName {get;}
        public SourceText Code { get; }
        public bool IsSuccess { get; }
    }
}
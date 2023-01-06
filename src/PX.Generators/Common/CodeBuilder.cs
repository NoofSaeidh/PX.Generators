using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PX.Generators.Common
{
    // not thread safe
    internal class CodeBuilder
    {
        private readonly string _indentation;
        private readonly StringBuilder _builder = new();
        private readonly IndentationScope _scope;
        private readonly NoIndentationScope _noIndentationScope;

        public CodeBuilder(string indentation)
        {
            _indentation        = indentation ?? string.Empty;
            _scope              = new(this);
            _noIndentationScope = new();
        }

        public IDisposable EmptyIndentation => _noIndentationScope;

        public IDisposable Indent(bool addBrackets = false)
        {
            _scope.Indent(addBrackets);
            return _scope;
        }

        public void Append(string code)
        {
            _builder.Append(code);
        }

        public IDisposable AppendWithBrackets(string code)
        {
            Append(code);
            Indent(addBrackets: true);
            return _scope;
        }

        public IDisposable AppendLineWithBrackets(string code)
        {
            AppendLine(code);
            Indent(addBrackets: true);
            AppendIndentation();
            return _scope;
        }

        public void AppendLine(string code)
        {
            _builder.Append(code);
            AppendIndentation();
        }

        public void AppendLine()
        {
            _builder.AppendLine();
            AppendIndentation();
        }

        private void AppendIndentation()
        {
            _builder.AppendLine(_scope.CurrentIndentation);
        }

        public override string ToString()
        {
            return _builder.ToString();
        }


        private class IndentationScope : IDisposable
        {
            private const string ClosingBracket = "}";
            private const string OpeningBracket = "{";
            private readonly CodeBuilder _builder;
            private readonly Stack<(string Indentation, bool AddBracets)> _indentations = new();
            public string? CurrentIndentation { get; private set; } = string.Empty;

            public IndentationScope(CodeBuilder builder)
            {
                _builder = builder;
                _indentations.Push((string.Empty, false));
            }

            public void Indent(bool addBrackets = false)
            {
                if (addBrackets)
                {
                    _builder._builder.Append(CurrentIndentation);
                    _builder._builder.AppendLine(OpeningBracket);
                }

                CurrentIndentation = _builder._indentation.Repeat(_indentations.Count);
                _indentations.Push((CurrentIndentation, addBrackets));
            }

            public void Dispose()
            {
                bool addBrackets = false;
                if (_indentations.Count > 0)
                {
                    (_, addBrackets) = _indentations.Pop();
                }

                (CurrentIndentation, _) = _indentations.Peek();
                
                if (addBrackets)
                {
                    _builder._builder.Append(CurrentIndentation);
                    _builder._builder.AppendLine(ClosingBracket);
                }
            }
        }

        private class NoIndentationScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}
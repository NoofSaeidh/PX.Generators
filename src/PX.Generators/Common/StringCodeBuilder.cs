using System;
using System.Collections.Generic;
using System.Text;

namespace PX.Generators.Common
{
    internal class StringCodeBuilder
    {
        private const string OpenBrace = "{";
        private const string CloseBrace = "}";

        private readonly StringBuilder _builder;
        private readonly string _indentation;
        private readonly Stack<CurrentLevel> _level;
        private readonly Dictionary<int, string> _indentationsByLevel;
        private readonly CodeBlockHandler _codeBlockHandler;


        public StringCodeBuilder(string indentation, int capacity = 0)
        {
            _builder             = new(capacity);
            _indentation         = indentation;
            _indentationsByLevel = new();
            _codeBlockHandler    = new(this);
            _level               = new();
        }

        public IDisposable CodeBlock(bool onNewLine = true)
        {
            _codeBlockHandler.Open(onNewLine);
            return _codeBlockHandler;
        }

        public StringCodeBuilder EmptyLine()
        {
            _builder.AppendLine();
            return this;
        }

        public StringCodeBuilder StartLine()
        {
            var indentation = _level.Peek().Indentation;
            _builder.AppendLine();
            if (string.IsNullOrEmpty(indentation) is false)
                _builder.Append(indentation);
            return this;
        }

        
        public StringCodeBuilder StartLine(string line)
        {
            StartLine();
            _builder.Append(line);
            return this;
        }

        public StringCodeBuilder ConditionalAdd(bool condition, string text)
        {
            if (condition)
            {
                return Add(text);
            }
            return this;
        }

        public StringCodeBuilder Add(string text)
        {
            _builder.Append(text);
            return this;
        }

        public override string ToString()
        {
            return _builder.ToString();
        }

        public void Clear()
        {
            _builder.Clear();
            InitLevels();
        }

        private void InitLevels()
        {
            _level.Clear();
            _level.Push(new(string.Empty));
        }

        private CurrentLevel TryPop()
        {
            if (_level.Count > 1)
                return _level.Pop();
            return _level.Peek();
        }

        private void Push()
        {
            _level.Push(new (GetIndentationByLevel(_level.Count)));
        }

        private string GetIndentationByLevel(int level)
        {
            if (_indentationsByLevel.TryGetValue(level, out var indentation))
                return indentation;

            if (level == 0) return string.Empty;
            char[] indentationArray = new char[level * _indentation.Length];
            for (int index = 0; index < indentationArray.Length; index++)
            {
                indentationArray[index] = _indentation[index % _indentation.Length];
            }

            return _indentationsByLevel[level] = new(indentationArray);
        }

        private readonly struct CurrentLevel
        {
            public CurrentLevel(string indentation)
            {
                Indentation = indentation;
            }

            public string Indentation { get; }
        }

        private class CodeBlockHandler : IDisposable
        {
            private readonly StringCodeBuilder _builder;

            public CodeBlockHandler(StringCodeBuilder builder)
            {
                _builder = builder;
            }

            public IDisposable Open(bool onNewLine)
            {
                if (onNewLine)
                    _builder.StartLine(OpenBrace);
                else
                    _builder.Add(OpenBrace);
                _builder.Push();
                return this;
            }

            public void Close()
            {
                _builder.TryPop();
                _builder.StartLine(CloseBrace);
            }

            public void Dispose()
            {
                Close();
            }
        }
    }
}
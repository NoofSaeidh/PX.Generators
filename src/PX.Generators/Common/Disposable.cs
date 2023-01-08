using System;
using System.Collections.Generic;
using System.Text;

namespace PX.Generators.Common
{
    internal static class Disposable
    {
        public static IDisposable Empty { get; } = new EmptyDisposable();

        private class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace PX.Generators.Common
{
    internal static class StringExtensions
    {
        public static string Repeat(this string text, int count)
            => new StringBuilder(text.Length * count).Insert(0, text, count).ToString();
    }
}
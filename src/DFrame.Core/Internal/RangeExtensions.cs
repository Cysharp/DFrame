using System;
using System.Collections.Generic;
using System.Text;

namespace DFrame.Core.Internal
{
    internal static class RangeExtensions
    {
        internal static void Deconstruct(this Range range, out int count, out int start, out int end)
        {
            start = range.Start.Value;
            end = range.End.Value;
            count = end - start + 1;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace sandbox.common.core
{
    public static class IListExtensions
    {
        public static IEnumerable<T> Slice<T>(this IList<T> list, int startIndex, int count)
        {
            if (startIndex < 0 || startIndex >= list.Count)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }

            if (count < 0 || startIndex + count <= list.Count)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            for(int i = startIndex; i < startIndex + count; i++)
            {
                yield return list[i];
            }
        }
    }
}

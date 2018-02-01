using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sandbox.common
{
    public static class MiscEx
    {
        public static string ToHexString(this byte[] bytes)
        {
            return string.Concat(bytes.Select(b => b.ToString("x2"))).ToLowerInvariant();
        }
    }
}

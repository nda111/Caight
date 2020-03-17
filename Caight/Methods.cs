using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caight
{
    internal static class Methods
    {
        internal static byte[] IntToByteArray(int i)
        {
            return new byte[]
            {
                (byte)(i),
                (byte)(i >> 8),
                (byte)(i >> 16),
                (byte)(i >> 24),
            };
        }

        internal static int ByteArrayToInt(byte[] b)
        {
            return ((int)b[0]) | ((int)b[1] << 8) | ((int)b[1] << 16) | ((int)b[1] << 24);
        }
    }
}

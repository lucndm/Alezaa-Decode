using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alezza.Decode
{
    public static class BytesUtils
    {
        public static byte[] GetBytes(this string str)
        {
            byte[] buffer = new byte[str.Length * 2];
            Buffer.BlockCopy(str.ToCharArray(), 0, buffer, 0, buffer.Length);
            return buffer;
        }

        public static string GetString(this byte[] bytes)
        {
            char[] chArray = new char[bytes.Length / 2];
            Buffer.BlockCopy(bytes, 0, chArray, 0, bytes.Length);
            return (string)new string(chArray);
        }
    }
}

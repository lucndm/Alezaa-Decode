using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;

namespace Alezza.Decode
{
    public static class DigestUtils
    {
        public static string Base64ComputeMD5(string str)
        {
            string str2 = Base64Encode(str);
            return CryptographicBuffer.EncodeToHexString(HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5).HashData(CryptographicBuffer.ConvertStringToBinary(str2, 0)));
        }

        public static string Base64Encode(string plainText)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }

        public static string GetMD5Hash(this string str)
        {
            return CryptographicBuffer.EncodeToHexString(HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5).HashData(CryptographicBuffer.ConvertStringToBinary(str, 0)));
        }
    }
}

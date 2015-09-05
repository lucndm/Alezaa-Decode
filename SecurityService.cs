using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Alezza.Decode
{
    public class SecurityService
    {

        public IBuffer Decrypt(string key, IBuffer data)
        {
            IBuffer iV = GetIV(key);
            return CryptographicEngine.Decrypt(this.GenerateSymmetricKey(key), data, iV);
        }

        public string Encrypt(string key, string content)
        {
            IBuffer data = CryptographicBuffer.ConvertStringToBinary(content, BinaryStringEncoding.Utf8);
            IBuffer buffer2 = this.Encrypt(key, data);
            return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf16BE, buffer2);
        }

        public IBuffer Encrypt(string key, IBuffer data)
        {
            IBuffer iV = GetIV(key);
            return CryptographicEngine.Encrypt(this.GenerateSymmetricKey(key), data, iV);
        }

        private string GenerateKey(string secret)
        {
            return (DigestUtils.Base64ComputeMD5(GetHardwareId()) + "|C841687C7A8CA1C034D75CCED0D5B7ED52443DD3896EF07168F0D011C3F00EBB|" + secret);
        }

        private CryptographicKey GenerateSymmetricKey(string encryptKey)
        {
            CryptographicKey key2;
            SymmetricKeyAlgorithmProvider algorithmProvider = GetAlgorithmProvider();
            IBuffer buffer = CryptographicBuffer.ConvertStringToBinary(encryptKey.Substring(0, 0x40), 0);
            try
            {
                return algorithmProvider.CreateSymmetricKey(buffer);
            }
            catch (ArgumentException)
            {
                key2 = null;
            }
            return key2;
        }

        private static SymmetricKeyAlgorithmProvider GetAlgorithmProvider()
        {
            return SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);
        }

        public string GetEncryptKey()
        {
            return this.GetEncryptKey("");
        }

        public string GetEncryptKey(string secret)
        {
            string key = Setting.EncryptKey;
            return this.Encrypt(key, this.GenerateKey(secret));
        }

        private string GetHardwareId()
        {
//            return UniqueIdCustom;
            return UniqueId;
        }

        private static IBuffer GetIV(string key)
        {
            int length = (int) GetAlgorithmProvider().BlockLength;
            return CryptographicBuffer.ConvertStringToBinary(DigestUtils.Base64ComputeMD5(key).Substring(0, length), 0);
        }

        public string GetXOEncryptKey()
        {
            return this.GetXOEncryptKey("");
        }

        public string GetXOEncryptKey(string secret)
        {
            string key = Setting.XOEncryptKey;
            return this.Encrypt(key, this.GenerateKey(secret));
        }

        public string XODecrypt(string key, string content)
        {
            IBuffer data = CryptographicBuffer.ConvertStringToBinary(content, BinaryStringEncoding.Utf16BE);
            IBuffer buffer2 = this.XODecrypt(key, data);
            return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf16BE, buffer2);
        }

        public IBuffer XODecrypt(string key, IBuffer data)
        {
            return this.XODecrypt(key, data, 0);
        }

        public IBuffer XODecrypt(string key, IBuffer data, int seedStart)
        {
            
            byte[] bytes = key.GetBytes();
            byte[] buffer2 = new byte[data.Length];
            byte[] buffer3 = new byte[data.Length];
            DataReader.FromBuffer(data).ReadBytes(buffer2);
            for (int i = 0; i < buffer3.Length; i++)
            {
                buffer3[i] = (byte) (buffer2[i] ^ bytes[(i + seedStart) % bytes.Length]);
            }
            return CryptographicBuffer.CreateFromByteArray(buffer3);
        }

        public string XOEncrypt(string key, string content)
        {
            IBuffer data = CryptographicBuffer.ConvertStringToBinary(content, BinaryStringEncoding.Utf16BE);
            IBuffer buffer2 = this.XOEncrypt(key, data);
            return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf16BE, buffer2);
        }

        public IBuffer XOEncrypt(string key, IBuffer data)
        {
            byte[] bytes = key.GetBytes();
            byte[] buffer2 = new byte[data.Length];
            byte[] buffer3 = new byte[data.Length];
            DataReader.FromBuffer(data).ReadBytes(buffer2);
            for (int i = 0; i < buffer3.Length; i++)
            {
                buffer3[i] = (byte) (buffer2[i] ^ bytes[i % bytes.Length]);
            }
            return CryptographicBuffer.CreateFromByteArray(buffer3);
        }

        private static string UniqueId
        {
            get
            {
                
                string str = ApplicationData.Current.LocalSettings.Values["UNIQUE_ID"] as string;
                if (string.IsNullOrEmpty(str))
                {
                    str = Guid.NewGuid().ToString();
                    ApplicationData.Current.LocalSettings.Values["UNIQUE_ID"] = str;
                }
                return str;
            }
        }
        
    }
}

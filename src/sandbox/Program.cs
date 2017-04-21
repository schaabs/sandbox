using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sandbox.common;
using System.IO;
using stress.codegen;
using System.Security.Cryptography;
using Xunit;
using System.Threading;
using System.IO.Compression;
using System.Security;
using System.Runtime.InteropServices;

namespace sandbox.temp
{
    static class LocalExtensions
    {
        public static byte[] ToBytes(this string str)
        {
            return UnicodeEncoding.Default.GetBytes(str);
        }

        public static byte[] Transform(this byte[] bytes, ICryptoTransform transform)
        {
            using (var memstream = new MemoryStream())
            using (var cryptStream = new CryptoStream(memstream, transform, CryptoStreamMode.Write))
            {
                cryptStream.Write(bytes, 0, bytes.Length);

                cryptStream.FlushFinalBlock();

                return memstream.ToArray();
            }
        }

        public static string ToUnicodeString(this byte[] bytes)
        {
            return string.Concat(UnicodeEncoding.Default.GetChars(bytes));
        }
    }

    class Program : Sandbox
    {
        static void Main(string[] args)
        {
            sandbox(Run);
        }

        public static void Run()
        {
            var strdata = "The lazy brown fox jumped over the fence again.";

            print(strdata);

            var byteData = strdata.ToBytes();

            var provider1 = new AesCryptoServiceProvider() { Padding = PaddingMode.Zeros };

            var provider2 = new AesCryptoServiceProvider() { Padding = PaddingMode.Zeros };
            provider1.Key = provider2.Key;

            var encryptor1 = provider1.CreateEncryptor();
            var encryptor2 = provider2.CreateEncryptor();

            var decryptor1 = provider1.CreateDecryptor();
            var decryptor2 = provider2.CreateDecryptor();


            var encryptedBytes = byteData.Transform(encryptor1).Transform(encryptor2);

            print(byteData.Transform(encryptor1).ToHexString());
            print(byteData.Transform(encryptor2).ToHexString());

            print(provider2.Key.ToHexString());
            print(provider2.Key.Transform(encryptor1).Transform(decryptor2).ToHexString());

            print(byteData.Transform(encryptor2).Transform(decryptor1).ToUnicodeString());

        }
    }
    
}

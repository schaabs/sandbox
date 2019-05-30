using System;
using System.Security.Cryptography;
using System.Text;

namespace ecsignxplat
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] data = Encoding.Unicode.GetBytes("I got's some data and I'm fixin' to get it signed");

            var ecKey = ECDsa.Create();

            ecKey.GenerateKey(ECCurve.CreateFromFriendlyName("secp256k1"));

            byte[] signature = ecKey.SignData(data, HashAlgorithmName.SHA256);

            Console.WriteLine(BitConverter.ToString(signature).Replace("-", ""));
        }
    }
}

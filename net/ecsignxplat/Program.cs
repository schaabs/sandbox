using System;
using System.Security.Cryptography;
using System.Text;

namespace ecsignxplat
{
    static class Extensions
    {
        public static string ToHex(this byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            byte[] data = new byte[1024];
            new Random(1).NextBytes(data);
            //byte[] data = Encoding.Unicode.GetBytes("I got's some data and I'm fixin' to get it signed'jas'dl;kja;lskgja;lskdgjalskgjal;gkjas;ldkgjas;lgkjasd;glkasjdg;laksjdg;alskdgj;asldkgj;wqlekgj;lekjg;lksj;lgkje;sklgjs;lekgj;alserkjga;lskegjpoespoegpaos'erg;jsa'erpgoaj'se;oga's;erojgas';eorgas';erojgas'eorgjas'oergas'egojase;g");
            var d = "903CE5DAB916DDE73ABED883CDB6EA963D1F4BE8E3826619423BA3FD92EA5952";
            var x = "7C20B1704000CD915B7B95D96AAC479F021CE811250D9D4FE54DA94F74167411";
            var y = "7F897BABDD1395BCD04CA5F17B772F01A22840F3BE6A966DA183C917D1E185AC";
            var xPlatSignature = "E8C84FCB838DD47E9BD9AFA147FD2158B2D611D92EC50946DEB54E895820E33328ACF160B6CE83CEE41A480E922829F091B6A9354438E5CEF28E0CA187BA7258";

            var ecParams = new ECParameters()
            {
                D = HexToBytes(d),
                Q = new ECPoint()
                {
                    X = HexToBytes(x),
                    Y = HexToBytes(y)
                },
                Curve = ECCurve.CreateFromFriendlyName("secp256k1")
            };
            var ecParams2 = new ECParameters()
            {
                D = HexToBytes(d),
                Q = new ECPoint()
                {
                    X = HexToBytes(x),
                    Y = HexToBytes(y)
                },
                Curve = ECCurve.CreateFromFriendlyName("secp256k1")
            };
            var ecKey = ECDsa.Create(ecParams);

            var ecKey2 = ECDsa.Create(ecParams2);

            var expParam = ecKey.ExportParameters(true);
            //ecKey.GenerateKey(ECCurve.CreateFromFriendlyName("secp256k1"));

            byte[] signature = ecKey.SignData(data,0, data.Length, HashAlgorithmName.SHA256);
            
            Console.WriteLine(signature.ToHex());

            Console.WriteLine(ecKey2.VerifyData(data, HexToBytes(xPlatSignature), HashAlgorithmName.SHA256));
        }


        public static byte[] HexToBytes(String hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}

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
    public class PasswordEncryptionProvider
    {
        // Create a new RNGCryptoServiceProvider.
        private RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();

        private const int HEADER_LENGTH = 256;

        public PasswordEncryptionProvider(string password)
        {
            HeaderCryptoProvider = new AesCryptoServiceProvider();

            HeaderCryptoProvider.Padding = PaddingMode.Zeros;

            InitializeHeaderCryptoProvider(password);

            ContentCryptoProvder = new AesCryptoServiceProvider();

            ContentCryptoProvder.Padding = PaddingMode.Zeros;
        }

        private void InitializeHeaderCryptoProvider(string password)
        {
            var bpwd = new PasswordDeriveBytes(password, CreateRandomSalt(7));

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();

            PrintKey(tdes.IV);

            var key_init = bpwd.CryptDeriveKey("TripleDES", "SHA512", 192, tdes.IV);

            var hashProvider = SHA256.Create();
            
            HeaderCryptoProvider.Key = hashProvider.ComputeHash(key_init);
            
            PasswordKeyHash = hashProvider.ComputeHash(HeaderCryptoProvider.Key);
            
            HeaderCryptoProvider.Padding = PaddingMode.Zeros;

            byte[] iv = hashProvider.ComputeHash(PasswordKeyHash);

            Array.Resize(ref iv, HeaderCryptoProvider.BlockSize / 8);

            HeaderCryptoProvider.IV = iv;
        }

        public byte[] PasswordKeyHash { get; private set; }

        public AesCryptoServiceProvider HeaderCryptoProvider { get; private set; }

        public AesCryptoServiceProvider ContentCryptoProvder { get; private set; }

        public async Task EncryptToStreamAsync(Stream input, Stream output, CancellationToken cancel)
        {
            await WriteHeaderToStream(output, cancel);

            await WriteContentToStreamAsync(input, output, cancel);
        }

        public async Task DecryptFromStreamAsync(Stream input, Stream output, CancellationToken cancel)
        {
            await DecryptHeader(input, cancel);

            await DecryptContentFromStreamAsync(input, output, cancel);
        }
        
        public async Task RecryptStreamAsync(string password, Stream stream, CancellationToken cancel)
        {
            await DecryptHeader(stream, cancel);

            InitializeHeaderCryptoProvider(password);

            stream.Seek(0, SeekOrigin.Begin);

            await WriteHeaderToStream(stream, cancel);
        }

        private async Task DecryptContentFromStreamAsync(Stream input, Stream output, CancellationToken cancel)
        {
            using (CryptoStream csDecrypt = new CryptoStream(input, ContentCryptoProvder.CreateDecryptor(), CryptoStreamMode.Read))
            {
                await csDecrypt.CopyToAsync(output);

                await output.FlushAsync();
            }
        }

        private async Task DecryptHeader(Stream input, CancellationToken cancel)
        {
            //read the hash and make sure it matches the password hash
            for (int i = 0; i < PasswordKeyHash.Length; i++)
            {
                if (input.ReadByte() != PasswordKeyHash[i])
                {
                    throw new InvalidOperationException("invalid password");
                }
            }

            //if the hash matches read the encrypted header
            var header = new byte[HEADER_LENGTH];

            await input.ReadAsync(header, 0, HEADER_LENGTH);

            SetContentKeyFromHeader(header);

        }

        private void SetContentKeyFromHeader(byte[] headerBytes)
        {
            var decryptedBytes = new byte[headerBytes.Length];

            using (MemoryStream inStream = new MemoryStream(headerBytes))
            {
                using (CryptoStream decryptStream = new CryptoStream(inStream, HeaderCryptoProvider.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    if(decryptStream.Read(decryptedBytes, 0, decryptedBytes.Length) == decryptedBytes.Length)
                    {
                        var key = new byte[ContentCryptoProvder.Key.Length];

                        var iv = new byte[ContentCryptoProvder.IV.Length];

                        Array.ConstrainedCopy(decryptedBytes, GetKeyHeaderOffset(), key, 0, key.Length);

                        Array.ConstrainedCopy(decryptedBytes, GetIVHeaderOffset(), iv, 0, iv.Length);

                        ContentCryptoProvder.Key = key;

                        ContentCryptoProvder.IV = iv;

                        PrintKey(ContentCryptoProvder.Key);

                        PrintKey(ContentCryptoProvder.IV);
                    }
                }
            }
        }

        private async Task WriteHeaderToStream(Stream stream, CancellationToken cancel)
        {
            await stream.WriteAsync(PasswordKeyHash, 0, PasswordKeyHash.Length, cancel);

            var contentKeyBytes = GetHeader();

            var encryptedContentKeyBytes = new byte[contentKeyBytes.Length];

            using (MemoryStream buffStream = new MemoryStream(encryptedContentKeyBytes))
            {
                using (CryptoStream csEncrypt = new CryptoStream(buffStream, HeaderCryptoProvider.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    await csEncrypt.WriteAsync(contentKeyBytes, 0, contentKeyBytes.Length);    
                }

                await stream.WriteAsync(encryptedContentKeyBytes, 0, encryptedContentKeyBytes.Length);

                await stream.FlushAsync();
            }
        }

        private byte[] GetHeader()
        {
            var entropy = new byte[GetEntropyLength()];

            rand.GetBytes(entropy);

            return entropy.Concat(ContentCryptoProvder.Key.Concat(ContentCryptoProvder.IV)).ToArray();
        }

        private int GetKeyHeaderOffset()
        {
            return GetEntropyLength();
        }

        private int GetIVHeaderOffset()
        {
            return GetKeyHeaderOffset() + ContentCryptoProvder.Key.Length;
        }

        private int GetEntropyLength()
        {
            return HEADER_LENGTH - (ContentCryptoProvder.Key.Length + ContentCryptoProvder.IV.Length);
        }

        private async Task WriteContentToStreamAsync(Stream input, Stream output, CancellationToken cancel)
        {
            Console.WriteLine($"input pos: {input.Position}");

            Console.WriteLine($"output pos: {output.Position}");

            using (CryptoStream csEncrypt = new CryptoStream(output, ContentCryptoProvder.CreateEncryptor(), CryptoStreamMode.Write))
            {
                await input.CopyToAsync(csEncrypt);

                await csEncrypt.FlushAsync();
                
            }
        }
        

        private byte[] CreateRandomSalt(int length)
        {
            // Create a buffer
            byte[] randBytes;

            if (length >= 1)
            {
                randBytes = new byte[length];
            }
            else
            {
                randBytes = new byte[1];
            }
            
            // Fill the buffer with random bytes.
            rand.GetBytes(randBytes);

            // return the bytes.
            return randBytes;
        }

        private static void PrintKey(byte[] key)
        {
            Console.WriteLine($"{key.Length} {string.Concat(key.Select(b => b.ToString("X2")))}");
        }


    }

    class Program : Sandbox
    {

        public static string ConvertToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null)
                throw new ArgumentNullException("securePassword");

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        static void Main(string[] args)
        {
            sandbox(Run);
        }

        public static void Run()
        {
            var inpath = @"D:\temp\crypto\crypto_in.txt";
            var outpath = @"D:\temp\crypto\crypto_encrypted.txt";

            var fileEncryptor = new PasswordEncryptionProvider("password");

            using (var infile = File.OpenRead(inpath))
            {
                using (var outfile = File.OpenWrite(outpath))
                {
                    fileEncryptor.EncryptToStreamAsync(infile, outfile, CancellationToken.None).Wait();
                }
            }

            inpath = @"D:\temp\crypto\crypto_encrypted.txt";
            outpath = @"D:\temp\crypto\crypto_decrypted.txt";

            fileEncryptor = new PasswordEncryptionProvider("password");

            using (var infile = File.OpenRead(inpath))
            {
                using (var outfile = File.OpenWrite(outpath))
                {
                    fileEncryptor.DecryptFromStreamAsync(infile, outfile, CancellationToken.None).Wait();
                }
            }
            //while ((pwd = ReadPassword()).Length > 0)
            //{

            //print();

            //print(pwd);

            //var bpwd = new PasswordDeriveBytes(pwd, CreateRandomSalt(7));

            //TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();

            //var key_init = bpwd.CryptDeriveKey("TripleDES", "SHA512", 192, tdes.IV);

            //var hashProvider = SHA256.Create();

            //var key = hashProvider.ComputeHash(key_init);

            //Console.Write("key: ");
            //PrintKey(key);

            //var hash = hashProvider.ComputeHash(key);

            //Console.Write("hash: ");

            //PrintKey(hash);
            //}
            //    //var args = new DumplingUploadCommandArgs();

            //    //args.Initialize();

            //    //args.WriteHelp();
            //    using (var file = new StreamWriter(@"d:\temp\siminput.csv"))
            //    {
            //        int buildid = 2146;

            //        for (int i = buildid - 32; i <= buildid; i+=3)
            //        {
            //            var line = $"4,7,16,0{i}.00,.001,.0008,.0010,{DateTime.Today - TimeSpan.FromDays(buildid - i)}";

            //            file.WriteLine(line);
            //        }
            //    }
        }

        public static void PrintKey(byte[] key)
        {
            print($"{key.Length} {string.Concat(key.Select(b => b.ToString("X2")))}");
        }



        public static Random rand = new Random();
        
        private class RunThread
        {
            int _threadId;

            public RunThread(int threadId)
            {
                _threadId = threadId;
            }

            public void Start()
            {
                for (int i = 0; i < 100000; i++)
                {
                    if (rand.NextBoolean())
                    {
                        JaggedArray.ReplaceEdge();

                        Console.WriteLine($"{_threadId}: inner");
                    }
                    else
                    {
                        JaggedArray.ReplaceInner();
                        Console.WriteLine($"{_threadId}: edge");
                    }
                }
            }
        }
    }

    public static class JaggedArray
    {
        private const int EDGEARR_MAXSIZE = 1024;
        private const int EDGEARR_MINSIZE = 64;

        private const int INNERARR_MAXSIZE = 128;
        private const int INNERARR_MINSIZE = 64;

        private static Random s_rand = new Random();

        private static object[] s_roots = new object[128];

        static JaggedArray()
        {
            for (int i = 0; i < s_roots.Length; i++)
            {
                s_roots[i] = NextInnerArray();
            }
        }

        [Fact]
        public static void ReplaceEdge()
        {
            //pick a random rooted jagged array
            var inner = (object[])s_roots[s_rand.Next(s_roots.Length)];

            //pick a random edge
            var edgeIx = s_rand.Next(inner.Length);

            var edge = (ChecksumArray)inner[edgeIx];

            //replace it and validate it's checksum
            inner[edgeIx] = NextEdgeArray();

            edge.AssertChecksum();
        }

        [Fact]
        public static void ReplaceInner()
        {
            //pick a random rooted jagged array
            var innerIx = s_rand.Next(s_roots.Length);

            var inner = (object[])s_roots[innerIx];

            //replace and validate all the edge array's checksums
            s_roots[innerIx] = NextInnerArray();

            foreach (ChecksumArray edge in inner)
            {
                edge.AssertChecksum();
            }
        }

        private static object[] NextInnerArray()
        {
            int size = s_rand.Next(INNERARR_MINSIZE, INNERARR_MAXSIZE + 1);

            object[] arr = new object[size];

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = NextEdgeArray();
            }

            return arr;
        }

        private static ChecksumArray NextEdgeArray()
        {
            int size = s_rand.Next(EDGEARR_MINSIZE, EDGEARR_MAXSIZE + 1);

            return new ChecksumArray(s_rand, size);
        }


        private class ChecksumArray
        {
            private int _checksum;
            private int[] _arr;

            public ChecksumArray(Random rand, int size)
            {
                _arr = new int[size];

                for (int i = 0; i < _arr.Length; i++)
                {
                    _arr[i] = rand.Next(int.MinValue, int.MaxValue);

                    _checksum ^= _arr[i];
                }
            }

            public int AssertChecksum()
            {
                int chk = 0;

                for (int i = 0; i < _arr.Length; i++)
                {
                    chk ^= _arr[i];
                }

                Assert.Equal(chk, _checksum);

                return _checksum;
            }

            public int Checksum { get { return _checksum; } }
        }
    }


}

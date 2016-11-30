using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sandbox.common;
using System.IO;
using stress.codegen;
using System.Security.Cryptography;

namespace sandbox.temp
{
    class Program : Sandbox
    {
        static void Main(string[] args)
        {
            sandbox(Run);
        }

        static void Run()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        static async Task RunAsync()
        {
            Random r = new Random();

            using (var sha1a = SHA1.Create())
            using (var fstream = await r.CreateFileAsync())
            using (var cryptStream = new CryptoStream(fstream, sha1a, CryptoStreamMode.Read))
            using (var tempFile = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.DeleteOnClose))
            {
                await cryptStream.CopyToAsync(tempFile);

                await tempFile.FlushAsync();

                print(sha1a.Hash.ToHexString());

                fstream.Position = 0;

                using (var sha1e = SHA1.Create())
                {
                    print(sha1e.ComputeHash(fstream).ToHexString());
                    
                }


            }
        }
    }

}

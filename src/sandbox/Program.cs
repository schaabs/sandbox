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
            var dto = new DateTimeOffset(DateTime.UtcNow);

            print(dto.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'"));

            



            //foreach (var rundir in Directory.EnumerateDirectories(@"\\perfdaddy\clrstress\sandbox\ProjectN\testruns", "*", SearchOption.TopDirectoryOnly).Where(d => Path.GetFileName(d).StartsWith("PN")))
            //{
            //    var i = new DirectoryInfo(rundir);

            //    print(rundir);
            //    print(i.CreationTime);
            //    print(i.CreationTimeUtc);
            //    print(i.LastWriteTime);
            //    print(i.LastWriteTimeUtc);
            //    //Directory.SetLastAccessTime(rundir, i.CreationTime);

            //    foreach (var dir in Directory.EnumerateDirectories(rundir, "*", SearchOption.AllDirectories))
            //    {
            //        var info = new DirectoryInfo(dir);

            //        print(dir);
            //        print(info.CreationTime);
            //        print(info.CreationTimeUtc);
            //        print(info.LastWriteTime);
            //        print(info.LastWriteTimeUtc);
            //        //Directory.SetLastAccessTime(dir, info.CreationTime);
            //        //Directory.SetLastWriteTime(dir, info.CreationTime);
            //    }
            //}
        }
    }
    
}

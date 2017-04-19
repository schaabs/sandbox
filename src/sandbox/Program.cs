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

    class Program : Sandbox
    {
        static void Main(string[] args)
        {
            sandbox(Run);
        }

        public static void Run()
        {
            File.WriteAllText(@"d:\temp\readwrite.txt", "Sweet One");

            using (var writeHandle = File.Open(@"d:\temp\readwrite.txt", FileMode.Open, FileAccess.Write, FileShare.Read))
            {
                using (var readHandle = File.Open(@"d:\temp\readwrite.txt", FileMode.Open, FileAccess.Read, FileShare.Write))
                {
                    byte[] buff = UnicodeEncoding.GetEncoding(0).GetBytes("Beat One ".ToArray());

                    for (int i = 0; i < readHandle.Length + buff.Length; i++)
                    {
                        byte currRead = (byte)readHandle.ReadByte();


                        writeHandle.WriteByte(buff[i % buff.Length]);

                        buff[i % buff.Length] = currRead;
                    }

                }
            }
        }
    }
    
}

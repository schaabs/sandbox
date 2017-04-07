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
            Task[] tasks = new Task[8];

            for(int i = 0; i < 8; i++)
            {
                var t = new RunThread(i);

                tasks[i] = Task.Run((Action)t.Start);
            }

            Task.WaitAll(tasks);
            
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

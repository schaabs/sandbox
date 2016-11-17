using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sandbox.tools;
using System.IO;

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
             Console.WriteLine("Hello World")
        }
    }

}

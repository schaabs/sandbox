using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sandbox.tools;
using System.IO;
using stress.codegen;

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
            var lti = new LoadTestInfo("")
            {
                SourceDirectory = @"d:\temp\sandbox\out",
                TestName = "HelloWorld_0001",
                EnvironmentVariables = new Dictionary<string, string>()
                {
                    { "Foo",  "1" },
                    { "Bar",  "3" }
                },
                SuiteConfig = new LoadSuiteConfig() { Host = "corerun" },

            };



            var shgen = new ExecutionFileGeneratorLinux();

            shgen.GenerateSourceFile(lti);
        }
    }

}

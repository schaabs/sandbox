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
            sandbox(RunForked);
        }

        static void Run()
        {
            int FORK_COUNT = 10;

            var forkTasks = new Task[FORK_COUNT];

            for (int i = 0; i < FORK_COUNT; i++)
            {
                forkTasks[i] = CopyFileAsync(@"d:\temp\stress.log", $@"d:\temp\stress0{i}.log");
            }

            Task.WaitAll(forkTasks);

        }
        static void RunForked()
        {
            int FORK_COUNT = 10;

            var forkTasks = new Task[FORK_COUNT];

            using (var file = File.OpenRead(@"d:\temp\stress.log"))
            {
                Stream[] forks = file.Fork(FORK_COUNT);

                for (int i = 0; i < FORK_COUNT; i++)
                {
                    forkTasks[i] = CopyToFileAsync(forks[i], $@"d:\temp\stress1{i}.log", 8 * 1024);
                }

                Task.WaitAll(forkTasks);
            }
        }


        private static async Task CopyFileAsync(string srcPath, string destPath)
        {
            using (var src = File.OpenRead(srcPath))
            using (var dest = File.OpenWrite(destPath))
            {
                await src.CopyToAsync(dest);
            }
        }

        private static async Task CopyToFileAsync(Stream stream, string path, int buffSize)
        {
            using(stream)
            using (var file = File.OpenWrite(path))
            {
                var buff = new byte[buffSize];

                var rBytes = 0;
                var cByte = 0;
                

                while ((rBytes = await stream.ReadAsync(buff, 0, buffSize)) != 0)
                {
                    cByte += rBytes;

                    await file.WriteAsync(buff, 0, rBytes);
                }

                await file.FlushAsync();
            }
        }
    }

}

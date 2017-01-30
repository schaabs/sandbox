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

namespace sandbox.temp
{
    class FinalizeReportingObject
    {
        private static int s_nextid;
        private static object s_reportlock = new object();
        public static TextWriter s_finalizeout;
        public static TextWriter s_allocout;

        private int _id;

        public FinalizeReportingObject()
        {
            _id = Interlocked.Increment(ref s_nextid);

            if (s_allocout != null)
            {
                lock (s_reportlock)
                {
                    s_allocout.WriteLine(_id.ToString("X8"));
                }
            }
        }

        ~FinalizeReportingObject()
        {
            if(s_finalizeout != null)
            {
                lock (s_reportlock)
                {
                    s_finalizeout.WriteLine(_id.ToString("X8"));
                }
            }
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
            //var args = new DumplingUploadCommandArgs();

            //args.Initialize();

            //args.WriteHelp();

            using (var filealloc = File.CreateText(@"d:\temp\finalizerAlloc3.txt"))
            using (var filefinal = File.CreateText(@"d:\temp\finalizerRun3.txt"))
            {
                FinalizeReportingObject.s_finalizeout = filefinal;
                FinalizeReportingObject.s_allocout = filealloc;

                var timoutSource = new CancellationTokenSource(5000);

                Task[] allocTasks = new Task[2];

                allocTasks[0] = Task.Run(() => AllocateLoop(() => new FinalizeReportingObject(), timoutSource.Token));
                allocTasks[1] = Task.Run(() => AllocateLoop(() => new object(), timoutSource.Token));


                Task.WaitAll(allocTasks);

                GC.Collect(2, GCCollectionMode.Forced, true);

                GC.WaitForPendingFinalizers();
            }
                
        }
        

        public static void AllocateLoop(Func<object> allocation, CancellationToken token)
        {
            object[][] jaggedArr = new object[100][];

            while (!token.IsCancellationRequested)
            {
                for (int i = 0; i < jaggedArr.Length && !token.IsCancellationRequested; i++)
                {
                    jaggedArr[i] = new object[2000];

                    for(int j = 0; j < jaggedArr[i].Length && !token.IsCancellationRequested; j++)
                    {
                        jaggedArr[i][j] = new FinalizeReportingObject();
                    }
                }
            }

            jaggedArr = null;
        }
    }

    public abstract class DumplingArgs : ArgParser
    {
        public DumplingArgs(string command) : base(command)
        {
            CommandPrefix = "dumpling";
        }

        public Flag Verbose { get; private set; } = new Flag() { Name = "verbose", Default = false, Help = "indicates that  all critical, standard, and diagnostic messages should be output" };

        public Flag Squelch { get; private set; } = new Flag() { Name = "squelch", Default = false, Help = "indicates that only critical messages should be ouput" };

        public Flag NoPrompt { get; private set; } = new Flag() { Name = "noprompt", Default = false, Help = "suppress prompts for user input" };

        public Arg<string> LogPath { get; private set; } = new Arg<string>() { Name = "logpath", Help = "the path to a log file for appending messge output" };

        public Arg<string> Url { get; private set; } = new Arg<string>() { Name = "url", Help = "url of the dumpling service for the connected client" };

        public Arg<string> ConfigPath { get; private set; } = new Arg<string>() { Name = "configpath", Help = "path to the saved dumpling client configuration file" };

        public Arg<string> DbgPath { get; private set; } = new Arg<string> { Name = "dbgpath", Help = "path to debugger to be used by the dumpling client for debugging and triage" };
    }

    public class DumplingConfigCommandArgs : DumplingArgs
    {
        public DumplingConfigCommandArgs() : base("config")
        {
        }

        public Choice<string> Action { get; private set; } = new Choice<string>() { Position = 0, Choices = new string[] { "dump", "save", "clear" }, Required = true, Help = "specifies the action to take on the dumpling client config" };
    }

    public class DumplingUploadCommandArgs : DumplingArgs
    {
        public DumplingUploadCommandArgs() : base("upload")
        {
        }

        public Arg<string> DumpPath { get; private set; } = new Arg<string> { Name = "dumppath", Help = "path to the dumpfile to be uploaded" };

        public Arg<string> DisplayName { get; private set; } = new Arg<string> { Name = "displayname", Help = "the name to be displayed in reports for the uploaded dump.  This argument is ignored unless --dumppath is specified" };

        public VArg<string> IncludePaths { get; private set; } = new VArg<string> { Name = "incpaths", Help = "paths to files or directories to be included in the upload. Note: directories will recursively include all subdirectories." };
        }

}

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
            var args = new DumplingUploadCommandArgs();

            args.Initialize();
            
            args.WriteHelp();
            
        }
        

        public static async Task RunAsync()
        {
            await Task.CompletedTask;
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace cypher
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                Console.WriteLine("invalid arguments");

                PrintUsage();

                return;
            }

            var action = args[0].ToLower();

            var path = args[1];

            var editor = args.Length > 2 ? args[2] : "notepad.exe";

            if (!File.Exists(path))
            {
                Console.WriteLine("file not found");

                PrintUsage();

                return;
            }

            try
            {
                switch (action)
                {
                    case "encrypt":
                        EncryptFile(path);
                        break;
                    case "decrypt":
                        DecryptFile(path);
                        break;
                    case "open":
                        OpenFile(path, editor);
                        break;
                    default:
                        Console.WriteLine("invalid arguments");

                        PrintUsage();

                        return;
                }
            }
            catch (InvalidPasswordException)
            {
                Console.WriteLine("invalid password");

                return;
            }
        }

        static void EncryptFile(string path)
        {
            var pwd = PromptPassword("Password:");

            if (pwd != PromptPassword("Confirm Password:"))
            {
                Console.WriteLine("passwords do not match");

                throw new InvalidPasswordException();
            }

            var crypto = new PasswordEncryptionProvider(pwd);

            using (var writeHandle = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.Read))
            using (var readHandle = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Write))
            {
                crypto.EncryptFileAsync(writeHandle, readHandle, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        static void DecryptFile(string path)
        {
            var pwd = PromptPassword("Password:");

            var crypto = new PasswordEncryptionProvider(pwd);

            using (var writeHandle = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.Read))
            using (var readHandle = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Write))
            {
                crypto.DecryptFileAsync(readHandle, writeHandle, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        static void OpenFile(string path, string editorPath)
        {
            var pwd = PromptPassword("Password:");

            using (var decryptFile = new DecryptionFile(path, pwd))
            {
                var proc = Process.Start(editorPath, decryptFile.DecryptedPath);

                proc.WaitForExit();
            }
        }


        static void PrintUsage()
        {
            Console.WriteLine("USAGE: filecrypto (ENCRYPT|DECRYPT|OPEN) <inpath> [outpath]");
        }


        static string PromptPassword(string prompt)
        {
            Console.Write(prompt + " ");

            var pwd = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);

                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.Remove(pwd.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    pwd.Append(i.KeyChar);
                }
            }
            Console.WriteLine();

            return pwd.ToString();
        }

    }
}

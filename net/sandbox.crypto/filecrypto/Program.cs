using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace filecrypto
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
                    case "recrypt":
                        RecryptFile(path);
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


            Console.WriteLine("success");
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

            crypto.EncryptFileAsync(path, CancellationToken.None).GetAwaiter().GetResult();
        }

        static void DecryptFile(string path)
        {
            var pwd = PromptPassword("Password:");

            var crypto = new PasswordEncryptionProvider(pwd);

            crypto.DecryptFileAsync(path, CancellationToken.None).GetAwaiter().GetResult();
        }

        static void RecryptFile(string path)
        {
            var pwd = PromptPassword("Old Password:");

            var newPwd = PromptPassword("New Password:");

            if (newPwd != PromptPassword("Confirm New Password:"))
            {
                Console.WriteLine("passwords do not match");

                throw new InvalidPasswordException();
            }

            var crypto = new PasswordEncryptionProvider(pwd);
            
            crypto.RecryptFileAsync(path, newPwd, CancellationToken.None).GetAwaiter().GetResult();
        }


        static void PrintUsage()
        {
            Console.WriteLine("USAGE: filecrypto (ENCRYPT|DECRYPT|RECRYPT) <inpath> [outpath]");
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

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
            if(args.Length < 2 || args.Length > 3)
            {
                Console.WriteLine("invalid arguments");

                PrintUsage();

                return;
            }

            var action = args[0].ToLower();

            var inFile = args[1];

            if(!File.Exists(inFile))
            {
                Console.WriteLine("file not found");

                PrintUsage();

                return;
            }

            var outFile = args.Length == 3 ? args[2] : null;

            using (Stream output = (outFile == null) ? (Stream)new MemoryStream() : (Stream)File.Open(outFile, FileMode.Create, FileAccess.Write))
            {
                using (Stream input = File.Open(inFile, FileMode.Open, FileAccess.ReadWrite))
                {
                    try
                    {
                        switch (action)
                        {
                            case "encrypt":
                                EncryptFile(input, output);
                                break;
                            case "decrypt":
                                DecryptFile(input, output);
                                break;
                            case "recrypt":
                                RecryptFile(input);
                                break;
                            default:
                                Console.WriteLine("invalid arguments");

                                PrintUsage();

                                return;
                        }
                    }
                    catch(InvalidPasswordException)
                    {
                        Console.WriteLine("invalid password");

                        return;
                    }
                }

                //if the output file is null copy the memory stream back to the input file.
                if(outFile == null)
                {
                    using (var file = File.Open(inFile, FileMode.Create, FileAccess.Write))
                    {
                        output.CopyTo(file);

                        file.Flush();
                    }
                }
            }

            Console.WriteLine("success");
        }

        static void EncryptFile(Stream input, Stream output)
        {
            var pwd = PromptPassword(true);

            if (pwd == null)
            {
                Console.WriteLine("passwords do not match");

                return;
            }

            var crypto = new PasswordEncryptionProvider(pwd);

            crypto.EncryptToStreamAsync(input, output, CancellationToken.None).GetAwaiter().GetResult();
        }

        static void DecryptFile(Stream input, Stream output)
        {
            var pwd = PromptPassword(false);

            var crypto = new PasswordEncryptionProvider(pwd);

            crypto.DecryptFromStreamAsync(input, output, CancellationToken.None).GetAwaiter().GetResult();
        }

        static void RecryptFile(Stream stream)
        {
            var pwd = PromptPassword(false);

            var newPwd = PromptPassword(true);

            var crypto = new PasswordEncryptionProvider(pwd);

            if (pwd == null)
            {
                Console.WriteLine("passwords do not match");

                return;
            }

            crypto.RecryptStreamAsync(newPwd, stream, CancellationToken.None).GetAwaiter().GetResult();
        }


        static void PrintUsage()
        {
            Console.WriteLine("USAGE: filecrypto (ENCRYPT|DECRYPT|RECRYPT) <inpath> [outpath]");
        }


        static string PromptPassword(bool confirm)
        {
            Console.Write("Password: ");

            var pwd = ReadPassword();

            if (confirm)
            {
                Console.Write("Confirm Password: ");

                if (pwd != ReadPassword())
                {
                    pwd = null;
                }
            }

            return pwd;
        }

        static string ReadPassword()
        {
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
            return pwd.ToString();

        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sandbox.common
{
    public class Sandbox
    {
        private static readonly object s_console_lock = new object();

        public static void sandbox(Action action)
        {
            DateTime startTime = DateTime.Now;

            Intro(startTime);

            DateTime endTime;

            try
            {
                action();
            }
            catch (Exception e)
            {
                PrintUnhandledException(e);
            }

            endTime = DateTime.Now;

            Outro(endTime, endTime - startTime);
        }

        public static void print()
        {
            print(string.Empty);
        }

        public static void print(string str)
        {
            lock (s_console_lock)
            {
                Console.WriteLine(str);
            }
        }

        public static void print(string format, params object[] args)
        {
            print(string.Format(format, args));
        }

        public static void print(object output)
        {
            if (output == null)
            {
                output = "NULL";
            }

            print(output.ToString());
        }

        public static string prompt(string prompt)
        {
            lock (s_console_lock)
            {
                Console.Write(prompt + ">");

                return Console.ReadLine();
            }
        }

        private static void sbout(params string[] s)
        {
            lock (s_console_lock)
            {
                ConsoleColor origBg = Console.BackgroundColor;
                ConsoleColor origFg = Console.ForegroundColor;

                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.Yellow;

                foreach (string str in s)
                {
                    print(str);
                }

                Console.BackgroundColor = origBg;
                Console.ForegroundColor = origFg;
            }
        }

        private static void PrintUnhandledException(Exception e)
        {
            sbout(string.Empty, "~    !!!Unhandled Exception!!!    ~", string.Empty, e.ToString());
        }

        private static void PrintStart(DateTime start)
        {
            sbout(string.Format("~                       begin time {0:HH:mm:ss.fff}                  ~", start));
        }

        private static void Outro(DateTime end, TimeSpan runTime)
        {
            string[] credits = new string[] {
                "",
                "~----------------------------------------------------------------~",
                "~                                                                ~",
                string.Format("~                      end time   {0:HH:mm:ss.fff}                   ~", end),
                "~                                                                ~",
                string.Format("~                      Runtime: {0}               ~", runTime.ToString("G")),
                "~                                                                ~",
                "~----------------------------------------------------------------~",
            };

            sbout(credits);

            Console.Write("enter to continue");

            Console.Read();

        }
        private static void Intro(DateTime start)
        {
            string[] intro = new string[] {
                "~----------------------------------------------------------------~",
                "~                          Sandbox v4.0                          ~",
                "~                                                                ~",
                "~                      Now with 96% less urine!                  ~",
                "~                                                                ~",
                string.Format("~                      begin time {0:HH:mm:ss.fff}                   ~", start),
                "~                                                                ~",
                "~----------------------------------------------------------------~",
                "",
            };

            sbout(intro);
        }
    }
}

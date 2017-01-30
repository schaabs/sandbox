using sandbox.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace sandbox.netcore
{
    class Program : Sandbox
    {
        static void Main(string[] args)
        {
            sandbox(Run);
        }

        public static void Run()
        {
            foreach (var mi in typeof(Program).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (var attrData in CustomAttributeData.GetCustomAttributes(mi).Where(a => a.AttributeType == typeof(ExecAttribute)))
                {
                    if (attrData.ConstructorArguments.Count == 1 && attrData.ConstructorArguments[0].Value is ICollection<CustomAttributeTypedArgument>)
                    {
                        print(mi.Name);

                        var vargs = ToObjArray(attrData.ConstructorArguments[0].Value as ICollection<CustomAttributeTypedArgument>);

                        mi.Invoke(null, vargs);
                    }

                }

                print();
            }

            print("Hello World");
        }

        private static object[] ToObjArray(ICollection<CustomAttributeTypedArgument> arguments)
        {
            var objArr = new object[arguments.Count];

            int i = 0;

            foreach(var typedArg in arguments)
            {
                objArr[i++] = typedArg.Value;
            }

            return objArr;
        }

        [Exec("key1", "val1")]
        [Exec("key2", "val2")]
        public static void Foo(string key, string value)
        {
            print($"Foo Says \"{key}={value}\"");
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    class ExecAttribute : Attribute
    {
        public ExecAttribute(params object[] arguments) { }
    }
    
}
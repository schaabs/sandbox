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
using System.IO.Compression;
using System.Security;
using System.Runtime.InteropServices;
using System.Reflection;

namespace sandbox.temp
{
    static class LocalExtensions
    {
        public static byte[] ToBytes(this string str)
        {
            return UnicodeEncoding.Default.GetBytes(str);
        }

        public static byte[] Transform(this byte[] bytes, ICryptoTransform transform)
        {
            using (var memstream = new MemoryStream())
            using (var cryptStream = new CryptoStream(memstream, transform, CryptoStreamMode.Write))
            {
                cryptStream.Write(bytes, 0, bytes.Length);

                cryptStream.FlushFinalBlock();

                return memstream.ToArray();
            }
        }

        public static string ToUnicodeString(this byte[] bytes)
        {
            return string.Concat(UnicodeEncoding.Default.GetChars(bytes));
        }
    }
    public class Foo
    {
        public T ThrowPlainFoo<T>(T foo)
        {
            return foo;
        }
    }

    /// <summary>
    /// Class Foo
    /// </summary>
    /// <typeparam name="T">Type of Foo</typeparam>
    public class Foo<T>
    {
        /// <summary>
        /// Inner class foo
        /// </summary>
        /// <typeparam name="X">Foo innards</typeparam>
        public class InnerFoo<X>
        {

            /// <summary>
            /// FooXY Summary
            /// </summary>
            /// <typeparam name="Y">Type of inner inner foo</typeparam>
            /// <param name="foo">foo to get inside</param>
            /// <returns>inside out foo</returns>
            public X FooXY<Y>(Y foo) where Y : X
            {
                return (X)foo;
            }

        }

        /// <summary>
        /// Make a Foo
        /// </summary>
        public Foo()
        {

        }

        /// <summary>
        /// Make a FooDoo
        /// </summary>
        /// <param name="foo">foodoo</param>
        public Foo(T foo)
        {

        }

        /// <summary>
        /// Makes  a FooDoo
        /// </summary>
        public T MakeFoo()
        {
            return default(T);
        }

        /// <summary>
        /// Throws some FooDoo back at you
        /// </summary>
        /// <param name="foo">FooDoo to sling</param>
        /// <returns>The FooDoo that you do</returns>
        public T ThrowFoo(T foo)
        {
            return foo;
        }

        /// <summary>
        /// Back to you with the foo
        /// </summary>
        /// <typeparam name="U">U Foo</typeparam>
        /// <param name="foo">foo for you</param>
        /// <returns>The good old foodoo</returns>
        public T FooU<U>(U foo) where U:T
        {
            return foo;
        }

        /// <summary>
        /// Choose From Foos
        /// </summary>
        /// <param name="foos">the foos to choose</param>
        /// <returns>the chosen foo</returns>
        public T ThrowAFoo(List<T> foos)
        {
            return foos[0];
        }

        /// <summary>
        /// Choose From Foos
        /// </summary>
        /// <param name="foos">the foos to choose</param>
        /// <returns>the chosen foo</returns>
        public T ThrowAFooKey<U>(List<KeyValuePair<T, U>> foos)
        {
            return foos[0].Key;
        }

        /// <summary>
        /// Out of the Foo
        /// </summary>
        /// <param name="foo">foo in</param>
        /// <param name="fooOut">foo out</param>
        public void OutAFoo(T foo, out List<T> fooOut)
        {
            fooOut = new List<T>() { foo };
        }

        /// <summary>
        /// Ref the Foo
        /// </summary>
        /// <param name="foo">foo in</param>
        /// <param name="fooRef">foo out</param>
        public void RefAFoo(T foo, ref T fooRef)
        {
            fooRef = foo;
        }

        /// <summary>
        /// Foo Reflector
        /// </summary>
        /// <param name="i">identity</param>
        /// <returns>FooDoo refletion</returns>
        T this[T i, string s]
        {
            get
            {
                return i;
            }
        }
        
    }

    class Program : Sandbox
    {
        static void Main(string[] args)
        {
            sandbox(Run);
        }

        public static string BuildXDocId(MemberInfo mInfo)
        {
            var buff = new StringBuilder();

            //append the prefix and the namespace
            buff.Append(GetXDocIdPrefix(mInfo));

            var type = mInfo as Type ?? mInfo.DeclaringType;

            buff.Append(type.FullName.Replace('+', '.'));

            var typeGenArgs = type.GetGenericArguments().Select(t => t.Name).ToArray();

            if (!(mInfo is Type))
            {
                var ctorInfo = mInfo as ConstructorInfo;
                var methInfo = mInfo as MethodInfo;
                var propInfo = mInfo as PropertyInfo;

                string[] mGenArgs = new string[] { };
                ParameterInfo[] mParams = new ParameterInfo[] { };

                buff.Append('.');

                if (ctorInfo != null)
                {
                    buff.Append('#');
                }

                buff.Append(mInfo.Name);

                if (methInfo != null)
                {
                    mGenArgs = methInfo.GetGenericArguments().Select(t => t.Name).ToArray();
                    mParams = methInfo.GetParameters();

                    if (mGenArgs.Length > 0)
                    {
                        buff.Append("``");

                        buff.Append(mGenArgs.Length);
                    }
                }
                if (propInfo != null)
                {
                    mParams = propInfo.GetIndexParameters();
                }

                if(mParams.Length > 0)
                {
                    buff.Append("(");

                    buff.Append(string.Join(",", mParams.Select(pInfo => GetParameterTypeString(pInfo.ParameterType, typeGenArgs, mGenArgs))));

                    buff.Append(")");
                }
                
            }
            return buff.ToString();
        }

        private static string GetParameterTypeString(Type paramType, string[] typeGenArgs, string[] memberGenArgs)
        {
            var typeStr = paramType.FullName;

            if(typeStr == null)
            {
                int index = -1;

                if(paramType.IsByRef)
                {
                    typeStr = GetParameterTypeString(paramType.GetElementType(), typeGenArgs, memberGenArgs) + "@";
                }
                else if (paramType.IsGenericParameter)
                {
                    if ((index = Array.IndexOf(typeGenArgs, paramType.Name)) >= 0)
                    {
                        typeStr = $"`{index}";
                    }
                    else if ((index = Array.IndexOf(memberGenArgs, paramType.Name)) >= 0)
                    {
                        typeStr = $"``{index}";
                    }
                }
                else if (paramType.IsGenericType)
                {
                    var genArgsStr = string.Join(",", paramType.GetGenericArguments().Select(t => GetParameterTypeString(t, typeGenArgs, memberGenArgs)));
                    typeStr = $"{paramType.Name.Split('`')[0]}{{{genArgsStr}}}";
                }
            }

            return typeStr;
        }

        private static string GetXDocIdPrefix(MemberInfo mInfo)
        {
            switch(mInfo.MemberType)
            {
                case MemberTypes.TypeInfo:
                case MemberTypes.NestedType:
                    return "T:";
                case MemberTypes.Constructor:
                case MemberTypes.Method:
                    return "M:";
                case MemberTypes.Field:
                    return "F:";
                case MemberTypes.Property:
                    return "P:";
                case MemberTypes.Event:
                    return "E:";
                default:
                    throw new ArgumentException($"Unsuported MemberType {mInfo.MemberType}", "mInfo");
            }
        }

        public static void PrintAllMemberXDocIds(Type t)
        {
            if (t != null)
            {
                foreach (var mInfo in typeof(Foo<>).GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    print(BuildXDocId(mInfo));

                    if (mInfo.MemberType == MemberTypes.NestedType && mInfo.DeclaringType == t)
                    {
                        PrintAllMemberXDocIds(mInfo as Type);
                    }
                }
            }

        }

        public static void Run()
        {
            PrintAllMemberXDocIds(typeof(Foo<>));
        }
    }
    
}

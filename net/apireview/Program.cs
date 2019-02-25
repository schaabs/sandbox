using Azure.Security.KeyVault;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace apireview
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var file = new StreamWriter(@"c:\.temp\temp.md"))
            {
                WriteAllTypesAysnc(typeof(KeyVaultClient).Assembly, file).GetAwaiter().GetResult();

                file.Flush();
            }
        }

        static async Task WriteAllTypesAysnc(Assembly assm, StreamWriter file)
        {
            var types = typeof(KeyVaultClient).Assembly.GetTypes();

            var writer = new ClassMarkdownWriter(file);

            foreach(var type in assm.GetTypes())
            {
                await writer.WriteAsync(type);
            }
        }


        public class ClassMarkdownWriter
        {
            private int _indentLevel = 0;
            private StreamWriter _file;

            public ClassMarkdownWriter(StreamWriter file)
            {
                _file = file;
            }

            public async Task WriteAsync(Type type)
            {
                //start the markdown code block
                await WriteLineAsync("~~~ csharp");

                //start the namespace
                await WriteLineAsync($"namespace {type.Namespace}");

                await WriteLineAsync("{");
                _indentLevel++;

                //start the class
                var baseClassStr = type.BaseType != null ? " : " + GetTypeString(type.BaseType) : string.Empty;

                await WriteLineAsync($"public class {GetTypeString(type)}{baseClassStr}");

                await WriteLineAsync("{");

                _indentLevel++;

                bool linesWritten = type.GetConstructors().Length > 0;

                //write constructors 
                foreach(var ctor in type.GetConstructors())
                {
                    await WriteContructorAsync(ctor);
                }

                if(linesWritten)
                {
                    await _file.WriteLineAsync();
                }

                //write properties
                foreach (var prop in type.GetProperties().Where(p => p.DeclaringType.Assembly == type.Assembly))
                {
                    linesWritten = true;

                    await WritePropertyAsync(prop);
                }

                //write methods
                if (linesWritten)
                {
                    await _file.WriteLineAsync();
                }

                foreach (var method in type.GetMethods().Where(meth => !meth.IsSpecialName && meth.DeclaringType.Assembly == type.Assembly))
                {
                    linesWritten = true;

                    await WriteMethodAsync(method);
                }

                //end the class
                _indentLevel--;

                await WriteLineAsync("}");

                //end the namespace
                _indentLevel--;

                await WriteLineAsync("}");

                //end the code block
                await WriteLineAsync("~~~");
            }

            private async Task WriteContructorAsync(ConstructorInfo ctor)
            {
                var buff = new StringBuilder($"public {GetFixedTypeName(ctor.DeclaringType)}(");
                
                var parameters = ctor.GetParameters();

                for (int i = 0; i < parameters.Length; i++)
                {
                    AppendParameterString(parameters[i], buff);

                    if (i < parameters.Length - 1)
                    {
                        buff.Append(", ");
                    }
                }

                buff.Append(");");

                await WriteLineAsync(buff.ToString());
            }

            private async Task WritePropertyAsync(PropertyInfo prop)
            {
                var accessorStr = prop.CanWrite ? "{ get; set; }" : "{ get; }";

                await WriteLineAsync($"public {GetTypeString(prop.PropertyType)} {prop.Name} {accessorStr}");
            }

            private async Task WriteMethodAsync(MethodInfo method)
            {
                StringBuilder buff = new StringBuilder("public ");

                buff.Append(method.IsStatic ? "static " : string.Empty);

                buff.Append(method.IsVirtual ? "virtual " : string.Empty);

                buff.Append(method.ReturnType.IsSubclassOf(typeof(Task)) ? "async " : string.Empty);

                buff.Append(GetTypeString(method.ReturnType)).Append(' ');

                buff.Append(method.Name);

                //todo: handle generic method defs

                buff.Append("(");

                var parameters = method.GetParameters();

                for (int i = 0; i < parameters.Length; i++)
                {
                    AppendParameterString(parameters[i], buff);

                    if(i < parameters.Length - 1)
                    {
                        buff.Append(", ");
                    }
                }

                buff.Append(");");

                await WriteLineAsync(buff.ToString());
            }

            private void AppendParameterString(ParameterInfo param, StringBuilder buff)
            {
                if(param.IsOut)
                {
                    buff.Append("out ");
                }

                if(param.IsRetval)
                {
                    buff.Append("ref ");
                }

                AppendTypeString(param.ParameterType, buff).Append(' ');

                buff.Append(param.Name);

                if(param.HasDefaultValue)
                {
                    buff.Append(" = default");
                }
            }


            private string GetMethodString(MethodInfo method)
            {
                return method.Name;
            }

            private string GetTypeString(Type type)
            {
                return AppendTypeString(type, new StringBuilder()).ToString();
            }

            private StringBuilder AppendTypeString(Type type, StringBuilder buff)
            {
                if(type.Name == "Nullable`1")
                {
                    return AppendTypeString(type.GetGenericArguments()[0], buff).Append('?');
                }

                buff.Append(GetFixedTypeName(type));

                if(type.IsGenericType || type.IsGenericTypeDefinition)
                {
                    buff.Append('<');

                    var genArgs = type.GetGenericArguments();

                    for (int i = 0; i < genArgs.Length; i++)
                    {
                        AppendTypeString(genArgs[i], buff);

                        if (i < genArgs.Length - 1)
                        {
                            buff.Append(", ");
                        }
                    }

                    buff.Append('>');
                }


                return buff;
            }

            private string GetFixedTypeName(Type type)
            {
                var arrSplit = type.Name.Split('[');

                switch(arrSplit[0])
                {
                    case "Object":
                        arrSplit[0] = "object";
                        break;
                    case "String":
                        arrSplit[0] = "string";
                        break;
                    case "Int32":
                        arrSplit[0] = "int";
                        break;
                    case "Int64":
                        arrSplit[0] = "long";
                        break;
                    case "Int16":
                        arrSplit[0] = "short";
                        break;
                    case "Double":
                        arrSplit[0] = "double";
                        break;
                    case "Float":
                        arrSplit[0] = "float";
                        break;
                    case "Boolean":
                        arrSplit[0] = "bool";
                        break;
                    case "Byte":
                        arrSplit[0] = "byte";
                        break;
                    default:
                        arrSplit[0] = arrSplit[0].Split('`')[0];
                        break;
                }

                return string.Join('[', arrSplit);
            }

            private async Task WriteLineAsync(string str)
            {
                await _file.WriteLineAsync(new String(' ', _indentLevel * 4) + str);
            }
        }
    }
}

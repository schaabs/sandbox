using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace docit
{
    class Program
    {
        static void Main(string[] args)
        {
            var index = new XDocReader(@"D:\madman_tfs\dev\DataDriven\MaDLybZ\Source\bin\Debug\Microsoft.Test.MaDLybZ.XML");

            XDocMember member;
            while ((member = index.GetNextMemberAsync().GetAwaiter().GetResult()) != null)
            {
                Console.WriteLine(member.Name);
            }
        }
    }

    internal class XDocMemberContentReader : XDocReaderBase
    {
        public XDocMemberContentReader(XmlReader reader) : base(reader)
        {
            Topic = reader.Name;

            ContentId = ReadAttributeValue("name") ?? ReadAttributeValue("cref");
        }

        public string Topic { get; private set; }

        public string ContentId { get; private set; }
        
        public string Content { get; private set; }

        public async Task ReadContentStringAsync()
        {
            var buff = new StringBuilder(_reader.Value);

            while (await _reader.ReadAsync())
            {
                if (_reader.IsStartElement() || _reader.IsEmptyElement)
                {
                    if (_reader.Name == "see")
                    {
                        var link = ReadAttributeValue("cref");

                        buff.Append($"[%{link}%](%{link}%)");
                    }
                    else if (_reader.Name == "paramref")
                    {
                        buff.Append($"**{ReadAttributeValue("name")}**");
                    }
                    else if (_reader.Name == "code")
                    {
                        buff.Append($"\n\n'''{await _reader.ReadInnerXmlAsync()}'''\n\n");
                    }
                    else
                    {
                        buff.Append($"\nERROR:{_reader.Name} {_reader.Value}\n");
                    }
                }
                else
                {
                }
            }

        }
    }

    internal abstract class XDocReaderBase
    {
        protected XmlReader _reader;

        protected XDocReaderBase(XmlReader reader)
        {
            _reader = reader;
        }

        public string XDocId { get; set; }

        protected string ReadAttributeValue(string attrName)
        {
            return ReadAttributeValue(_reader, attrName);
        }

        protected static string ReadAttributeValue(XmlReader reader, string attrName)
        {

            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                if (reader.Name == attrName)
                {
                    reader.MoveToElement();

                    return reader.Value;
                }
            }

            reader.MoveToElement();

            return null;
        }
    }

    internal abstract class MarkdowndWriterBase
    {
        private FileStream _file;

        public string FilePath { get; set; }


    }

    internal class MemberMarkdownWriter : MarkdowndWriterBase
    {
        public MemberDocInfo MemberDocInfo { get; private set; }
    }

    public static class XDocId
    {
        public static string FromMemberInfo(MemberInfo mInfo)
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

                if (mParams.Length > 0)
                {
                    buff.Append("(");

                    buff.Append(string.Join(",", mParams.Select(pInfo => GetParameterTypeString(pInfo.ParameterType, typeGenArgs, mGenArgs) + (pInfo.IsOut ? "@" : ""))));

                    buff.Append(")");
                }

            }
            return buff.ToString();
        }

        private static string GetParameterTypeString(Type paramType, string[] typeGenArgs, string[] memberGenArgs)
        {
            var typeStr = paramType.FullName;

            if (typeStr == null)
            {
                int index = -1;

                if (paramType.IsByRef)
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
    }

    internal abstract class MemberDocInfo
    {
        public MemberDocInfo(MemberInfo memberInfo, XDocMemberReader xdocReader, MemberDocInfo parentDocInfo)
        {
            MemberInfo = memberInfo;

            XDocReader = xdocReader;

            ParentDocInfo = parentDocInfo;
        }
        
        public MemberInfo MemberInfo { get; private set; }

        public XDocMemberReader XDocReader { get; private set; }

        public MemberDocInfo ParentDocInfo { get; private set; }

        protected virtual string GetTitle()
        {
            return MemberInfo.Name;
        }

        protected virtual string GetNamespace()
        {
            return MemberInfo.ReflectedType.Namespace;
        }
        
    }
    
    internal class XDocMemberReader : XDocReaderBase
    {
        private Dictionary<string, List<XDocMemberContentReader>> _contentDict;

        public XDocMemberReader(XmlReader reader) : base(reader)
        {
            this.XDocId = ReadAttributeValue(reader, "name");
        }
        
        protected async Task ReadContentElementsAsync()
        {
            while (await _reader.ReadAsync())
            {
                if (_reader.IsStartElement())
                {
                    AddContent(new XDocMemberContentReader(_reader.ReadSubtree()));
                }
            }
        }

        private void AddContent(XDocMemberContentReader content)
        {
            List<XDocMemberContentReader> topicList = null;

            if (!_contentDict.TryGetValue(content.Topic, out topicList))
            {
                topicList = new List<XDocMemberContentReader>();

                _contentDict[content.Topic] = topicList;
            }

            topicList.Add(content);
        }

    }

    internal class XDocReader : XDocReaderBase
    {
        public XDocReader(string path) : base(XmlReader.Create(path, new XmlReaderSettings() { Async = true, IgnoreComments = true }))
        {
            Members = new Dictionary<string, XDocMemberReader>();
        }

        public Dictionary<string, XDocMemberReader> Members { get; private set; } 

        public async Task ReadAsync()
        {
            while (await _reader.ReadAsync())
            {
                if (_reader.NodeType == XmlNodeType.Element && _reader.Name == "member")
                {
                    var memberReader = new XDocMemberReader(_reader.ReadSubtree());

                    Members[memberReader.XDocId] = memberReader;
                }
            }
        }        
    }
}

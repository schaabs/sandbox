using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Threading;

namespace sandbox.common
{
    public class ConsoleBuffer
    {
        private StringBuilder _buff = new StringBuilder();
        private int _linemax = 100;
        private int _linepos = 0;
        private int _indent = 0;

        public void SetIndent(int indent)
        {
            _indent = indent;

            if(indent > _linepos)
            {
                _buff.Append(' ', indent - _linepos + 1);

                _linepos = indent;
            }
        }

        public int CurrentPosition { get { return _linepos; } }

        public int RemainingLineLength
        {
            get
            {
                return _linemax - _linepos - 1;
            }
        }

        public void Append<T>(T val)
        {
            var str = val.ToString();

            //if str will fit on the current line
            if (str.Length <= RemainingLineLength)
            {
                _buff.Append(str);

                _linepos += str.Length;
            }
            else if (str.Length < _linemax - _indent)
            {
                NextLine();

                Append(str);
            }
            else
            {
                int breakIx = str.LastIndexOf(" ", RemainingLineLength - 1);

                if (breakIx <= 0)
                {
                    breakIx = RemainingLineLength - 1;
                }

                _buff.Append(str, 0, breakIx + 1);

                NextLine();

                Append(str.Substring(breakIx + 1));
            }
        }
        
        public void AppendLine<T>(T val)
        {
            Append(val);

            NextLine();
        }

        public void AppendLine()
        {
            NextLine();
        }

        public void NextLine()
        {
            _buff.AppendLine();

            if (_indent > 0)
            {
                _buff.Append(' ', _indent);
            }

            _linepos = _indent;
        }

        public override string ToString()
        {
            return _buff.ToString();
        }

    }
    public abstract class ArgParser
    {
        private List<Arg> _pargs = new List<Arg>();

        private Dictionary<string, Arg> _nargs = new Dictionary<string, Arg>();

        public ArgParser(string command)
        {
            Command = command;
        }

        public void Initialize()
        {
            LoadArgs();
        }

        public Flag PrintHelp { get; private set; } = new Flag() { Name = "help", Help = "show this help message and exit" };

        public string Command { get; private set; }
        
        public virtual void AppendUsage(ConsoleBuffer buff)
        {
            buff.AppendLine("usage:");

            buff.Append(Command);

            buff.Append(" ");

            buff.SetIndent(buff.CurrentPosition);

            for (int i = 0; i < _pargs.Count; i++)
            {
                _pargs[i].AppendUsage(buff);

                buff.Append(" ");
            }

            foreach (var narg in _nargs.Values)
            {
                narg.AppendUsage(buff);

                buff.Append(" ");
            }

            buff.SetIndent(0);
        }

        public virtual void AppendHelp(ConsoleBuffer buff)
        {
            AppendUsage(buff);

            buff.AppendLine();

            buff.AppendLine();

            if (_pargs.Count > 0)
            {
                buff.AppendLine("positional arguments:");

                buff.AppendLine();

                for (int i = 0; i < _pargs.Count; i++)
                {
                    _pargs[i].AppendHelp(buff);

                    buff.AppendLine();
                }
            }

            if (_nargs.Count > 0)
            {
                buff.AppendLine("named arguments:");

                buff.AppendLine();

                foreach(var narg in _nargs.Values)
                {
                    narg.AppendHelp(buff);

                    buff.AppendLine();
                }
            }

        }

        private void LoadArgs()
        {
            int curpos = 0;

            bool pvargFound = false;

            //load the positional arguments
            foreach(var parg in this.GetType().GetRuntimeProperties().Where(p => p.PropertyType.GetTypeInfo().IsSubclassOf(typeof(Arg))).Select(p => p.GetValue(this)).Cast<Arg>().Where(a => a.Position.HasValue).OrderBy(a => a.Position.Value))
            {
                if(parg.Position.Value != curpos)
                {
                    throw new ArgumentException($"Invalid positional arguments, no positional argument is specified for position {curpos}");
                }

                if(pvargFound)
                {
                    throw new ArgumentException($"Invalid positional arguments, no positional arguments are allowed after a positional argument of variable length", parg.Name);
                }

                _pargs.Add(parg);

                curpos++;

                pvargFound = (parg as IVArg)?.Count <= 0;
            }

            //load the named arguments
            foreach (var narg in this.GetType().GetRuntimeProperties().Where(p => p.PropertyType.GetTypeInfo().IsSubclassOf(typeof(Arg))).Select(p => p.GetValue(this)).Cast<Arg>().Where(a => !a.Position.HasValue))
            {
                if(string.IsNullOrWhiteSpace(narg.Name))
                {
                    throw new ArgumentException($"Invalid arguments, all non-positional arguments must have a valid unique Name");
                }

                if(_nargs.ContainsKey(narg.Name.ToLower()))
                {
                    throw new ArgumentException($"Invalid arguments, all non-positional arguments must have a valid unique Name", narg.Name);
                }

                _nargs[narg.Name.ToLower()] = narg;
            }
        }
    }

    public abstract class Arg
    {
        public string Name { get; set; } = null;

        public string Help { get; set; } = null;

        public string MetaVar { get; set; } = null;

        public int? Position { get; set; } = null;

        public bool Required { get; set; } = false;

        public abstract void AppendUsage(ConsoleBuffer buff);

        public abstract void AppendHelp(ConsoleBuffer buff);
    }

    public abstract class ArgBase<T> : Arg
    {
        protected ArgBase()
        {
            ParseFunc = ParseArgString;
        }

        public Func<string, T> ParseFunc { get; set; }


        protected static Lazy<MethodInfo> StaticParseMethodInfo
        {
            get
            {
                return new Lazy<MethodInfo>(() => typeof(T).GetRuntimeMethods().FirstOrDefault
                                                        (
                                                            mi => mi.IsStatic &&
                                                            mi.IsPublic &&
                                                            !mi.ContainsGenericParameters &&
                                                            mi.Name == "Parse" &&
                                                            mi.ReturnType == typeof(T) &&
                                                            mi.GetParameters().Length == 1 &&
                                                            mi.GetParameters()[0].ParameterType == typeof(string)
                                                        ),
                                           LazyThreadSafetyMode.ExecutionAndPublication);
            }
        }

        protected virtual T ParseArgString(string elem)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)((object)elem);
            }

            if (StaticParseMethodInfo.Value == null)
            {
                throw new ArgumentException($"Unable to parse argument, type {typeof(T).Name} does not expose a static 'Parse' method.", Name);
            }

            try
            {
                var parsed = StaticParseMethodInfo.Value.Invoke(null, new object[] { elem });

                return (T)parsed;
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Parsing the value '{elem}' to type {typeof(T).Name} failed with exception. See inner exception for specific failure.", Name, e);
            }
        }

        public override void AppendUsage(ConsoleBuffer buff)
        {
            if(!Required)
            {
                buff.Append("[");
            }

            AppendHelpTitle(buff);

            if (!Required)
            {
                buff.Append("]");
            }
        }

        public override void AppendHelp(ConsoleBuffer buff)
        {
            AppendHelpTitle(buff);

            buff.AppendLine();

            buff.Append(Help);
        }

        protected virtual void AppendHelpTitle(ConsoleBuffer buff)
        {
            if (Position != null)
            {
                AppendMetaValue(buff);
            }
            else
            {
                buff.Append("--");

                buff.Append(Name ?? string.Empty);

                buff.Append(" ");

                AppendMetaValue(buff);
            }
        }

        protected virtual void AppendMetaValue(ConsoleBuffer buff)
        {
            buff.Append(MetaVar ?? Name?.ToUpperInvariant() ?? typeof(T).Name);
        }
    }

    public class Arg<T> : ArgBase<T>
    {
        public T Default { get; set; }

        public T Value { get; protected set; }
        
        public virtual void Parse(string strVal)
        {
            if (Required && string.IsNullOrWhiteSpace(strVal))
            {
                throw new ArgumentException("At least one value must be specified for the required argument", Name);
            }

            Value = ParseFunc(strVal);
        }
    }

    public class Flag : Arg<bool>
    {
        public override void Parse(string strVal)
        {
            if (string.IsNullOrWhiteSpace(strVal))
            {
                Value = true;
            }
        }

        protected override void AppendHelpTitle(ConsoleBuffer buff)
        {
            buff.Append("--");

            buff.Append(Name);
        }
    }

    public class Choice<T> : Arg<T>
    {
        public T[] Choices { get; set; }

        public override void Parse(string strVal)
        {
            base.Parse(strVal);
        }

        protected override T ParseArgString(string elem)
        {
            T val = base.ParseArgString(elem);

            if (!Choices.Contains(val))
            {
                throw new ArgumentException($"The specified value does not match one of the allowed values ( {string.Join(" | ", Choices)} )", Name);
            }

            return val;
        }

        protected override void AppendMetaValue(ConsoleBuffer buff)
        {
            buff.Append("(");
            
            for(int i = 0; i < Choices.Length; i++)
            {
                buff.Append(Choices[i]);

                if(i < Choices.Length - 1)
                {
                    buff.Append("|");
                }
            }

            buff.Append(")");
        }
    }

    public interface IVArg
    {
        int Count { get; }
    }

    public class VArg<T> : ArgBase<T>, IVArg
    {
        public int Count { get; set; } = 0;

        public T[] DefaultValues { get; set; }

        public T[] Values { get; protected set; }

        public virtual void Parse(string[] vargs)
        {
            if (Count > 0 && vargs.Length != Count)
            {
                throw new ArgumentException($"The number of specified values for the argument did not match the expected count of {Math.Abs(Count)}", Name);
            }

            if (Count < 0 && vargs.Length > Math.Abs(Count))
            {
                throw new ArgumentException($"The number of specified values for the argument exceeded the maximum of {Math.Abs(Count)}", Name);
            }

            if(Required && vargs.Length == 0)
            {
                throw new ArgumentException("At least one value must be specified for the required argument", Name);
            }

            Values = new T[vargs.Length];
            
            for(int i = 0; i < vargs.Length; i++)
            {
                Values[i] = ParseFunc(vargs[i]);
            }
        }

        protected override void AppendMetaValue(ConsoleBuffer buff)
        {
            string metaElemValue = MetaVar ?? Name?.ToUpperInvariant();

            if(!Required || Count < 0)
            {
                buff.Append("[");
            }

            if (Count <= 0)
            {
                buff.Append(metaElemValue);

                if (Count == 0 || Count <= -2)
                {
                    buff.Append(" [");

                    buff.Append(metaElemValue);

                    if (Count == 0 || Count <= -3)
                    {
                        buff.Append(" ...");

                        if (Count != 0)
                        {
                            buff.Append("(");

                            buff.Append(Math.Abs(Count));

                            buff.Append("X MAX)");
                        }
                    }

                    buff.Append("]");
                }
            }
            else
            {
                for(int i = 0; i < Count && i < 3; i++)
                {
                    buff.Append(metaElemValue);

                    buff.Append(i + 1);

                    if(i < Count - 1)
                    {
                        buff.Append(" ");
                    }
                }

                if(Count > 3)
                {
                    buff.Append(" ... ");
                }

                if (Count > 2)
                {
                    buff.Append(metaElemValue);

                    buff.Append(Count);
                }

            }

            if (!Required || Count < 0)
            {
                buff.Append("]");
            }
        }
    }

    public abstract class DumplingArgs : ArgParser
    {
        public DumplingArgs(string command) : base(command)
        {
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

        public Arg<string> DumpPath { get; private set; } = new Arg<string> { Name = "dumppath", Help = "path to teh dumpfile to be uploaded" };
    }
}

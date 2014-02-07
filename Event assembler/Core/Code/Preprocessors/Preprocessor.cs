using System;
using System.Collections.Generic;
using System.Text;
using Nintenlord.Collections;
using Nintenlord.Event_Assembler.Core.Code.Preprocessors.BuiltInMacros;
using Nintenlord.Event_Assembler.Core.Code.Preprocessors.Directives;
using Nintenlord.Event_Assembler.Core.Collections;
using Nintenlord.Event_Assembler.Core.IO.Input;
using Nintenlord.Event_Assembler.Core.IO.Logs;
using Nintenlord.Utility;
using Nintenlord.Utility.Strings;

namespace Nintenlord.Event_Assembler.Core.Code.Preprocessors
{
    public sealed class Preprocessor : IDirectivePreprocessor
    {
        readonly Stack<bool> include;
        readonly IDefineCollection defCol;
        readonly Pool pool;

        readonly List<string> predefined;
        readonly List<string> reserved;
        readonly CurrentLine curLine;
        readonly CurrentFile curFile;
        readonly Dictionary<string, IDirective> directives;

        readonly ILog messageLog;
        IInputStream inputStream;
        int blockCommentDepth;

        const bool includedFilesAsNewScope = true;

        public Preprocessor(ILog messageLog)
        {
            this.messageLog = messageLog;
            this.predefined = new List<string>();
            this.reserved = new List<string>();
            this.pool = new Pool();
            this.curLine = new CurrentLine();
            this.curFile = new CurrentFile();
            this.blockCommentDepth = 0;

            var defColOpt = new DefineCollectionOptimized();
            defColOpt["IsDefined"] = new IsDefined(defColOpt);
            defColOpt["DeconstVector"] = new DeconstructVector();
            defColOpt["ConstVector"] = new BuildVector();
            defColOpt["ToParameters"] = new VectorToParameter();
            defColOpt["Signum"] = new Signum();
            defColOpt["Switch"] = new Switch();
            defColOpt["String"] = new InsertText();
            defColOpt["AddToPool"] = pool;
            defColOpt["_line_"] = curLine;
            defColOpt["_file_"] = curFile;
            defCol = defColOpt;

            this.include = new Stack<bool>();
            this.include.Push(true);

            directives = (new IDirective[] { 
                new IfDefined(),
                new IfNotDefined(),
                new Define(),
                new DumpPool(),
                new Else(),
                new EndIf(),
                new Include(),
                new IncludeBinary(),
                new Undefine()
            }).GetDictionary<string, IDirective>();
        }

        #region IPreprocessor Members

        public void AddDefined(IEnumerable<string> original)
        {
            predefined.AddRange(original);
        }

        public void AddReserved(IEnumerable<string> reserved)
        {
            this.reserved.AddRange(reserved);
        }

        public string Process(string line, IInputStream inputStream)
        {
            curLine.Stream = inputStream;
            curFile.Stream = inputStream;
            this.inputStream = inputStream;

            StringBuilder lineModific = new StringBuilder(line);

            if (!Nintenlord.Utility.Strings.Parser.ReplaceCommentsWith(lineModific, ' ', ref blockCommentDepth))
            {
                messageLog.AddError(inputStream.GetErrorString("Error removing comments"));
            }
            line = lineModific.ToString();

            if (line.FirstNonWhiteSpaceIs('#'))
            {
                HandleDirective(line);
                return "";
            }
            else
            {
                if (include.And())
                {
                    var newResult = defCol.ApplyDefines(line);

                    if (!newResult.CausedError)
                    {
                        string newLine = newResult.Result; 
                        foreach (var item in predefined)
                        {
                            newLine = newLine.Replace(item, " ");
                        }
                        if (defCol.ContainsName("USING_CODE"))
                        {
                            newLine = HandleCODE(newLine);
                        }

                        return newLine;
                    }
                    else
                    {
                        messageLog.AddError(inputStream.GetErrorString(newResult.ErrorMessage));
                        return line;
                    }
                }
                else
                {
                    return "";
                }
            }
        }

        public static string HandleCODE(string newLine)
        {
            var index = newLine.IndexOf("CODE");

            if (index >= 0)
            {
                var parameters = newLine.Remove(index, 4).Split(' ');

                StringBuilder bldr = new StringBuilder();
                bool lastWasWord = false;
                bool lastWasByte = false;
                foreach (var parameter in parameters)
                {
                    bool currentIsByte = parameter.StartsWith("0x");
                    if (lastWasByte)
                    {
                        if (!currentIsByte)
                        {
                            bldr.Append("; WORD ");
                        }
                    }
                    else if (lastWasWord)
                    {
                        if (currentIsByte)
                        {
                            bldr.Append("; BYTE ");
                        }
                    }
                    else //Only at the start of code
                    {
                        if (currentIsByte)
                        {
                            bldr.Append("BYTE ");
                        }
                        else
                        {
                            bldr.Append("WORD ");
                        }
                    }
                    bldr.Append(parameter);
                    bldr.Append(' ');

                    //End
                    lastWasByte = currentIsByte;
                    lastWasWord = !currentIsByte;
                }

                return bldr.ToString();
            }
            else return newLine;
        }
        
        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (pool.AmountOfLines > 0)
            {
                messageLog.AddWarning("Pool contains undumped lines at the end");
            }
            if (include.Count > 1)
            {
                messageLog.AddWarning("#ifdef's are missing at the end");
            }

        }

        #endregion
        
        #region IDirectivePreprocessor Members

        public Stack<bool> Include
        {
            get { return include; }
        }

        public IDefineCollection DefCol
        {
            get { return defCol; }
        }

        public Pool Pool
        {
            get { return pool; }
        }

        public IInputStream Input
        {
            get
            {
                return inputStream;
            }
        }


        public bool IsValidToDefine(string name)
        {
            return reserved.Contains(name) || predefined.Contains(name);
        }

        public void IncludeFile(string file)
        {
            inputStream.OpenSourceFile(file);
        }

        public void IncludeBinary(string file)
        {
            inputStream.OpenBinaryFile(file);
        }
        
        public bool IsPredefined(string name)
        {
            return predefined.Contains(name);
        }
        #endregion
        
        private void HandleDirective(string line)
        {
            string[] elements = Nintenlord.Utility.Strings.Parser.SplitToParameters(line);

            string directiveName;
            int parameterAmount;
            if (elements[0].Equals("#"))
            {
                directiveName = elements[1];
                parameterAmount = elements.Length - 2;
            }
            else
            {
                directiveName = elements[0].TrimStart('#');
                parameterAmount = elements.Length - 1;
            }
            string[] parameters = new string[parameterAmount];
            Array.Copy(elements, elements.Length - parameterAmount, parameters, 0, parameterAmount);

            IDirective directive;
            if (directives.TryGetValue(directiveName, out directive))
            {
                var error = directive.Matches("Directive " + directiveName, parameterAmount);
                if (!error)
                {
                    if (directive.RequireIncluding)
                    {
                        if (this.include.And())
                        {
                            var causedError = directive.Apply(parameters, this);
                            if (causedError)
                                messageLog.AddError(inputStream.GetErrorString(causedError));
                        }
                    }
                    else
                    {
                        var causedError = directive.Apply(parameters, this);
                        if (causedError)
                            messageLog.AddError(inputStream.GetErrorString(causedError));
                    }
                }
                else messageLog.AddError(inputStream.GetErrorString(error));
            }
            else
            {
                messageLog.AddError(inputStream.GetErrorString(
                    ": No directive with the name #" + directiveName + " exists"));
            }
        }

    }
}
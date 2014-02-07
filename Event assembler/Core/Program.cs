using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nintenlord.Collections;
using Nintenlord.Event_Assembler.Core.Code;
using Nintenlord.Event_Assembler.Core.Code.Language;
using Nintenlord.Event_Assembler.Core.Code.Language.Old;
using Nintenlord.Event_Assembler.Core.Code.Language.Parser;
using Nintenlord.Event_Assembler.Core.Code.Preprocessors;
using Nintenlord.Event_Assembler.Core.Code.Templates;
using Nintenlord.Event_Assembler.Core.GBA;
using Nintenlord.Event_Assembler.Core.IO.Input;
using Nintenlord.Event_Assembler.Core.IO.Logs;
using Nintenlord.IO;
using Nintenlord.Utility.Strings;
using Nintenlord.Event_Assembler.Core.Code.Language.Types.IntegerRepresentations;

namespace Nintenlord.Event_Assembler.Core
{
    /// <summary>
    /// Done:
    ///       Fix problem with merging codes making label positions vanish.
    ///       Fix template comparing to return UNIT instead of UNIT 0.
    ///       Fix problem with 1 bit long parameters.
    ///       Fix problem with bits getting reversed when reading/writing. *A FEATURE, NOT A BUG*
    ///       Make preprocessor handle stacked block comments properly.
    ///       Make sure paths like \Test\test.txt are processed correctly.
    ///       Fix Template choosing 0 0 0 0 over [0,0,0,0]
    ///       Make EACodeLanguage to reveal it's codes somehow.
    ///       Add pool ability to preprocessor, with second parameter as optional label name.
    ///       Add built-in macros like ?(), >(), =(), cond(), vector buiding and unbuilding, etc.
    ///       Rewrite macro storing to make searching faster.
    ///       Rewrite code template storing to make searching faster.
    ///       Remove ChooseEnum from IMEssageLog.
    ///       Move LanguageRawsAnalyzer to Core.
    ///       Make codes give better error codes if code exists but amount of parameters is right.
    ///       Make $XX008001 fail properly.
    ///
    /// Later releases:
    ///       Make Disassembly use BinaryReader or some sort of input-stream somehow.
    ///       Rewrite offset handling to make use IntegerType properly.
    ///       Add support for disassembling fixed (other parameters affect) amount of pointed code.
    ///       Make structure disassembly more modular.
    ///       More options to disassembly.
    ///       Disassembly rewrite to use types(meaning Eliwood will appear in disassembled chapters)
    ///       
    ///       Move game specific things to separate files.
    ///       All the remaining language specific things to raws, meaning you can add custom languages
    ///       Type system(class, character, position etc)
    ///       Make language raws processing and code assembling more modular for possible future IDE.
    ///       
    ///       Make recursive macros work properly.
    ///       Make preprocessor properly report errors with line "#"
    ///       Add list/vector processing macros aka Head, Tail and Cons, then implement EAstdlib
    ///         macros based on them like Map, Fold and etc.
    ///       Add built-in macro for getting lists/vectors length.
    ///       Make IDefineCollection to reveal it's defines and macros somehow.
    ///       Make macros give better error codes if macro exists but amount of parameters is right.
    /// </summary>
    public static class Program
    {
        static private IDictionary<string, EACodeLanguage> languages;
        static private readonly StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
        
        //StdOut - output
        //StdIn - input
        //StdError - errors and messages
        //0 - D, A or Doc, assemble, disassemble or doc generation
        //1 - language (assembly or disassembly only)
        //2 - disassembly mode (disassembly only)
        //3 - offset to disassemble (disassembly only)
        //4 - priority to disassemble (disassembly only)
        //5 - length to disassemble (disassembly only)
        //flags: -addEndGuards
        //       -raws:Folder or file
        //       -rawsExt:extension
        //       -output:File
        //       -input:File
        //       -error:File
        //       -docHeader:File
        //       -docFooter:File
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }

            var messageLog = new MessageLog(1000);

            //var cmd = HandleFlags(flags, messageLog);
            var cmd = new CommandLineArgs();
            var error = cmd.SetArgs(args);

            if (!error.CausedError)
            {
                try
                {
                    Run(messageLog, cmd);
                }
                catch (Exception e)
                {
                    messageLog.AddError(e.Message);
                }
            }

            if (cmd.errorFile != null)
            {
                using (var writer = new StreamWriter(cmd.errorFile))
                {
                    messageLog.WriteToStream(writer);
                }
            }
            else
            {
                messageLog.WriteToStream(Console.Error);
            }
            messageLog.Clear();
        }

        private static void Run(MessageLog messageLog, CommandLineArgs cmd)
        {
            if (stringComparer.Compare(cmd.operation, "doc") == 0)
            {
                MakeDoc(cmd.outputFile, cmd.rawsFolder, cmd.rawsExtension, cmd.isDirectory, cmd.docHeader, cmd.docFooter);
            }
            else if (stringComparer.Compare(cmd.operation, "plusplus") == 0)
            {
#if !DEBUG
                throw new NotImplementedException();
#endif
                LoadCodes(cmd.rawsFolder, cmd.rawsExtension, cmd.isDirectory, false);

                EACodeLanguage language;
                if (languages.TryGetValue(cmd.language, out language))
                {
                    HighlightingHelper.GetNotepadPlusPluslanguageDoc(language, cmd.outputFile);
                }
            }
            else if (stringComparer.Compare(cmd.operation, "prognotepad") == 0)
            {
                LoadCodes(cmd.rawsFolder, cmd.rawsExtension, cmd.isDirectory, false);
                
                HighlightingHelper.GetProgrammersNotepadlanguageDoc(languages.Values, cmd.outputFile);
            }
            else
            {
                LoadCodes(cmd.rawsFolder, cmd.rawsExtension, cmd.isDirectory, false);

                if (languages.ContainsKey(cmd.language))
                {
                    if (stringComparer.Compare(cmd.operation, "A") == 0)
                    {
                        Assemble(cmd.inputFile, cmd.outputFile, cmd.language, messageLog);
                    }
                    else if (stringComparer.Compare(cmd.operation, "D") == 0)
                    {
                        Disassemble(
                            cmd.inputFile,
                            cmd.outputFile,
                            cmd.language,
                            cmd.addEndGuards,
                            cmd.disassemblyMode.Value,
                            cmd.offset.Value,
                            cmd.priority.HasValue ? cmd.priority.Value : Priority.none,
                            cmd.size.HasValue ? cmd.size.Value : 0,
                            messageLog);
                    }
                    else messageLog.AddError("{0} is not a valid operation.", cmd.operation);
                }
                else messageLog.AddError("{0} is not a valid language", cmd.language);
            }
        }


        public static bool CodesLoaded
        {
            get { return languages != null; }
        }

        public static void Assemble(string inputFile, string outputFile, string languageName, ILog messageLog)
        {
            TextReader reader;
            bool close;
            if (inputFile != null)
            {
                reader = File.OpenText(inputFile);
                close = true;
            }
            else
            {
                reader = Console.In;
                close = false;
            }

            EACodeLanguage language = languages[languageName];

            if (outputFile != null)
            {
                if (File.Exists(outputFile))
                {
                    if (File.GetAttributes(outputFile).HasFlag(FileAttributes.ReadOnly))
                    {
                        messageLog.AddError("outputFile is read-only.");
                        goto end;
                    }
                }

                var cache = new ChangeStream();
                using (BinaryWriter writer = new BinaryWriter(cache))
                {
                    Assemble(language, reader, writer, messageLog);
                    if (messageLog.ErrorCount == 0)
                    {
                        using (Stream stream = File.OpenWrite(outputFile))
                        {
                            cache.WriteToFile(stream);
                        }
                    }
                }
            }
            else
            {
                messageLog.AddError("outputFile needs to be specified for assembly.");
            }
            end:

            if (close)
                reader.Close();
        }

        public static void Disassemble(string inputFile, string outputFile, string languageName,
            bool addEndGuards, DisassemblyMode mode, int offset, Priority priority, int size, ILog messageLog)
        {
            if (!File.Exists(inputFile))
            {
                messageLog.AddError("File " + inputFile + " doesn't exist.");
                return;
            }
            else if (File.Exists(outputFile))
            {
                if (File.GetAttributes(outputFile).HasFlag(FileAttributes.ReadOnly))
                {
                    messageLog.AddError("Output cannot be written to. It is read-only.");
                    return;
                }
            }
            
            EACodeLanguage language = languages[languageName];
            byte[] data = File.ReadAllBytes(inputFile);

            if (offset > data.Length)
            {
                messageLog.AddError("Offset is larger than size of file.");
            }
            else
            {
                if (size <= 0 || size + offset > data.Length)
                {
                    size = data.Length - offset;
                }
                IEnumerable<string[]> code;
                string[] defaultLines;
                switch (mode)
                {
                    case DisassemblyMode.Block:
                        code = language.Disassemble(data, offset, size, priority, addEndGuards, messageLog);
                        defaultLines = CoreInfo.DefaultLines(language.Name,
                            Path.GetFileName(inputFile), offset, size);
                        break;
                    case DisassemblyMode.ToEnd:
                        code = language.DisassembleToEnd(data, offset, priority, addEndGuards, messageLog);
                        defaultLines = CoreInfo.DefaultLines(language.Name,
                            Path.GetFileName(inputFile), offset, null);
                        break;
                    case DisassemblyMode.Structure:
                        code = language.DisassembleChapter(data, offset, addEndGuards, messageLog);
                        defaultLines = CoreInfo.DefaultLines(language.Name,
                            Path.GetFileName(inputFile), offset, null);
                        break;
                    default:
                        throw new ArgumentException();
                }

                if (messageLog.ErrorCount == 0)
                {
                    using (StreamWriter sw = new StreamWriter(outputFile))
                    {
                        sw.WriteLine();
                        sw.WriteLine(Frame(defaultLines, "//", 1));
                        sw.WriteLine();

                        foreach (string[] line in code)
                        {
                            sw.WriteLine(line.ToElementWiseString(" ", "", ""));
                        }
                    }
                }
            }
        }
        
        public static void LoadCodes(string rawsFolder, string extension, bool isDirectory, bool collectDocCodes)
        {
            languages = new Dictionary<string, EACodeLanguage>();
            LanguageProcessor pro = new LanguageProcessor(collectDocCodes, 
                new TemplateComparer(), stringComparer);
            IPointerMaker<int> ptrMaker = new GBAPointerMaker();
            if (isDirectory)
            {
                pro.ProcessCode(rawsFolder, extension);
            }
            else
            {
                pro.ProcessCode(rawsFolder);
            }
            foreach (KeyValuePair<string, ICodeTemplateStorer> item in pro.Languages)
            {
                Tuple<string, List<Priority>>[][] pointerList;

                switch (item.Key)
                {
                    case "FE6":
                        pointerList = FE6CodeLanguage.PointerList;
                        break;
                    case "FE7":
                        pointerList = FE7CodeLanguage.PointerList;
                        break;
                    case "FE8":
                        pointerList = FE8CodeLanguage.PointerList;
                        break;
                    default:
                        throw new NotSupportedException("Language " + item.Key + " not supported.");
                }
                ICodeTemplateStorer storer = item.Value;
                if (item.Key == "FE8")
                {
                    storer.AddCode(new GenericFE8Template(), Priority.none);
                }
                EACodeLanguage language = new EACodeLanguage(
                    item.Key, ptrMaker,
                    pointerList,
                    storer, stringComparer
                    );
                languages[item.Key] = language;
            }

        }
                
        public static void MakeDoc(string output, string rawsFolder, 
            string extension, bool isDirectory, string header, string footer)
        {
            var pro = new LanguageProcessor(true,
                new TemplateComparer(), stringComparer);
            //IPointerMaker<int> ptrMaker = new GBAPointerMaker();
            if (isDirectory)
            {
                pro.ProcessCode(rawsFolder, extension);
            }
            else
            {
                pro.ProcessCode(rawsFolder);
            }
            using (StreamWriter writer = File.CreateText(output))
            {
                if (header != null)
                {
                    writer.WriteLine(File.ReadAllText(header));
                    writer.WriteLine();
                }

                pro.WriteDocs(writer);

                if (footer != null)
                {
                    writer.WriteLine(File.ReadAllText(footer));
                    writer.WriteLine();
                }
            }
        }

        public static void Preprocess(string originalFile, string outputFile, string game, ILog messageLog)
        {
            EACodeLanguage language = languages[game];

            var predefined = new[]
            {
                "_" + game + "_",
                "_EA_"
            };

            using (var preprocessor = new Preprocessor(messageLog))
            {
                preprocessor.AddReserved(language.GetCodeNames());
                preprocessor.AddDefined(predefined);

                using (var reader = File.OpenText(originalFile))
                {
                    var stream = new PreprocessingInputStream(reader, preprocessor);
                    
                    var writer = new StringWriter();
                    while (true)
                    {
                        string line = stream.ReadLine();
                        if (line == null)
                        {
                            break;
                        }
                        writer.WriteLine(line);
                    }
                    messageLog.AddMessage("Processed code:\n" + writer + "\nEnd processed code");
                    //File.WriteAllText(outputFile, writer.ToString());   
                    
                }
            }
        }




        private static void Assemble(EACodeLanguage language, TextReader input, BinaryWriter output, ILog log)
        {
            var predefined = new List<string>
            {
                "_" + language.Name + "_",
                "_EA_"
            };
            using (IPreprocessor preprocessor = new Preprocessor(log))
            {
                preprocessor.AddReserved(language.GetCodeNames());
                preprocessor.AddDefined(predefined.ToArray());

                IInputStream stream = new PreprocessingInputStream(input, preprocessor);
                    
                language.Assemble(stream, output, log);
            }
        }
                
        private static string Frame(string[] lines, string toFrameWith, int padding)
        {
            int longestLine = lines.Aggregate(0, (i, s) => Math.Max(s.Length, i));

            string fullFrame = toFrameWith.Repeat(padding * 2 + toFrameWith.Length * 2 + longestLine);
            string hollowFrame = toFrameWith + " ".Repeat(padding * 2 + longestLine) + toFrameWith;
            string paddingText = " ".Repeat(padding);

            var builder = new StringBuilder();
            builder.AppendLine(fullFrame);
            builder.AppendLine(hollowFrame);

            foreach (string line in lines)
            {
                builder.AppendLine(toFrameWith +
                    paddingText +
                    line.PadRight(longestLine, ' ') +
                    paddingText +
                    toFrameWith
                    );
            }

            builder.AppendLine(hollowFrame);
            builder.AppendLine(fullFrame);
            return builder.ToString();
        }
    }
}

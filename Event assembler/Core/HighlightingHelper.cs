// -----------------------------------------------------------------------
// <copyright file="NotePadPlusPlusHelper.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Xml;
using Nintenlord.Collections;
using Nintenlord.Event_Assembler.Core.Code.Language.Old;
using Nintenlord.Utility.Primitives;

namespace Nintenlord.Event_Assembler.Core
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class HighlightingHelper
    {
        static readonly string[] preprocessorDirectives = 
        {
            "Words3", "#ifdef", "#define", "#pool", "#else", "#endif", "#ifndef", "#include", "#incbin", "#undef"
        };

        static readonly string[] builtInCodes = 
        {
            "CURRENTOFFSET", "MESSAGE", "ERROR", "WARNING", "ALIGN", "ORG"
        };

        public static void GetNotepadPlusPluslanguageDoc(EACodeLanguage language, string outputFile)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.ASCII,
                Indent = true,
                OmitXmlDeclaration = true,
                IndentChars = "    "
            };
            using (var writer = XmlWriter.Create(outputFile, settings))
            {
                writer.WriteStartElement("NotepadPlus");
                {
                    writer.WriteStartElement("UserLang");
                    writer.WriteAttributeString("name", language.Name + " Event Assembly");
                    writer.WriteAttributeString("ext", "event");

                    {
                        writer.WriteStartElement("Settings");

                        writer.WriteStartElement("Global");
                        writer.WriteAttributeString("caseIgnored", "yes");
                        writer.WriteEndElement();

                        writer.WriteStartElement("TreatAsSymbol");
                        writer.WriteAttributeString("comment", "yes");
                        writer.WriteAttributeString("commentLine", "yes");
                        writer.WriteEndElement();

                        writer.WriteStartElement("Prefix");
                        writer.WriteAttributeString("words1", "no");
                        writer.WriteAttributeString("words2", "no");
                        writer.WriteAttributeString("words3", "no");
                        writer.WriteAttributeString("words4", "no");
                        writer.WriteEndElement();

                        writer.WriteEndElement();
                    }
                    {
                        writer.WriteStartElement("KeywordLists");

                        NotepadPlusPlus.WriteKeywords(writer, "Delimiters", "<>");
                        NotepadPlusPlus.WriteKeywords(writer, "Folder+", "{");
                        NotepadPlusPlus.WriteKeywords(writer, "Folder-", "}");
                        NotepadPlusPlus.WriteKeywords(writer, "Operators", "(", ")", "[", "]", "+", "-", "*", "/", "%", ">>", "<<", "&", "|", "^", ",", ";");
                        NotepadPlusPlus.WriteKeywords(writer, "Comment", "1/*", "2*/", "0//");
                        NotepadPlusPlus.WriteKeywords(writer, "Words1", language.GetCodeNames());
                        NotepadPlusPlus.WriteKeywords(writer, "Words2");
                        //WriteKeywords(writer, "Words2", "CURRENTOFFSET", "MESSAGE", "ERROR", "WARNING", "ALIGN", "ORG");
                        NotepadPlusPlus.WriteKeywords(writer, "Words3", "#ifdef", "#define", "#pool", "#else", "#endif", "#ifndef", "#include", "#incbin", "#undef");
                        NotepadPlusPlus.WriteKeywords(writer, "Words4");

                        writer.WriteEndElement();
                    }
                    {
                        writer.WriteStartElement("Styles");

                        NotepadPlusPlus.WriteStyle(writer, "DEFAULT", 11, Color.Black, Color.White, "", 0, null);
                        NotepadPlusPlus.WriteStyle(writer, "FOLDEROPEN", 12, Color.Black, Color.White, "", 0, null);
                        NotepadPlusPlus.WriteStyle(writer, "FOLDERCLOSE", 13, Color.Black, Color.White, "", 0, null);
                        NotepadPlusPlus.WriteStyle(writer, "KEYWORD1", 5, Color.FromArgb(0, 0, 0xFF), Color.White, "", 0, null);
                        NotepadPlusPlus.WriteStyle(writer, "KEYWORD2", 6, Color.FromArgb(0, 0x80, 0xFF), Color.White, "", 0, null);
                        NotepadPlusPlus.WriteStyle(writer, "KEYWORD3", 7, Color.FromArgb(0, 0x40, 0x80), Color.White, "", 1, null);
                        NotepadPlusPlus.WriteStyle(writer, "KEYWORD4", 8, Color.FromArgb(0x40, 0x80, 0x80), Color.White, "", 0, null);
                        NotepadPlusPlus.WriteStyle(writer, "COMMENT", 1, Color.FromArgb(0, 0x9F, 0), Color.White, "", 0, null);
                        NotepadPlusPlus.WriteStyle(writer, "COMMENT LINE", 2, Color.FromArgb(0, 0x80, 0), Color.White, "", 0, null);
                        NotepadPlusPlus.WriteStyle(writer, "NUMBER", 4, Color.FromArgb(0x80, 0, 0x80), Color.White, "", 0, null);
                        NotepadPlusPlus.WriteStyle(writer, "OPERATOR", 10, Color.FromArgb(0xFF, 0, 0), Color.White, "", 1, null);
                        NotepadPlusPlus.WriteStyle(writer, "DELIMINER1", 14, Color.FromArgb(0xFF, 0, 0xFF), Color.White, "", 0, null);
                        NotepadPlusPlus.WriteStyle(writer, "DELIMINER2", 15, Color.FromArgb(0xFF, 0, 0x80), Color.White, "", 0, null);
                        NotepadPlusPlus.WriteStyle(writer, "DELIMINER3", 16, Color.Black, Color.White, "", 0, null);

                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }

        }

        public static void GetProgrammersNotepadlanguageDoc(IEnumerable<EACodeLanguage> languages, string outputFile)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = false,
                IndentChars = "  "
            };
            //settings.Encoding = Encoding.UTF8;

            const string baseName = "EA-language-base";

            using (var writer = XmlWriter.Create(outputFile, settings))
            {
                writer.WriteStartElement("Scheme");
                {
                    writer.WriteComment("Codes used by each language");
                    writer.WriteStartElement("keyword-classes");
                    {
                        foreach (var language in languages)
                        {
                            writer.WriteStartElement("keyword-class");
                            writer.WriteAttributeString("name", language.Name);
                            writer.WriteString(language.GetCodeNames().ToElementWiseString(" ", "", ""));
                            writer.WriteEndElement();
                        }

                        //writer.WriteStartElement("keyword-class");
                        //writer.WriteAttributeString("name", "preprocessor");
                        //writer.WriteString(preprocessorDirectives.ToElementWiseString(" ", "", ""));
                        //writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    
                    writer.WriteStartElement("base-language");
                    writer.WriteAttributeString("name", baseName);
                    {
                        writer.WriteStartElement("lexer");
                        writer.WriteAttributeString("name", "cpp");
                        writer.WriteEndElement();

                        writer.WriteStartElement("property");
                        writer.WriteAttributeString("name", "lexer.cpp.track.preprocessor");
                        writer.WriteAttributeString("value", "0");
                        writer.WriteEndElement();

                        writer.WriteStartElement("use-styles");
                        {             
                            ProgrammersNotepad.WriteStyle(writer, "Default", 32);
                            ProgrammersNotepad.WriteStyle(writer, "Whitespace", 0, "whitespace");
                            ProgrammersNotepad.WriteStyle(writer, "Comment", 1, "commentbox");
                            ProgrammersNotepad.WriteStyle(writer, "Comment Line", 2, "commentline");

                            ProgrammersNotepad.WriteStyle(writer, "Number", 4, "number");
                            ProgrammersNotepad.WriteStyle(writer, "Keyword", 5, "keyword");
                            ProgrammersNotepad.WriteStyle(writer, "String", 6, "string");
                            ProgrammersNotepad.WriteStyle(writer, "Character", 7, "string");

                            ProgrammersNotepad.WriteStyle(writer, "Operator", 10, null, null, null, false, true);
                            ProgrammersNotepad.WriteStyle(writer, "Identifier", 11);
                            ProgrammersNotepad.WriteStyle(writer, "End of line string", 12, null, 
                                Color.Black, Color.FromArgb(0xe0, 0xc0, 0xe0), true);
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();

                    foreach (var language in languages)
                    {
                        writer.WriteStartElement("language");
                        writer.WriteAttributeString("base", baseName);
                        writer.WriteAttributeString("name", language.Name);
                        writer.WriteAttributeString("title", language.Name + " Event Assembly");
                        writer.WriteAttributeString("folding", "true");
                        writer.WriteAttributeString("foldcomments", "true");
                        writer.WriteAttributeString("foldelse", "true");
                        writer.WriteAttributeString("foldcompact", "true");
                        writer.WriteAttributeString("foldpreproc", "true");
                        {
                            writer.WriteStartElement("lexer");
                            writer.WriteAttributeString("name", "cpp");
                            writer.WriteEndElement();

                            writer.WriteStartElement("property");
                            writer.WriteAttributeString("name", "lexer.cpp.track.preprocessor");
                            writer.WriteAttributeString("value", "0");
                            writer.WriteEndElement();

                            writer.WriteStartElement("comments");
                            writer.WriteAttributeString("line", "//");
                            writer.WriteAttributeString("streamStart", "/*");
                            writer.WriteAttributeString("streamEnd", "*/");
                            writer.WriteEndElement();

                            writer.WriteStartElement("use-keywords");
                            {
                                ProgrammersNotepad.UseKeywords(writer, 0, "Code names", language.Name);
                            }
                            writer.WriteEndElement();

                            writer.WriteStartElement("use-styles");
                            {
                                ProgrammersNotepad.WriteStyle(writer, "Preprocessor", 9, "preprocessor");
                            }
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
            }
        }

        static private class NotepadPlusPlus
        {
            public static void WriteKeywords(XmlWriter writer, string name)
            {
                writer.WriteStartElement("Keywords");
                writer.WriteAttributeString("name", name);
                writer.WriteEndElement();
            }

            public static void WriteKeywords(XmlWriter writer, string name, params string[] keyWords)
            {
                WriteKeywords(writer, name, (IEnumerable<string>)keyWords);
            }

            public static void WriteKeywords(XmlWriter writer, string name, IEnumerable<string> keyWords)
            {
                writer.WriteStartElement("Keywords");
                writer.WriteAttributeString("name", name);
                writer.WriteValue(keyWords.ToElementWiseString(" ", "", ""));
                writer.WriteEndElement();
            }

            public static void WriteStyle(
                XmlWriter writer,
                string name,
                int styleID,
                Color fgColor,
                Color bgColor,
                string fontName,
                int fontStyle,
                int? fontSize)
            {
                writer.WriteStartElement("WordsStyle");

                writer.WriteAttributeString("name", name);
                writer.WriteAttributeString("styleID", styleID.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("fgColor", GetRPGString(fgColor));
                writer.WriteAttributeString("bgColor", GetRPGString(bgColor));
                writer.WriteAttributeString("fontName", fontName);
                writer.WriteAttributeString("fontStyle", fontStyle.ToString(CultureInfo.InvariantCulture));

                if (fontSize != null)
                {
                    writer.WriteAttributeString("fontSize", fontSize.Value.ToString(CultureInfo.InvariantCulture));
                }

                writer.WriteEndElement();
            }

        }

        static private class ProgrammersNotepad
        {
            public static void WriteStyle(XmlWriter writer, string name, int key, string className = null,
                Color? foreGround = null, Color? background = null, bool eolFilled = false, bool bold = false)
            {
                writer.WriteStartElement("style");

                writer.WriteAttributeString("name", name);
                writer.WriteAttributeString("key", key.ToString(CultureInfo.InvariantCulture));

                if (className != null)
                    writer.WriteAttributeString("class", className);

                if (foreGround != null)
                    writer.WriteAttributeString("fore", GetRPGString(foreGround.Value));

                if (background != null)
                    writer.WriteAttributeString("back", GetRPGString(background.Value));

                if (eolFilled)
                    writer.WriteAttributeString("eolfilled", true.ToString().ToLower());

                if (bold)
                    writer.WriteAttributeString("bold", true.ToString().ToLower());

                writer.WriteEndElement();
            }

            public static void UseKeywords(XmlWriter writer, int key, string name, string className)
            {
                writer.WriteStartElement("keyword");
                writer.WriteAttributeString("key", key.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("name", name);
                writer.WriteAttributeString("class", className);
                writer.WriteEndElement();
            }
        }

        private static string GetRPGString(Color fgColor)
        {
            return (fgColor.ToArgb() & 0xFFFFFF).ToHexString("").PadLeft(6, '0');
        }
    }
}

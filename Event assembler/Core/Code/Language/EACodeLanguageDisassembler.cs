using System;
using System.Collections.Generic;
using System.Linq;
using Nintenlord.Collections;
using Nintenlord.Event_Assembler.Core.Code.Language.Old;
using Nintenlord.Event_Assembler.Core.Code.Templates;
using Nintenlord.Event_Assembler.Core.IO.Logs;
using Nintenlord.Utility;
using Nintenlord.Utility.Primitives;
using Nintenlord.Utility.Strings;
using Nintenlord.Event_Assembler.Core.Code.Language.Types.IntegerRepresentations;
using Nintenlord.Event_Assembler.Core.IO.Input;

namespace Nintenlord.Event_Assembler.Core.Code.Language
{
    /// <summary>
    /// To be generealized.
    /// </summary>
    sealed class EACodeLanguageDisassembler<T>
    {
        private const string offsetChangerName = "ORG";
        private const string currentOffsetName = "CURRENTOFFSET";
        private const string messagePrinterName = "MESSAGE";
        private const int minimumOffset = 0x100000;

        readonly ICodeTemplateStorer codeStorage;
        readonly IPointerMaker<T> pointerMaker;
        readonly Tuple<string, List<Priority>>[][] pointerList;
        readonly IIntegerType<T> intType;

        public EACodeLanguageDisassembler(
            ICodeTemplateStorer codeStorage,
            IPointerMaker<T> pointerMaker,
            Tuple<string, List<Priority>>[][] pointerList,
            IIntegerType<T> intType)
        {
            this.codeStorage = codeStorage;
            this.pointerMaker = pointerMaker;
            this.pointerList = pointerList;
            this.intType = intType;
        }

        public IEnumerable<string[]> Disassemble(byte[] code, int offset, int length, Priority priority, ILog log, bool addEndingLines)
        {
            var lines = new SortedDictionary<int, Code>();

            var priorities = new List<Priority>
            {
                priority,
                Priority.low
            };

            foreach (var codes in FindTemplates(code, offset, length, lines, priorities, log))
            {
                lines[codes.Offset] = codes;
            }

            var labels = new SortedDictionary<int, string>();
            FindLables(lines, labels);

            MergeRepeatableCodes(lines, x => !labels.ContainsKey(x));
            
            return GetLines(lines, labels, addEndingLines);
        }

        public IEnumerable<string[]> DisassembleChapter(byte[] code, int offset, ILog log, bool addEndingLines)
        {
            var pointerlistValues = new List<int>();
            var lines = new SortedDictionary<int, Code>();
            var labels = new SortedDictionary<int, string>();
            labels[offset] = "PointerList";
            
            foreach (var item in this.pointerList.SelectMany(x => x).Index())
            {
                int pointerOffset = offset + 4 * item.Item1;
                T pointer = intType.FromBytes(code, pointerOffset);
                if (this.pointerMaker.IsAValidPointer(pointer))
                {
                    int offsetVal = intType.GetIntValue(this.pointerMaker.MakeOffset(pointer));
                    pointerlistValues.Add(offsetVal);
                    if (offsetVal > 0 && !labels.ContainsKey(offsetVal))
                    {
                        labels.Add(offsetVal, item.Item2.Item1);
                        foreach (var line in FindTemplates(code, offsetVal, lines,
                            item.Item2.Item2, x => x.EndingCode, log))
                        {
                            lines[line.Offset] = line;
                        }
                    }
                }
                else
                {
                    log.AddError("Invalid pointer {0} in offset {1} at pointer list.", intType.ToString(pointer, 16), pointerOffset);
                    return Enumerable.Empty<string[]>();
                }
            }

            FindPointedCodes(code, lines.Values, lines, log);

            FindLables(lines, labels);

            MergeRepeatableCodes(lines, x => !labels.ContainsKey(x));//Can cause labels to get omitted, needs to be fixed.

            //After merging because I want custom format
            AddPointerListCodes(offset, pointerlistValues.ToArray(), lines, log);

            return GetLines(lines, labels, addEndingLines);
        }

        public IEnumerable<string[]> DisassembleToEnd(byte[] code, int offset, Priority priority, ILog log, bool addEndingLines)
        {
            var lines = new SortedDictionary<int, Code>();

            var priorities = new List<Priority>
            {
                priority,
                Priority.low
            };

            foreach (var codeOffset in FindTemplates(code, offset, code.Length, lines, priorities, x => x.EndingCode, log))
            {
                lines[codeOffset.Offset] = codeOffset;
            }

            var labels = new SortedDictionary<int, string>();
            FindLables(lines, labels);

            MergeRepeatableCodes(lines, x => !labels.ContainsKey(x));

            return GetLines(lines, labels, addEndingLines);
        }


        public IEnumerable<string[]> Disassemble(IInputByteStream stream, int length, Priority priority, ILog log, bool addEndingLines)
        {
            var lines = new SortedDictionary<int, Code>();

            var priorities = new List<Priority>
            {
                priority,
                Priority.low
            };

            foreach (var codes in FindTemplates(stream, length, lines, priorities, log))
            {
                lines[codes.Offset] = codes;
            }

            var labels = new SortedDictionary<int, string>();
            FindLables(lines, labels);

            MergeRepeatableCodes(lines, x => !labels.ContainsKey(x));

            return GetLines(lines, labels, addEndingLines);
        }

        public IEnumerable<string[]> DisassembleChapter(IInputByteStream stream, ILog log, bool addEndingLines)
        {
            var startOffset = stream.Offset;
            var pointerlistValues = new List<int>();
            var lines = new SortedDictionary<int, Code>();
            var labels = new SortedDictionary<int, string>();
            labels[startOffset] = "PointerList";

            foreach (var item in this.pointerList.SelectMany(x => x).Index())
            {
                var aPointer = item.Item2;
                int pointerOffset = startOffset + 4 * item.Item1;
                stream.Offset = pointerOffset;
                T pointer = intType.FromStream(stream);
                if (this.pointerMaker.IsAValidPointer(pointer))
                {
                    int offsetVal = intType.GetIntValue(this.pointerMaker.MakeOffset(pointer));
                    pointerlistValues.Add(offsetVal);
                    if (offsetVal > 0 && !labels.ContainsKey(offsetVal))
                    {
                        labels.Add(offsetVal, aPointer.Item1);
                        stream.Offset = offsetVal;
                        foreach (var line in FindTemplates(stream, lines,
                            aPointer.Item2, x => x.EndingCode, log))
                        {
                            lines[line.Offset] = line;
                        }
                    }
                }
                else
                {
                    log.AddError("Invalid pointer {0} in offset {1} at pointer list.", intType.ToString(pointer, 16), pointerOffset);
                    return Enumerable.Empty<string[]>();
                }
            }

            FindPointedCodes(stream, lines.Values, lines, log);

            FindLables(lines, labels);

            MergeRepeatableCodes(lines, x => !labels.ContainsKey(x));

            //After merging because I want custom format
            AddPointerListCodes(startOffset, pointerlistValues.ToArray(), lines, log);

            return GetLines(lines, labels, addEndingLines);
        }

        public IEnumerable<string[]> DisassembleToEnd(IInputByteStream stream, Priority priority, ILog log, bool addEndingLines)
        {
            var lines = new SortedDictionary<int, Code>();

            var priorities = new List<Priority>
            {
                priority,
                Priority.low
            };

            foreach (var codeOffset in FindTemplates(stream, lines, priorities, x => x.EndingCode, log))
            {
                lines[codeOffset.Offset] = codeOffset;
            }

            var labels = new SortedDictionary<int, string>();
            FindLables(lines, labels);

            MergeRepeatableCodes(lines, x => !labels.ContainsKey(x));

            return GetLines(lines, labels, addEndingLines);
        }




        private sealed class Code
        {
            readonly private ICodeTemplate template;
            readonly private string[] text;
            readonly private int length;
            readonly private int offset;

            /// <summary>
            /// The template of this code
            /// </summary>
            public ICodeTemplate Template
            {
                get { return template; }
            }
            /// <summary>
            /// The text of this code
            /// </summary>
            public string[] Text
            {
                get { return text; }
            }
            /// <summary>
            /// Lenght of this code in bytes
            /// </summary>
            public int Length
            {
                get { return length; }
            }
            public int Offset
            {
                get { return offset; }
            }

            /// <summary>
            /// Creates a new Code from template and matching text.
            /// </summary>
            /// <param name="line">Code split to parameters</param>
            /// <param name="template">Template of this code</param>
            public Code(string[] line, ICodeTemplate template, int length, int offset)
            {
                this.template = template;
                this.text = line;
                this.length = length;
                this.offset = offset;
            }

            /// <summary>
            /// Return templates hash code
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return template.GetHashCode();
            }

            public IEnumerable<Tuple<int, Priority>> GetPointedOffsets()
            {
                var templ = template as CodeTemplate;
                if (templ != null)
                {
                    for (int i = 0; i < templ.AmountOfParams; i++)
                    {
                        if (templ[i].Pointer)
                        {
                            yield return Tuple.Create(text[i + 1].GetValue(), templ[i].PointedPriority);
                        }
                    }
                }
            }

            public string[] ReplaceOffsetsWithLables(IDictionary<int, string> lables)
            {
                string[] result = new string[text.Length];
                Array.Copy(text, result, text.Length);

                for (int i = 1; i < result.Length; i++)
                {
                    int val;
                    string labelName;
                    if (result[i].TryGetValue(out val) && lables.TryGetValue(val, out labelName))
                    {
                        result[i] = labelName;
                    }
                }
                return result;
            }
        }




        private IEnumerable<Code> FindTemplates(byte[] code, int offset, int lengthToDiss,
            SortedDictionary<int, Code> lines, IEnumerable<Priority> prioritiesToUse, ILog log)
        {
            return FindTemplates(code, offset, offset + lengthToDiss, lines, prioritiesToUse, x => false, log);
        }

        private IEnumerable<Code> FindTemplates(byte[] code, int offset,
            SortedDictionary<int, Code> lines, IEnumerable<Priority> prioritiesToUse, ILog log)
        {
            return FindTemplates(code, offset, code.Length, lines, prioritiesToUse, x => false, log);
        }

        private IEnumerable<Code> FindTemplates(byte[] code, int offset,
            SortedDictionary<int, Code> lines, IEnumerable<Priority> prioritiesToUse, Predicate<ICodeTemplate> endCondition, ILog log)
        {
            return FindTemplates(code, offset, code.Length, lines, prioritiesToUse, endCondition, log);
        }

        private IEnumerable<Code> FindTemplates(byte[] code, int offset, int lastOffset,
            SortedDictionary<int, Code> lines, IEnumerable<Priority> prioritiesToUse,
            Predicate<ICodeTemplate> endCondition, ILog log)
        {
            while (offset < lastOffset)
            {
                Code ccode;
                if (!lines.TryGetValue(offset, out ccode))
                {
                    var res = GetCode(code, offset, prioritiesToUse);
                    if (res.CausedError)
                    {
                        log.AddError(res.ErrorMessage);
                        yield break;
                    }
                    else
                    {
                        ccode = res.Result;
                        yield return res.Result;
                    }
                }
                
                if (endCondition(ccode.Template))
                {
                    yield break;
                }
                offset += ccode.Length;
            }
        }



        private IEnumerable<Code> FindTemplates(IInputByteStream stream, int lengthToDiss,
            SortedDictionary<int, Code> lines, IEnumerable<Priority> prioritiesToUse, ILog log)
        {
            return FindTemplates(stream, stream.Offset + lengthToDiss, lines, prioritiesToUse, x => false, log);
        }

        private IEnumerable<Code> FindTemplates(IInputByteStream stream,
            SortedDictionary<int, Code> lines, IEnumerable<Priority> prioritiesToUse, ILog log)
        {
            return FindTemplates(stream, stream.Length, lines, prioritiesToUse, x => false, log);
        }

        private IEnumerable<Code> FindTemplates(IInputByteStream stream,
            SortedDictionary<int, Code> lines, IEnumerable<Priority> prioritiesToUse, Predicate<ICodeTemplate> endCondition, ILog log)
        {
            return FindTemplates(stream, stream.Length, lines, prioritiesToUse, endCondition, log);
        }
        
        private IEnumerable<Code> FindTemplates(IInputByteStream stream, int lastOffset,
            SortedDictionary<int, Code> lines, IEnumerable<Priority> prioritiesToUse,
            Predicate<ICodeTemplate> endCondition, ILog log)
        {
            while (stream.Offset < lastOffset)
            {
                Code ccode;
                if (!lines.TryGetValue(stream.Offset, out ccode))
                {
                    var res = GetCode(stream, prioritiesToUse);
                    if (res.CausedError)
                    {
                        log.AddError(res.ErrorMessage);
                        yield break;
                    }
                    else
                    {
                        ccode = res.Result;
                        yield return res.Result;
                    }
                }

                if (endCondition(ccode.Template))
                {
                    yield break;
                }
            }
        }


        private CanCauseError<Code> GetCode(byte[] code, int currOffset, IEnumerable<Priority> prioritiesToUse)
        {
            return from template in codeStorage.FindTemplate(code, currOffset, prioritiesToUse)

                   from line in template.GetAssembly(code, currOffset, intType, pointerMaker)
                   
                   let length = template.GetLengthBytes(code, currOffset)

                   select new Code(line, template, length, currOffset);

            //var templateResult = codeStorage.FindTemplate(code, currOffset, prioritiesToUse);
            //if (templateResult.CausedError)
            //{
            //    return templateResult.ConvertError<Old.Code>();
            //}
            //else
            //{
            //    ICodeTemplate template = templateResult.Result;
            //    var line = template.GetAssembly(code, currOffset, intType, pointerMaker);
            //    int lengthCode = template.GetLengthBytes(code, currOffset);
            //    return line.CausedError
            //               ? line.ConvertError<Old.Code>()
            //               : new Old.Code(line.Result, template, lengthCode, currOffset);
            //}
        }

        private CanCauseError<Code> GetCode(IInputByteStream stream, IEnumerable<Priority> prioritiesToUse)
        {
            int startOffset = stream.Offset;
            return from template in codeStorage.FindTemplate(stream, prioritiesToUse)

                   from line in template.GetAssembly(stream, intType, pointerMaker)

                   let length = stream.Offset - startOffset

                   select new Code(line, template, length, startOffset);

            //var templateResult = codeStorage.FindTemplate(code, currOffset, prioritiesToUse);
            //if (templateResult.CausedError)
            //{
            //    return templateResult.ConvertError<Old.Code>();
            //}
            //else
            //{
            //    ICodeTemplate template = templateResult.Result;
            //    var line = template.GetAssembly(code, currOffset, intType, pointerMaker);
            //    int lengthCode = template.GetLengthBytes(code, currOffset);
            //    return line.CausedError
            //               ? line.ConvertError<Old.Code>()
            //               : new Old.Code(line.Result, template, lengthCode, currOffset);
            //}
        }


        private void FindPointedCodes(byte[] code, 
            IEnumerable<Code> codesToSearch, 
            SortedDictionary<int, Code> lines, ILog log)
        {
            var pointerOffsets = new SortedDictionary<int, Priority>();
            foreach (var line in codesToSearch)
            {
                foreach (var offset in line.GetPointedOffsets())
                {
                    if(!pointerOffsets.ContainsKey(offset.Item1))
                    {
                        pointerOffsets.Add(offset.Item1, offset.Item2);
                    }
                }
            }

            var offsetsToHandle = from pointerOffset in pointerOffsets
                                  where pointerOffset.Key >= minimumOffset 
                                     && pointerOffset.Key < code.Length
                                     && HandlePriority(pointerOffset.Value)
                                     && !lines.ContainsKey(pointerOffset.Key)
                                  select pointerOffset;

            var newCodes = new SortedDictionary<int, Code>();

            var usedPriorities = new[]
            {
                Priority.none,
                Priority.low
            };
            foreach (KeyValuePair<int, Priority> item in offsetsToHandle)
            {
                usedPriorities[0] = item.Value;

                foreach (var line in FindTemplates(code, item.Key, lines, usedPriorities, x => x.EndingCode, log))
                {
                    newCodes.Add(line.Offset, line);
                    lines.Add(line.Offset, line);
                }
            }

            if (newCodes.Count > 0)
            {
                FindPointedCodes(code, newCodes.Values, lines, log);
            }
        }

        private void FindPointedCodes(IInputByteStream stream,
            IEnumerable<Code> codesToSearch,
            SortedDictionary<int, Code> lines, ILog log)
        {
            var pointerOffsets = new SortedDictionary<int, Priority>();
            foreach (var line in codesToSearch)
            {
                foreach (var offset in line.GetPointedOffsets())
                {
                    if (!pointerOffsets.ContainsKey(offset.Item1))
                    {
                        pointerOffsets.Add(offset.Item1, offset.Item2);
                    }
                }
            }

            var offsetsToHandle = from pointerOffset in pointerOffsets
                                  where pointerOffset.Key >= minimumOffset
                                     && pointerOffset.Key < stream.Length
                                     && HandlePriority(pointerOffset.Value)
                                     && !lines.ContainsKey(pointerOffset.Key)
                                  select pointerOffset;

            var newCodes = new SortedDictionary<int, Code>();

            var usedPriorities = new[]
            {
                Priority.none,
                Priority.low
            };
            foreach (KeyValuePair<int, Priority> item in offsetsToHandle)
            {
                usedPriorities[0] = item.Value;
                stream.Offset = item.Key;
                foreach (var line in FindTemplates(stream, lines, usedPriorities, x => x.EndingCode, log))
                {
                    newCodes.Add(line.Offset, line);
                    lines.Add(line.Offset, line);
                }
            }

            if (newCodes.Count > 0)
            {
                FindPointedCodes(stream, newCodes.Values, lines, log);
            }
        }





        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines">Needs to be sorted by offset.</param>
        /// <param name="lables"></param>
        /// <param name="addEndingMessages"></param>
        /// <returns></returns>
        private IEnumerable<string[]> GetLines(
            IEnumerable<KeyValuePair<int, Code>> lines,
            IDictionary<int, string> lables,
            bool addEndingMessages)
        {
            var emptyLine = new string[0];
            bool addedLine = false;
            bool enderLineAdded = false;
            int latestOffset = 0;

            foreach (var line in lines)
            {
                int currentOffset = line.Key;
                Code code = line.Value;
                if (line.Key != latestOffset)
                {
                    if (addEndingMessages && !enderLineAdded && latestOffset > 0)
                    {
                        if (!addedLine) yield return emptyLine;
                        addedLine = true;
                        foreach (var item in GetEnderLines(latestOffset))
                        {
                            yield return item;
                        }
                        yield return emptyLine;
                        enderLineAdded = true;
                    }
                    if (!addedLine) yield return emptyLine;
                    addedLine = true;
                    yield return new string[] { offsetChangerName, currentOffset.ToHexString("$") };
                }
                string labelName;
                if (lables.TryGetValue(currentOffset, out labelName))
                {
                    if (!addedLine) yield return emptyLine;
                    addedLine = true;
                    yield return (labelName + ":").GetArray();
                }
                enderLineAdded = false;
                yield return code.ReplaceOffsetsWithLables(lables);

                if (code.Template.EndingCode)
                {
                    yield return emptyLine;
                    addedLine = true;
                }
                else addedLine = false;
                latestOffset = currentOffset + code.Length;
            }

            if (addEndingMessages && !enderLineAdded)
            {
                if (!addedLine) yield return emptyLine;
                foreach (var item in GetEnderLines(latestOffset))
                {
                    yield return item;
                }
            }
        }

        private void AddPointerListCodes(int offset, int[] pointerListValues,
            SortedDictionary<int, Code> lines, ILog log)
        {
            var pointerTemplateResult = codeStorage.FindTemplate("POIN", Priority.pointer);
            if (pointerTemplateResult.CausedError)
            {
                log.AddError(pointerTemplateResult.ErrorMessage);
            }
            else
            {
                var pointerTemplate = pointerTemplateResult.Result;
                int totalIndex = 0;
                foreach (var pointer in this.pointerList)
                {
                    var line = new List<string>
                    {
                        pointerTemplate.Name
                    };
                    int thisOffset = offset + 4 * totalIndex;
                    for (int j = 0; j < pointer.Length; j++)
                    {
                        line.Add(pointerListValues[totalIndex].ToHexString("$"));
                        totalIndex++;
                    }
                    lines[thisOffset] = new Code(line.ToArray(), pointerTemplate, pointer.Length * 4, thisOffset);
                }
            }
        }

        private void FindLables(IDictionary<int, Code> lines,
            IDictionary<int, string> lables)
        {
            foreach (var line in lines)
            {
                foreach (var priorityOffset in line.Value.GetPointedOffsets())
                {
                    int offset = priorityOffset.Item1;
                    bool hasLine = lines.ContainsKey(offset);
                    bool noLable = !lables.ContainsKey(offset);
                    if (hasLine && noLable)
                    {
                        lables.Add(offset, "label" + (lables.Count + 1));
                    }
                }
            }
        }

        private bool HandlePriority(Priority priority)
        {
            return priority != Priority.pointer
                && priority != Priority.unknown
                && priority != Priority.ASM
                && priority != Priority.reinforcementData;
        }

        private void MergeRepeatableCodes(SortedDictionary<int, Code> lines, Predicate<int> isAllowed)
        {
            int[] codeOffsets = lines.Keys.ToArray();
            for (int i = 0; i < codeOffsets.Length; i++)
            {
                int currentOffset = codeOffsets[i];
                Code line = lines[currentOffset];
                int maxRepetition = line.Template.MaxRepetition;
                if (maxRepetition > 1)
                {
                    int tempOffset = currentOffset + line.Length;
                    var toJoin = new List<Code>();

                    while (lines.ContainsKey(tempOffset) &&
                        isAllowed(tempOffset) &&
                        toJoin.Count < maxRepetition - 1 &&
                        lines[tempOffset].Template == line.Template)
                    {
                        Code lineToJoin = lines[tempOffset];
                        toJoin.Add(lineToJoin);
                        lines.Remove(tempOffset);
                        tempOffset += lineToJoin.Length;
                    }

                    if (toJoin.Count > 0)
                    {
                        var newText = new List<string>((toJoin.Count + 1) *
                                                                (line.Text.Length - 1) + 1);
                        newText.AddRange(line.Text);
                        int length = line.Length;
                        foreach (Code codeToJoin in toJoin)
                        {
                            length += codeToJoin.Length; 
                                 
                            for (int j = 1; j < codeToJoin.Text.Length; j++)
                            {
                                newText.Add(codeToJoin.Text[j]);
                            }
                        }
                        line = new Code(newText.ToArray(), line.Template, length, currentOffset);
                        lines[currentOffset] = line;
                    }
                    i += toJoin.Count;
                }
            }
        }
        
        private IEnumerable<Code> MergeRepeatableCodes(IEnumerable<Code> codes, Predicate<int> canMergeOver)
        {
            var enumerator = codes.GetEnumerator();

            var toJoin = new List<Code>();
            while (enumerator.MoveNext())
            {
                var firstCode = enumerator.Current;
                var offset = firstCode.Offset;
                int maxRepetition = firstCode.Template.MaxRepetition;

                toJoin.Add(firstCode);
                while (toJoin.Count < maxRepetition && enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (!canMergeOver(current.Offset) ||
                        current.Template != firstCode.Template)
                        break;
                    toJoin.Add(current);
                }

                if (toJoin.Count == 1)
                {
                    yield return firstCode;
                }
                else if (toJoin.Count > 1)
                {
                    var newText = new List<string>((toJoin.Count + 1) *
                                                            (firstCode.Text.Length - 1) + 1);
                    int length = 0;
                    foreach (Code codeToJoin in toJoin)
                    {
                        length += codeToJoin.Length;
                        newText.AddRange(codeToJoin.Text);
                    }
                    yield return new Code(newText.ToArray(), firstCode.Template, length, offset);
                }
                toJoin.Clear();
            }
        }

        private IEnumerable<Tuple<int, IEnumerable<Code>>> GetConnectedComponents(SortedDictionary<int, Code> lines)
        {
            int[] codeOffsets = lines.Keys.ToArray();            
            var coll = new List<Code>();

            for (int i = 0; i < codeOffsets.Length; i++)
            {
                int offset = codeOffsets[i];

                var runningOffset = offset;
                while (true)
                {
                    Code code;
                    if (!lines.TryGetValue(runningOffset, out code))
                        break;
                    coll.Add(code);
                    runningOffset += code.Length;
                    i++;
                }

                yield return Tuple.Create(offset, (IEnumerable<Code>)coll.ToArray());
                coll.Clear();
            }
        }
        
        private static string[][] GetEnderLines(int endingOffset)
        {
            return new[]
            {
             "//The next line is for re-assembling purposes. Do not delete!".GetArray(),
             new []{ messagePrinterName,   "Original ending offset is " 
                + endingOffset.ToHexString("$")
                + " and the new ending offset is", currentOffsetName},

            };
        }
    }
}

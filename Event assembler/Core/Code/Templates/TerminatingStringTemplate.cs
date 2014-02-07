using System;
using System.Collections.Generic;
using System.Linq;
using Nintenlord.Collections;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression.Tree;
using Nintenlord.Event_Assembler.Core.IO.Logs;
using Nintenlord.Utility;
using Nintenlord.Event_Assembler.Core.Code.Language.Types.IntegerRepresentations;

namespace Nintenlord.Event_Assembler.Core.Code.Templates
{
    /// <summary>
    /// Template for terminating string of data
    /// </summary>
    sealed class TerminatingStringTemplate : ICodeTemplate
    {
        readonly TemplateParameter parameter;
        readonly byte[] endingValue;
        readonly string name;
        readonly int offsetMod;

        public TemplateParameter Parameter
        {
            get { return parameter; }
        }

        public TerminatingStringTemplate(
            string name, IEnumerable<TemplateParameter> parameters, int endingValue, int offsetMod)
        {
            this.offsetMod = offsetMod;
            this.parameter = parameters.First();
            this.endingValue = BitConverter.GetBytes(endingValue).Take(parameter.LenghtInBytes).ToArray();
            this.name = name;
        }

        #region ICodeTemplate Members

        public string Name
        {
            get { return name; }
        }
        public int MaxRepetition
        {
            get { return 1; }
        }
        public bool EndingCode
        {
            get { return true; }
        }
        public int OffsetMod
        {
            get { return offsetMod; }
        }
        public int AmountOfFixedCode
        {
            get { return 0; }
        }


        public bool Matches(byte[] data, int offset)
        {
            return true;
        }

        public int GetLengthBytes(byte[] code, int offset)
        {
            int currentOffset = offset;
            while (IsNotEnding(code,currentOffset))
            {
                currentOffset += parameter.LenghtInBytes;
            }
            return currentOffset - offset + endingValue.Length;
        }

        public CanCauseError<string[]> GetAssembly<T>(byte[] code, int offset, IIntegerType<T> intType, IPointerMaker<T> pointerMaker)
        {
            List<string> assemly = new List<string>();
            assemly.Add(name);
            while (IsNotEnding(code, offset))
            {
                T[] value = parameter.GetValues(code, offset, intType);
                assemly.Add(intType.ToString(value[0], parameter.NumberBase));
                offset += parameter.LenghtInBytes;
            }
            return assemly.ToArray();
        }
        
        public bool Matches(Language.Types.Type[] code)
        {
            foreach (var item in code)
            {
                if (!parameter.CompatibleType(item))
                {
                    return false;
                }
            }
            return true;
        }

        public int GetLengthBytes<T>(IExpression<T>[] code)
        {
            return (code.Length + 1) * parameter.LenghtInBytes;
        }

        public CanCauseError<byte[]> GetData<T>(IExpression<T>[] code, Func<string, CanCauseError<T>> getSymbolValue, IIntegerType<T> intType, IPointerMaker<T> pointerMaker)
        {
            List<byte> bytes = new List<byte>(0x20);
            for (int i = 0; i < code.Length; i++)
            {
                var values = CodeTemplate.GetValues(code[i], parameter, getSymbolValue, intType, pointerMaker);
                if (values.CausedError)
                {
                    return values.ConvertError<byte[]>();
                }
                else
                {
                    byte[] temp = new byte[parameter.LenghtInBytes];
                    parameter.InsertValues(values.Result, temp, intType);
                    bytes.AddRange(temp);
                }
            }
            bytes.AddRange(endingValue);

            return bytes.ToArray();
        }

        #endregion

        #region IParameterized Members

        public int MinAmountOfParameters
        {
            get { return -1; }
        }

        public int MaxAmountOfParameters
        {
            get { return -1; }
        }

        #endregion

        private bool IsNotEnding(byte[] code, int currentOffset)
        {
            return !code.Equals(currentOffset, endingValue, 0, parameter.LenghtInBytes);
        }
        private bool IsNotEnding(IO.Input.IInputByteStream stream)
        {
            var end = stream.PeekBytes(parameter.LenghtInBytes);
            return endingValue.Equals(0, end, 0, endingValue.Length);
        }


        public bool Matches(IO.Input.IInputByteStream stream)
        {
            return true;
        }

        public CanCauseError<string[]> GetAssembly<T>(IO.Input.IInputByteStream stream, IIntegerType<T> intType, IPointerMaker<T> pointerMaker)
        {
            List<string> assemly = new List<string>();
            assemly.Add(name);
            while (IsNotEnding(stream))
            {
                T[] value = parameter.GetValues(stream, intType);
                assemly.Add(intType.ToString(value[0], parameter.NumberBase));
            }
            stream.ReadBytes(parameter.LenghtInBytes);
            return assemly.ToArray();
        }
    }
}

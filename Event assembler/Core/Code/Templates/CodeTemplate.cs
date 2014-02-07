using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nintenlord.Collections;
using Nintenlord.Event_Assembler.Core.Code.Language;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression.Tree;
using Nintenlord.Event_Assembler.Core.IO.Logs;
using Nintenlord.Utility;
using Nintenlord.Utility.Primitives;
using Nintenlord.Utility.Strings;
using EAType = Nintenlord.Event_Assembler.Core.Code.Language.Types.Type;
using Nintenlord.Event_Assembler.Core.Code.Language.Types.IntegerRepresentations;

namespace Nintenlord.Event_Assembler.Core.Code.Templates
{
    /// <summary>
    /// Template for stored text code
    /// </summary>
    sealed class CodeTemplate : ICodeTemplate, IEnumerable<TemplateParameter>, INamed<int>
    {
        readonly string name;
        readonly int lenght;
        readonly int id;
        readonly byte[] baseData;
        readonly bool canBeRepeated;
        readonly bool checkForProblems;
        readonly bool isEndingCode;
        readonly int offsetMod;
        readonly int amountOfFixedCode;
        readonly bool canBeAssembled;
        readonly bool canBeDisassembled;
        readonly List<TemplateParameter> parameters;
        readonly List<TemplateParameter> fixedParameters;       
        
        public int Length
        {
            get { return lenght; }
        }
        public int LengthInBytes
        {
            get
            {
                return lenght / 8;
            }
        }
        public int AmountOfParams
        {
            get { return parameters.Count; }
        }
        public TemplateParameter this[int i]
        {
            get 
            {
                if (i < 0)
                {
                    throw new IndexOutOfRangeException();
                }
                else if (canBeRepeated)
                {
                    return parameters[i % AmountOfParams]; 
                }
                return parameters[i]; 
            }
        }
        public bool CanBeDisassembled
        {
            get { return canBeDisassembled; }
        }
        public bool CanBeAssembled
        {
            get { return canBeAssembled; }
        }

        public CodeTemplate(string name, int id, int lenght, IEnumerable<TemplateParameter> parameters, bool canBeRepeated, bool chechForProblems, bool end, int offsetMode, bool canBeAssembled, bool canBeDisassembled)
        {
            this.offsetMod = offsetMode;
            this.isEndingCode = end;
            this.checkForProblems = chechForProblems;
            this.canBeRepeated = canBeRepeated;
            this.lenght = lenght;
            this.name = name;
            this.id = id;
            this.canBeAssembled = canBeAssembled;
            this.canBeDisassembled = canBeDisassembled;

            this.parameters = new List<TemplateParameter>(parameters.Count());
            this.fixedParameters = new List<TemplateParameter>(parameters.Count());

            baseData = new byte[LengthInBytes];
            if (id != 0)
            {
                baseData[0] = (byte)(id & 0xFF);
                baseData[1] = (byte)((id >> 8) & 0xFF);
            }

            foreach (var parameter in parameters)//apply and remove fixed parameters
            {
                if (parameter.IsFixed)
                {
                    fixedParameters.Add(parameter);
                    if (parameter.Name.IsValidNumber())
                    {
                        int value = parameter.Name.GetValue();
                        parameter.InsertValues(value.GetArray(), baseData);
                    }
                    else
                    {
                        throw new ArgumentException("The name of fixed parameter is" +
                                                " not a number: " + parameter.Name);
                    }
                }
                else
                {
                    this.parameters.Add(parameter);
                }
            }
            this.amountOfFixedCode = 0;
            if (id != 0) amountOfFixedCode += 2;
            foreach (var item in fixedParameters)
            {
                amountOfFixedCode += item.LenghtInBytes;
            }

            if (checkForProblems)
            {
                if (!CheckIfWorks())
                {
                    throw new ArgumentException("Argumenst are not valid in code: " + name + " " 
                        + parameters.ToElementWiseString(", ","{","}"));
                }
            }
        }

        private bool CheckIfWorks()
        {
            if (id != 0 && this.LengthInBytes < 2) //Template with ID must have atleast 2 bytes 
            {
                return false;
            }

            if (this.lenght < 0)
            {
                return false;
            }

            if (canBeRepeated && (parameters.Count != 1))
            {
                return false;
            }

            bool[] usedBits = new bool[this.lenght];
            for (int i = 0; i < usedBits.Length; i++)
            {
                usedBits[i] = false;
            }
            if (id != 0)//ID uses first 2 bytes 
            {
                for (int i = 0; i < 16; i++)
                {
                    usedBits[i] = true;
                }
            }

            foreach (TemplateParameter parameter in parameters)
            {
                if (parameter.LastPosition > this.lenght 
                    || parameter.Position < 0)//make sure there are no overflows
                {
                    return false;
                }
                for (int i = parameter.Position; i < parameter.LastPosition; i++)
                {
                    if (usedBits[i])//make sure no parameter collisions
                    {
                        return false;
                    }
                    usedBits[i] = true;
                }
            }

            return true;
        }

        private CanCauseError<byte[]> GetDataUnit<T>(IExpression<T>[] parameters, Func<string, CanCauseError<T>> getSymbolValue, IIntegerType<T> intType, IPointerMaker<T> pointerMaker)
        {
            byte[] data = baseData.Clone() as byte[];
            for (int i = 0; i < parameters.Length; i++)
            {
                TemplateParameter parameter = this[i];

                if (parameter.Lenght > 0)
                {
                    var values = GetValues(parameters[i], parameter, getSymbolValue, intType, pointerMaker);
                    if (values.CausedError)
                    {
                        return values.ConvertError<byte[]>();
                    }
                    else
                    {
                        parameter.InsertValues(values.Result, data, intType);
                    }

                }
            }
            return data;
        }

        internal static CanCauseError<T[]> GetValues<T>(
            IExpression<T> parameter, 
            TemplateParameter paramTemp, 
            Func<string, CanCauseError<T>> getSymbolValue,             
            IIntegerType<T> intType,
            IPointerMaker<T> pointerMaker)
        {
            T[] values;
            if (parameter is ExpressionList<T>)
            {
                var list = parameter as ExpressionList<T>;
                values = new T[list.ComponentCount];
                for (int j = 0; j < list.ComponentCount; j++)
                {
                    var error = Folding.Fold<T>(list[j], getSymbolValue, intType);
                    if (error.CausedError)
                    {
                        return error.ConvertError<T[]>();
                    }
                    else
                    {
                        values[j] = error.Result;
                    }
                }
            }
            else
            {
                var error = Folding.Fold(parameter, getSymbolValue, intType);
                if (error.CausedError)
                {
                    return error.ConvertError<T[]>();
                }
                else
                {
                    if (paramTemp.Pointer && pointerMaker != null)
                    {
                        values = new T[] { pointerMaker.MakePointer(error.Result) };//error.Result 
                    }
                    else
                    {
                        values = new T[] { error.Result };
                    }
                }
            }
            return values;
        }

        #region ICodeTemplate Members

        public string Name
        {
            get { return name; }
        }
        public bool EndingCode
        {
            get { return isEndingCode; }
        }
        public int MaxRepetition
        {
            get 
            {
                if (canBeRepeated)
                {
                    return 4;
                }
                else
                {
                    return 1;
                } 
            }
        }
        public int OffsetMod
        {
            get { return offsetMod; }
        }
        public int AmountOfFixedCode
        {
            get { return amountOfFixedCode; }
        }

        
        public bool Matches(byte[] data, int offset)
        {
            if (!canBeDisassembled)
                return false;

            if (offset * 8 + this.lenght > data.Length * 8)//If there isn't room
                return false;
            if (offset % offsetMod != 0)
                return false;
            if (this.id == 0 && this.fixedParameters.Count == 0 && this.parameters.Count == 0)//If test can't fail
                return true;

            if (this.checkForProblems)
            {
                if (this.id != 0 && id != data[offset] + (data[offset + 1] << 8))
                    return false;
            }

            foreach (TemplateParameter parameter in fixedParameters)
            {
                byte[] valueBytes = baseData.GetBits(parameter.Position, parameter.Lenght);
                byte[] toCompare = data.GetBits(offset * 8 + parameter.Position, parameter.Lenght);
                if (!valueBytes.SequenceEqual(toCompare))
                {
                    return false;
                }
            }
            //foreach (TemplateParameter parameter in parameters)
            //{
            //    if (parameter.pointer)
            //    {
            //        byte[] toCompare = data.GetBits(offset * 8 + parameter.position, parameter.lenght);
            //        if (!this.pointerMaker.IsAValidPointer(BitConverter.ToInt32(toCompare, 0)))
            //        {
            //            return false;
            //        }
            //    }
            //}

            return true;
        }

        public int GetLengthBytes(byte[] code, int offset)
        {
            return this.LengthInBytes;
        }

        public CanCauseError<string[]> GetAssembly<T>(byte[] code, int offset, IIntegerType<T> intType, IPointerMaker<T> pointerMaker)
        {
            string[] assembly = new string[this.AmountOfParams + 1];
            assembly[0] = this.name;

            for (int i = 0; i < parameters.Count; i++)
            {
                TemplateParameter parameter = this[i];
                StringBuilder builder = new StringBuilder(parameter.Lenght / 2);

                T[] values = parameter.GetValues(code, offset, intType);
                if (values.Length > 1)
                {
                    builder.Append("[");
                    for (int j = 0; j < values.Length; j++)
                    {
                        builder.Append(intType.ToString(values[j], parameter.NumberBase));
                        if (j != parameter.MaxDimensions - 1)
                        {
                            builder.Append(",");
                        }
                    }
                    builder.Append("]");
                }
                else
                {
                    T value = values[0];
                    if (parameter.Pointer)
                    {
                        if (pointerMaker.IsAValidPointer(value))
                        {
                            value = pointerMaker.MakeOffset(value);
                        }
                        else
                        {
                            value = intType.FromInt(0);
                        }
                    }
                    builder.Append(intType.ToString(value, parameter.NumberBase));
                }
                assembly[i + 1] = builder.ToString();
            }

            return assembly;
        }
        

        public bool Matches(EAType[] paramTypes)
        {
            if (!canBeAssembled)
                return false;
            
            if (canBeRepeated)
            {
                if (this.AmountOfParams == 1)
                {
                    for (int i = 0; i < paramTypes.Length; i++)
                    {
                        if (!this[i].CompatibleType(paramTypes[i]))
                        {
                            return false;
                        }
                    }
                }
                else return false;
            }
            else
            {
                if (this.AmountOfParams == paramTypes.Length)
                {
                    for (int i = 0; i < this.AmountOfParams; i++)
                    {
                        if (!this[i].CompatibleType(paramTypes[i]))
                        {
                            return false;
                        }
                    }
                }
                else return false;
            }
            return true;
        }

        public int GetLengthBytes<T>(IExpression<T>[] code)
        {
            if (this.canBeRepeated)
            {
                return LengthInBytes * code.Length;
            }
            else
            {
                return LengthInBytes;
            }
        }

        public CanCauseError<byte[]> GetData<T>(IExpression<T>[] code, Func<string, CanCauseError<T>> getSymbolValue, IIntegerType<T> intType, IPointerMaker<T> pointerMaker)
        {
            if (this.canBeRepeated)
            {
                List<byte> data = new List<byte>(code.Length * this.LengthInBytes);

                int repeats = code.Length / this.AmountOfParams;

                for (int i = 0; i < repeats; i++)
                {
                    var newData = this.GetDataUnit(new[] { code[i] }, getSymbolValue, intType, pointerMaker);
                    if (newData.CausedError)
                    {
                        return newData.ConvertError<byte[]>();
                    }
                    else
                    {
                        data.AddRange(newData.Result);
                    }
                }

                return data.ToArray();
            }
            else
            {
                return GetDataUnit(code, getSymbolValue, intType, pointerMaker);
            }

        }

        #endregion
        
        #region IEnumerable<Parameter> Members

        public IEnumerator<TemplateParameter> GetEnumerator()
        {
            return parameters.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return parameters.GetEnumerator();
        }

        #endregion

        #region IParameterized Members

        public int MinAmountOfParameters
        {
            get { return AmountOfParams; }
        }

        public int MaxAmountOfParameters
        {
            get { return AmountOfParams; }
        }

        #endregion

        #region INamed<int> Members

        int INamed<int>.Name
        {
            get { return id; }
        }

        #endregion

        public override string ToString()
        {
            if (parameters.Count > 0)
            {
                return string.Format("{0} {1}", name, parameters.ToElementWiseString(", ", "", ""));
            }
            else
            {
                return name;
            }
        }

        public override int GetHashCode()
        {
            return name.GetHashCode() ^ id;
        }


        public bool Matches(IO.Input.IInputByteStream stream)
        {
            throw new NotImplementedException();
        }

        public CanCauseError<string[]> GetAssembly<T>(IO.Input.IInputByteStream stream, IIntegerType<T> intType, IPointerMaker<T> pointerMaker)
        {
            throw new NotImplementedException();
        }
    }
}

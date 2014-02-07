// -----------------------------------------------------------------------
// <copyright file="GenericFE8Template.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Nintenlord.Event_Assembler.Core.Code.Templates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Nintenlord.Event_Assembler.Core.Code.Language.Expression;
    using Nintenlord.Utility;
    using Nintenlord.Event_Assembler.Core.Code.Language.Types.IntegerRepresentations;

    /// <summary>
    /// Generic FE8 code to help disassembly
    /// </summary>
    public class GenericFE8Template : ICodeTemplate
    {
        #region ICodeTemplate Members

        public int MaxRepetition
        {
            get { return 1; }
        }

        public bool EndingCode
        {
            get { return false; }
        }

        public int OffsetMod
        {
            get { return 2; }
        }

        public int AmountOfFixedCode
        {
            get { return 0; }
        }


        public bool Matches(byte[] data, int offset)
        {
            return data[offset + 1] > 1;
        }

        public int GetLengthBytes(byte[] data, int offset)
        {
            return (data[offset] >> 4) * 2;
        }

        public CanCauseError<string[]> GetAssembly<T>(byte[] data, int offset, IIntegerType<T> intType, IPointerMaker<T> pointerMaker)
        {
            var length = (data[offset] >> 4);
            List<string> code = new List<string>();
            code.Add(this.Name);
            for (int i = 0; i < length; i++)
            {
                code.Add("0x" 
                    + data[offset + i * 2 + 1].ToString("X2") 
                    + data[offset + i * 2].ToString("X2"));
            }
            return code.ToArray();
        }
        

        public bool Matches(Language.Types.Type[] code)
        {
            throw new NotImplementedException();
        }

        public int GetLengthBytes<T>(IExpression<T>[] code)
        {
            throw new NotImplementedException();
        }

        public CanCauseError<byte[]> GetData<T>(IExpression<T>[] code, Func<string, CanCauseError<T>> getSymbolValue, IIntegerType<T> intType, IPointerMaker<T> pointerMaker)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region INamed<string> Members

        public string Name
        {
            get { return "FE8Code"; }
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

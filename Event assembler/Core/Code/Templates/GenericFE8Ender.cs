using System;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression;
using Nintenlord.Utility;
using Nintenlord.Event_Assembler.Core.Code.Language.Types.IntegerRepresentations;

namespace Nintenlord.Event_Assembler.Core.Code.Templates
{
    public class GenericFE8Ender : ICodeTemplate
    {
        #region ICodeTemplate Members

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
            get { return 2; }
        }

        public int AmountOfFixedCode
        {
            get { return 0; }
        }



        public bool Matches(byte[] data, int offset)
        {
            return data[offset + 1] == 0x1;
        }

        public int GetLengthBytes(byte[] data, int offset)
        {
            return (data[offset] >> 4) * 2;
        }

        public CanCauseError<string[]> GetAssembly<T>(byte[] data, int offset, IIntegerType<T> intType, IPointerMaker<T> pointerMaker)
        {
            return new string[] { this.Name };
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
            get { return "FE8End"; }
        }

        #endregion

        #region IParameterized Members

        public int MinAmountOfParameters
        {
            get { return 0; }
        }

        public int MaxAmountOfParameters
        {
            get { return 0; }
        }

        #endregion


        public bool Matches(IO.Input.IInputByteStream stream)
        {
            throw new NotImplementedException();
        }

        public int GetLengthBytes(IO.Input.IInputByteStream stream)
        {
            throw new NotImplementedException();
        }

        public CanCauseError<string[]> GetAssembly<T>(IO.Input.IInputByteStream stream, IIntegerType<T> intType, IPointerMaker<T> pointerMaker)
        {
            throw new NotImplementedException();
        }
    }
}
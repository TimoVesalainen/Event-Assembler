using System;
using System.Linq;
using Nintenlord.Event_Assembler.Core.Code.Language;
using Nintenlord.Event_Assembler.Core.Code.Language.Old;
using Nintenlord.Collections;
using Nintenlord.Utility;
using Nintenlord.Utility.Primitives;
using Nintenlord.Utility.Strings;
using Nintenlord.Event_Assembler.Core.Code.Language.Types.IntegerRepresentations;

namespace Nintenlord.Event_Assembler.Core.Code.Templates
{
    /// <summary>
    /// Parameter for code templates
    /// </summary>
    sealed class TemplateParameter
    {
        readonly public string Name;
        readonly public int Position;
        readonly public int Lenght;
        
        readonly public int MinDimensions;
        readonly public int MaxDimensions;

        readonly public bool Pointer;
        readonly public Priority PointedPriority;

        readonly public bool IsFixed;
        readonly public bool Signed;

        readonly public int NumberBase;

        /// <summary>
        /// TODO: Check that parameters with uneven amount of bits don't cause any problems
        /// </summary>
        public int LenghtInBytes
        {
            get { return Lenght / 8; }
        }
        public int PositionInBytes
        {
            get { return Position / 8; }
        }
        public int LastPosition
        {
            get
            {
                return Position + Lenght;
            }
        }
        public int LastPositionInBytes
        {
            get
            {
                return (Position + Lenght)/8;
            }
        }
        public int BitsPerCoord
        {
            get { return Lenght / MaxDimensions; }
        }

        public TemplateParameter(string name, int position, int lenght, int minDimensions, 
            int maxDimensions, bool pointer, Priority pointedPriority, bool signed, bool isFixed, int numberBase)
        {
            this.Signed = signed;
            this.PointedPriority = pointedPriority;
            this.Name = name;
            this.Position = position;
            this.Lenght = lenght;
            this.MinDimensions = minDimensions;
            this.MaxDimensions = maxDimensions;
            this.Pointer = pointer;
            this.IsFixed = isFixed;
            this.NumberBase = numberBase;
        }
        
        public T[] GetValues<T>(byte[] data, int codeOffset, IIntegerType<T> intType)
        {
            //throw new NotImplementedException();
            T[] result = new T[MaxDimensions];
            for (int i = 0; i < result.Length; i++)
            {
                int coordOffsetBit = codeOffset * 8 + Position + i * BitsPerCoord;

                byte[] value = data.GetBits(coordOffsetBit, BitsPerCoord);
                Array.Resize(ref value, 4);
                if (this.Signed && value.GetBits(BitsPerCoord - 1, 1)[0] == 1)
                {
                    value.WriteTo(BitsPerCoord, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 
                        sizeof(int) * 8 - BitsPerCoord);
                }
                result[i] = intType.FromBytes(value);
            }
            return result;
        }

        public bool InsertValues<T>(T[] values, byte[] code, IIntegerType<T> intType)
        {
            //throw new NotImplementedException();
            for (int i = 0; i < values.Length; i++)
            {
                byte[] value = intType.GetBytes(values[i]);
                code.WriteTo(Position + i * BitsPerCoord, value, BitsPerCoord);
            }
            return true;
        }

        public bool InsertValues(int[] values, byte[] code)
        {
            //throw new NotImplementedException();
            for (int i = 0; i < values.Length; i++)
            {
                byte[] value = BitConverter.GetBytes(values[i]);
                code.WriteTo(Position + i * BitsPerCoord, value, BitsPerCoord);
            }
            return true;
        }
        
        public bool CompatibleType(Language.Types.Type type)
        {
            switch (type.type)
            {
                case Nintenlord.Event_Assembler.Core.Code.Language.Types.MetaType.Atom:
                    return (this.MinDimensions == this.MaxDimensions) && (this.MinDimensions == 1);
                case Nintenlord.Event_Assembler.Core.Code.Language.Types.MetaType.Vector:
                    return type.ParameterCount.IsInRange(this.MinDimensions, this.MaxDimensions) 
                        && type.vectorParameterTypes.All(x => x.type == Language.Types.MetaType.Atom);
                default:
                    throw new ArgumentException();
            }
        }
        
        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            if (obj is TemplateParameter)
            {
                return Equals(obj as TemplateParameter);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() 
                ^ MinDimensions 
                ^ MaxDimensions;
        }


        public T[] GetValues<T>(IO.Input.IInputByteStream stream, IIntegerType<T> intType)
        {
            //throw new NotImplementedException();
            T[] result = new T[MaxDimensions];
            //TODO: Doesn't work like this
            var paramData = stream.ReadBytes(this.LenghtInBytes);

            for (int i = 0; i < result.Length; i++)
            {
                int coordOffsetBit = Position + i * BitsPerCoord;

                byte[] value = paramData.GetBits(coordOffsetBit, BitsPerCoord);
                //Array.Resize(ref value, 4);
                if (this.Signed && value.GetBits(BitsPerCoord - 1, 1)[0] == 1)
                {
                    value.WriteTo(BitsPerCoord, new byte[] { 0xFF }.Repeat().Take(value.Length).ToArray(),
                        value.Length * 8 - BitsPerCoord);
                }
                result[i] = intType.FromBytes(value);
            }
            return result;
        }
    }
}
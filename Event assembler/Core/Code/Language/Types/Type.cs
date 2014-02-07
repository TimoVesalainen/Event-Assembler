// -----------------------------------------------------------------------
// <copyright file="Type.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Nintenlord.Collections;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression;

namespace Nintenlord.Event_Assembler.Core.Code.Language.Types
{
    /// <summary>
    /// Class representing type information.
    /// </summary>
    public sealed class Type
    {
        public readonly MetaType type;
        public readonly Type[] vectorParameterTypes;

        public int ParameterCount
        {
            get { return vectorParameterTypes.Length; }
        }

        private Type(MetaType type)
        {
            this.type = type;
            this.vectorParameterTypes = null;
        }

        private Type(Type[] parameters)
        {
            this.type = MetaType.Vector;
            this.vectorParameterTypes = parameters;
        }

        public static readonly Type Atom = new Type(MetaType.Atom);

        private static Dictionary<int, Type> vectorTypes = new Dictionary<int,Type>();
        public static Type Vector(int paramCount)
        {
            Type result;
            if (!vectorTypes.TryGetValue(paramCount, out result))
            {
                result = vectorTypes[paramCount] = new Type(Repeat(paramCount, Atom).ToArray());
            }
            return result;
        }

        public static IEnumerable<T> Repeat<T>(int count, T toRepeat)
        {
            for (int i = 0; i < count; i++)
            {
                yield return toRepeat;
            }
        }

        public static Type GetType<T>(IExpression<T> typed)
        {
            if (IsAtom(typed))
            {
                return GetAtomicType(typed);
            }
            else
            {
                var types = typed.GetChildren().Select(GetType).ToArray();

                //Cache common, shallow types.
                if (types.All(x => x.type == MetaType.Atom))
                {
                    Type type;
                    if (!vectorTypes.TryGetValue(types.Length, out type))
                    {
                        type = vectorTypes[types.Length] = new Type(types);
                    }
                    return type;
                }
                else
                {
                    return new Type(types);
                }                
            }
        }

        private static bool IsAtom<T>(IExpression<T> typed)
        {
            switch (typed.Type)
            {
                case EAExpressionType.XOR:
                case EAExpressionType.AND:
                case EAExpressionType.OR:
                case EAExpressionType.LeftShift:
                case EAExpressionType.RightShift:
                case EAExpressionType.Division:
                case EAExpressionType.Multiply:
                case EAExpressionType.Modulus:
                case EAExpressionType.Minus:
                case EAExpressionType.Sum:
                case EAExpressionType.Value:
                case EAExpressionType.Symbol:
                    return true;

                case EAExpressionType.List:
                    return false;

                default:
                    throw new ArgumentException();
            }
        }

        private static Type GetAtomicType<T>(IExpression<T> typed)
        {
            return Atom;
        }

        public override string ToString()
        {
            return this.type == MetaType.Vector
                       ? vectorParameterTypes.ToElementWiseString(", ", "[", "]")
                       : type.ToString();
        }
    }
}

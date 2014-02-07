// -----------------------------------------------------------------------
// <copyright file="Folding.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression.Tree;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression.Tree.MathOperators;
using Nintenlord.Utility;
using Nintenlord.Event_Assembler.Core.Code.Language.Types.IntegerRepresentations;

namespace Nintenlord.Event_Assembler.Core.Code.Language.Expression
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    static public class Folding
    {
        static public int Fold(IExpression<int> expression)
        {
            var op = expression as BinaryOperator<int>;
            switch (expression.Type)
            {
                case EAExpressionType.Value:
                    return ((ValueExpression<int>)expression).Value;
                case EAExpressionType.Division:
                    return Fold(op.First) / Fold(op.Second);
                case EAExpressionType.Multiply:
                    return Fold(op.First) * Fold(op.Second);
                case EAExpressionType.Modulus:
                    return Fold(op.First) % Fold(op.Second);
                case EAExpressionType.Minus:
                    return Fold(op.First) - Fold(op.Second);
                case EAExpressionType.Sum:
                    return Fold(op.First) + Fold(op.Second);
                case EAExpressionType.XOR:
                    return Fold(op.First) ^ Fold(op.Second);
                case EAExpressionType.AND:
                    return Fold(op.First) & Fold(op.Second);
                case EAExpressionType.OR:
                    return Fold(op.First) | Fold(op.Second);
                case EAExpressionType.LeftShift:
                    return Fold(op.First) << Fold(op.Second);
                case EAExpressionType.RightShift:
                    return Fold(op.First) >> Fold(op.Second);
                default:
                    throw new ArgumentException();
            }
        }
        
        static public CanCauseError<int> TryFold(IExpression<int> expression)
        {
            BinaryOperator<int> op = expression as BinaryOperator<int>;
            Func<int, int, int> func;
            switch (expression.Type)
            {
                case EAExpressionType.Value:
                    return ((ValueExpression<int>)expression).Value;
                case EAExpressionType.Division:
                    func = (x, y) => x / y;
                    break;
                case EAExpressionType.Multiply:
                    func = (x, y) => x * y;
                    break;
                case EAExpressionType.Modulus:
                    func = (x, y) => x % y;
                    break;
                case EAExpressionType.Minus:
                    func = (x, y) => x - y;
                    break;
                case EAExpressionType.Sum:
                    func = (x, y) => x + y;
                    break;
                case EAExpressionType.XOR:
                    func = (x, y) => x ^ y;
                    break;
                case EAExpressionType.AND:
                    func = (x, y) => x & y;
                    break;
                case EAExpressionType.OR:
                    func = (x, y) => x | y;
                    break;
                case EAExpressionType.LeftShift:
                    func = (x, y) => x << y;
                    break;
                case EAExpressionType.RightShift:
                    func = (x, y) => x >> y;
                    break;
                default:
                    return CanCauseError<int>.Error("Unsupported type: {0}", expression.Type);
            }
            return func.Map(TryFold(op.First), TryFold(op.Second));
        }

        static public CanCauseError<int> Fold(IExpression<int> expression, Func<string, int?> symbolVals)
        {
            var op = expression as BinaryOperator<int>;
            Func<int, int, int> func;
            switch (expression.Type)
            {
                case EAExpressionType.Value:
                    return ((ValueExpression<int>)expression).Value;
                case EAExpressionType.Symbol:
                    string name = ((Symbol<int>)expression).Name;
                    int? val = symbolVals(name);
                    return val ?? CanCauseError<int>.Error("Symbol {0} isn't in scope", name);
                case EAExpressionType.Division:
                    func = (x, y) => x / y;
                    break;
                case EAExpressionType.Multiply:
                    func = (x, y) => x * y;
                    break;
                case EAExpressionType.Modulus:
                    func = (x, y) => x % y;
                    break;
                case EAExpressionType.Minus:
                    func = (x, y) => x - y;
                    break;
                case EAExpressionType.Sum:
                    func = (x, y) => x + y;
                    break;
                case EAExpressionType.XOR:
                    func = (x, y) => x ^ y;
                    break;
                case EAExpressionType.AND:
                    func = (x, y) => x & y;
                    break;
                case EAExpressionType.OR:
                    func = (x, y) => x | y;
                    break;
                case EAExpressionType.LeftShift:
                    func = (x, y) => x << y;
                    break;
                case EAExpressionType.RightShift:
                    func = (x, y) => x >> y;
                    break;
                default:
                    return CanCauseError<int>.Error("Unsupported type: {0}", expression.Type);
            }
            return func.Map(Fold(op.First, symbolVals), Fold(op.Second, symbolVals));
        }



        static public T Fold<T>(IExpression<T> expression, IIntegerType<T> integerType)
        {
            var op = expression as BinaryOperator<T>;
            T first, second;
            if (op != null)
            {
                first = Fold<T>(op.First, integerType);
                second = Fold<T>(op.Second, integerType);
            }
            else
            {
                first = second = default(T);
            }

            switch (expression.Type)
            {
                case EAExpressionType.Value:
                    return ((ValueExpression<T>)expression).Value;
                case EAExpressionType.Division:
                    return integerType.Division(first, second);
                case EAExpressionType.Multiply:
                    return integerType.Multiplication(first, second);
                case EAExpressionType.Modulus:
                    return integerType.Modulus(first, second);
                case EAExpressionType.Minus:
                    return integerType.Subtraction(first, second);
                case EAExpressionType.Sum:
                    return integerType.Addition(first, second);
                case EAExpressionType.XOR:
                    return integerType.BitwiseAnd(integerType.BitwiseOr(first, second), integerType.Complement(integerType.BitwiseOr(first, second)));
                case EAExpressionType.AND:
                    return integerType.BitwiseAnd(first, second);
                case EAExpressionType.OR:
                    return integerType.BitwiseOr(first, second);
                case EAExpressionType.LeftShift:
                    return integerType.BitShift(first, second);
                case EAExpressionType.RightShift:
                    return integerType.BitShift(first, integerType.Minus(second));
                default:
                    throw new ArgumentException();
            }
        }

        static public CanCauseError<T> TryFold<T>(IExpression<T> expression, IIntegerType<T> integerType)
        {
            var op = expression as BinaryOperator<T>;
            Func<T, T, T> func;
            switch (expression.Type)
            {
                case EAExpressionType.Value:
                    return ((ValueExpression<T>)expression).Value;
                case EAExpressionType.Division:
                    func = integerType.Division;
                    break;
                case EAExpressionType.Multiply:
                    func = integerType.Multiplication;
                    break;
                case EAExpressionType.Modulus:
                    func = integerType.Modulus;
                    break;
                case EAExpressionType.Minus:
                    func = integerType.Subtraction;
                    break;
                case EAExpressionType.Sum:
                    func = integerType.Addition;
                    break;
                case EAExpressionType.XOR:
                    func = (x, y) => integerType.BitwiseAnd(integerType.BitwiseOr(x, y), integerType.Complement(integerType.BitwiseOr(x, y)));
                    break;
                case EAExpressionType.AND:
                    func = integerType.BitwiseAnd;
                    break;
                case EAExpressionType.OR:
                    func = integerType.BitwiseOr;
                    break;
                case EAExpressionType.LeftShift:
                    func = integerType.BitShift;
                    break;
                case EAExpressionType.RightShift:
                    func = (x, y) => integerType.BitShift(x, integerType.Minus(y));
                    break;
                default:
                    func = null;
                    break;
                    //return CanCauseError<T>.Error("Unsupported type: {0}", expression.Type);
            }
            return func.Map(TryFold(op.First, integerType), TryFold<T>(op.Second, integerType));
        }

        static public CanCauseError<T> Fold<T>(IExpression<T> expression, Func<string, CanCauseError<T>> symbolVals, IIntegerType<T> integerType)
        {
            var op = expression as BinaryOperator<T>;
            Func<T, T, T> func;
            switch (expression.Type)
            {
                case EAExpressionType.Value:
                    return ((ValueExpression<T>)expression).Value;
                case EAExpressionType.Symbol:
                    return symbolVals(((Symbol<T>)expression).Name);
                case EAExpressionType.Division:
                    func = integerType.Division;
                    break;
                case EAExpressionType.Multiply:
                    func = integerType.Multiplication;
                    break;
                case EAExpressionType.Modulus:
                    func = integerType.Modulus;
                    break;
                case EAExpressionType.Minus:
                    func = integerType.Subtraction;
                    break;
                case EAExpressionType.Sum:
                    func = integerType.Addition;
                    break;
                case EAExpressionType.XOR:
                    func = (x, y) => integerType.BitwiseAnd(integerType.BitwiseOr(x, y), integerType.Complement(integerType.BitwiseOr(x, y)));
                    break;
                case EAExpressionType.AND:
                    func = integerType.BitwiseAnd;
                    break;
                case EAExpressionType.OR:
                    func = integerType.BitwiseOr;
                    break;
                case EAExpressionType.LeftShift:
                    func = integerType.BitShift;
                    break;
                case EAExpressionType.RightShift:
                    func = (x, y) => integerType.BitShift(x, integerType.Minus(y));
                    break;
                default:
                    return CanCauseError<T>.Error("Unsupported type: {0}", expression.Type);
            }
            return func.Map(Fold(op.First, symbolVals, integerType), Fold(op.Second, symbolVals, integerType));
        }

        static private Func<T, T, T> GetOperator<T>(BinaryOperator<T> op, IIntegerType<T> integerType)
        {
            Func<T, T, T> func;
            switch (op.Type)
            {
                case EAExpressionType.Division:
                    func = integerType.Division;
                    break;
                case EAExpressionType.Multiply:
                    func = integerType.Multiplication;
                    break;
                case EAExpressionType.Modulus:
                    func = integerType.Modulus;
                    break;
                case EAExpressionType.Minus:
                    func = integerType.Subtraction;
                    break;
                case EAExpressionType.Sum:
                    func = integerType.Addition;
                    break;
                case EAExpressionType.XOR:
                    func = (x, y) => integerType.BitwiseAnd(integerType.BitwiseOr(x, y), integerType.Complement(integerType.BitwiseOr(x, y)));
                    break;
                case EAExpressionType.AND:
                    func = integerType.BitwiseAnd;
                    break;
                case EAExpressionType.OR:
                    func = integerType.BitwiseOr;
                    break;
                case EAExpressionType.LeftShift:
                    func = integerType.BitShift;
                    break;
                case EAExpressionType.RightShift:
                    func = (x, y) => integerType.BitShift(x, integerType.Minus(y));
                    break;
                default:
                    func = null;
                    break;
            }
            return func;
        }
    }
}

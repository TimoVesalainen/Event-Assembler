// -----------------------------------------------------------------------
// <copyright file="MathParser2.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression.Tree;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression.Tree.MathOperators;
using Nintenlord.Event_Assembler.Core.Code.Language.Lexer;
using Nintenlord.IO.Scanners;
using Nintenlord.Parser;

namespace Nintenlord.Event_Assembler.Core.Code.Language.Parser
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class MathParser<T> : Parser<Token, IExpression<T>>
    {
        static readonly List<IDictionary<string, Func<Token, IExpression<T>, IExpression<T>, IExpression<T>>>> levels;

        static MathParser()
        {
            var bitwiseOps = new Dictionary<string, Func<Token, IExpression<T>, IExpression<T>, IExpression<T>>>
            {
                {"^" , (token, left, rigth) => new BitwiseXor<T>(left, rigth, token.Position)},
                {"|" , (token, left, rigth) => new BitwiseOr<T>(left, rigth, token.Position)},
                {"&" , (token, left, rigth) => new BitwiseAnd<T>(left, rigth, token.Position)}
            };
            var shiftOps = new Dictionary<string, Func<Token, IExpression<T>, IExpression<T>, IExpression<T>>>
            {
                {"<<" , (token, left, rigth) => new BitShiftLeft<T>(left, rigth, token.Position)},
                {">>" , (token, left, rigth) => new BitShiftRight<T>(left, rigth, token.Position)}
            };
            var addOps = new Dictionary<string, Func<Token, IExpression<T>, IExpression<T>, IExpression<T>>>
            {
                {"+" , (token, left, rigth) => new Sum<T>(left, rigth, token.Position)},
                {"-" , (token, left, rigth) => new Minus<T>(left, rigth, token.Position)}
            };
            var mulOps = new Dictionary<string, Func<Token, IExpression<T>, IExpression<T>, IExpression<T>>>
            {
                {"*" , (token, left, rigth) => new Multiply<T>(left, rigth, token.Position)},
                {"/" , (token, left, rigth) => new Division<T>(left, rigth, token.Position)},
                {"%" , (token, left, rigth) => new Modulus<T>(left, rigth, token.Position)}
            };
            levels = new List<IDictionary<string, Func<Token, IExpression<T>, IExpression<T>, IExpression<T>>>>
            {
                bitwiseOps,
                shiftOps,
                addOps,
                mulOps
            };
        }


        readonly private Func<string, T> evaluate;
        public MathParser(Func<string, T> evaluate)
        {
            this.evaluate = evaluate;                
        }

        protected override IExpression<T> ParseMain(IScanner<Token> scanner, out Match<Token> match)
        {
            var result = ParseExpression(scanner, out match);
            return result;
        }

        private IExpression<T> ParseExpression(IScanner<Token> scanner, out Match<Token> tempMatch)
        {
            return ParseLevel(scanner, 0, out tempMatch);
        }

        private IExpression<T> ParseLevel(IScanner<Token> scanner, int level, out Match<Token> match)
        {
            if (level == levels.Count)
            {
                return ParseInteger(scanner, out match);
            }

            var left = ParseLevel(scanner, level + 1, out match);

            if (!match.Success)
                return null;

            IExpression<T> result = left;
            while (true)
            {
                var token = scanner.Current;
                if (token.Type != TokenType.MathOperator)
                    break;

                var currentLevel = levels[level];

                Func<Token, IExpression<T>, IExpression<T>, IExpression<T>> creator;
                if (currentLevel.TryGetValue(token.Value, out creator))
                {
                    match++;
                    scanner.MoveNext();

                    Match<Token> secondMatch;
                    var right = ParseLevel(scanner, level + 1, out secondMatch);

                    match += secondMatch;

                    if (secondMatch.Success)
                    {
                        result = creator(token, result, right);
                    }
                    else break;
                }
                else break;
            }

            return result;
        }
        
        private IExpression<T> ParseInteger(IScanner<Token> scanner, out Match<Token> match)
        {
            bool negative = false;
            //bool inverted = false;
            match = new Match<Token>(scanner, 0);
            var token = scanner.Current;
            switch (token.Type)
            {
                case TokenType.MathOperator:
                    if (token.Value == "-")
                    {
                        scanner.MoveNext(); match++;
                        token = scanner.Current;
                        negative = true;
                        //inverted = false;
                        goto case TokenType.IntegerLiteral;
                    }
                    else if (token.Value == "+")
                    {
                        scanner.MoveNext(); match++;
                        token = scanner.Current;
                        negative = false;
                        //inverted = false;
                        goto case TokenType.IntegerLiteral;
                    }
                    //else if (token.Value == "~")
                    //{
                    //    scanner.MoveNext(); match++;
                    //    token = scanner.Current;
                    //    negative = false;
                    //    inverted = true;
                    //    goto case TokenType.IntegerLiteral;
                    //}
                    else
                    {
                        match = new Match<Token>(scanner, "Operator instead of math value");
                        return null;
                    }
                case TokenType.Symbol:
                    scanner.MoveNext(); match++;
                    return new Symbol<T>(token.Value, token.Position);

                case TokenType.IntegerLiteral:
                    scanner.MoveNext(); match++;
                    try
                    {
                        T evaluated = evaluate(negative ? "-" + token.Value : token.Value);

                        return new ValueExpression<T>(evaluated, token.Position);
                    }
                    catch (FormatException e)
                    {
                        match = new Match<Token>(scanner, e.Message);
                        return null;
                    }
                    catch (ArgumentException e)
                    {
                        match = new Match<Token>(scanner, e.Message);
                        return null;
                    }

                case TokenType.LeftParenthesis:
                    scanner.MoveNext(); match++;

                    Match<Token> tempMatch;
                    var res = ParseExpression(scanner, out tempMatch);
                    match += tempMatch;
                    if (match.Success)
                    {
                        if (scanner.Current.Type == TokenType.RightParenthesis)
                        {
                            scanner.MoveNext();
                            match++;
                            return res;
                        }
                        else
                        {
                            match = new Match<Token>(scanner, "Unclosed parenthesis");
                        }
                    }
                    return null;

                default:
                    match = new Match<Token>(scanner, "Unsupported token in math expression");
                    return null;
            }
        }
    }
}

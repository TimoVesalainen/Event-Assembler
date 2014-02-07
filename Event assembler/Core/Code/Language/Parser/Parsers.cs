using System;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression.Tree;
using Nintenlord.Event_Assembler.Core.Code.Language.Lexer;
using Nintenlord.Parser;

namespace Nintenlord.Event_Assembler.Core.Code.Language.Parser
{
    static class Parsers
    {
        public static IParser<Token, IExpression<T>> GetStatementParser<T>(Func<string, T> eval)
        {
            var parameterParser = GetParameterParser(eval);

            var codeParser = new StatementParser<T>(parameterParser).Name("Statement");

            return codeParser;
        }

        //private static IParser<Token, Parameter<T>> GetParameterParser<T>(Func<string, T> eval)
        //{
        //    var commaParser = TokenTypeParser.GetTypeParser(TokenType.Comma);
        //    var leftSquareParser = TokenTypeParser.GetTypeParser(TokenType.LeftSquareBracket);
        //    var rightSquareParser = TokenTypeParser.GetTypeParser(TokenType.RightSquareBracket);

        //    var atomParser = new MathParser2<T>(eval).Name("Atom");

        //    var vectorParser =
        //        atomParser.
        //        SepBy1(commaParser).
        //        Between(
        //            leftSquareParser,
        //            rightSquareParser).
        //        Name("Vector");

        //    var atomParam = atomParser.Transform(x => new Parameter<T>(x, x.Position));
        //    var vectorParam = vectorParser.Transform(x => new Parameter<T>(x, x[0].Position));

        //    var parameterParser = (vectorParam | atomParam).Name("Parameter");
        //    return parameterParser;
        //}

        private static IParser<Token, IExpression<T>> GetParameterParser<T>(Func<string, T> eval)
        {
            var commaParser = TokenTypeParser.GetTypeParser(TokenType.Comma);
            var leftSquareParser = TokenTypeParser.GetTypeParser(TokenType.LeftSquareBracket);
            var rightSquareParser = TokenTypeParser.GetTypeParser(TokenType.RightSquareBracket);

            var atomParser = new MathParser<T>(eval).Name("Atom");

            IParser<Token, IExpression<T>> vectorParser = null;
            var lazyVector = ParserHelpers.Lazy(() => vectorParser);

            vectorParser =
                (lazyVector | atomParser).
                SepBy1(commaParser).
                Between(
                    leftSquareParser,
                    rightSquareParser).
                Transform(x => new ExpressionList<T>(x, x[0].Position)).
                Name("Vector");
            
            var parameterParser = (atomParser | vectorParser).Name("Parameter");
            return parameterParser;
        }

        public static IParser<Token, IExpression<T>> GetMathParser<T>(Func<string, T> eval)
        {
            var symbolParser = TokenTypeParser.GetTypeParser(TokenType.Symbol);
            var integerParser = TokenTypeParser.GetTypeParser(TokenType.IntegerLiteral);
            var leftParenParser = TokenTypeParser.GetTypeParser(TokenType.LeftParenthesis);
            var rightParenParser = TokenTypeParser.GetTypeParser(TokenType.RightParenthesis);
            var mathOPParser = TokenTypeParser.GetTypeParser(TokenType.MathOperator);

            throw new NotImplementedException();
        }
    }
}

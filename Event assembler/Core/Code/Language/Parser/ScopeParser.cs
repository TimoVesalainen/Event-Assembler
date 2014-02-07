// -----------------------------------------------------------------------
// <copyright file="ScopeParser.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression;
using Nintenlord.Event_Assembler.Core.Code.Language.Expression.Tree;
using Nintenlord.Event_Assembler.Core.Code.Language.Lexer;
using Nintenlord.IO.Scanners;
using Nintenlord.Parser;

namespace Nintenlord.Event_Assembler.Core.Code.Language.Parser
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    sealed class ScopeParser<T> : Parser<Token, Scope<T>>
    {
        readonly IParser<Token, IEnumerable<IExpression<T>>> statementsParser;
        readonly IParser<Token, Token> scopeStartParser;
        readonly IParser<Token, Token> scopeEndParser;

        public ScopeParser(
            IParser<Token, IEnumerable<IExpression<T>>> statementsParser,
            IParser<Token, Token> scopeStartParser,
            IParser<Token, Token> scopeEndParser)
        {
            this.statementsParser = statementsParser;
            this.scopeStartParser = scopeStartParser;
            this.scopeEndParser = scopeEndParser;
        }

        protected override Scope<T> ParseMain(IScanner<Token> scanner, out Match<Token> match)
        {
            var scopedStuff = new List<IExpression<T>>();
            match = new Match<Token>(scanner);
            var scopeStartPos = scanner.Current.Position;

            while (true)
            {
                Match<Token> latest;
                var statements = statementsParser.Parse(scanner, out latest);
                match += latest;
                if (!match.Success) return null; //Parsing unsuccesful
                scopedStuff.AddRange(statements);

                scopeStartParser.Parse(scanner, out latest);
                if (!latest.Success) break;//Only succesful loop exit point
                match += latest;

                var childScope =  this.Parse(scanner, out latest);
                match += latest;
                if (match.Success) scopedStuff.Add(childScope);
                else return null; //Parsing unsuccesful

                scopeEndParser.Parse(scanner, out latest);
                match += latest;

                if (!match.Success) return null; //Parsing unsuccesful
	        }

            return new Scope<T>(scopedStuff, scopeStartPos);
        }
    }
}

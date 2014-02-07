namespace Nintenlord.Event_Assembler.Core.Code.Language.Lexer
{
    public enum TokenType
    {
        EndOfStream,

        LeftParenthesis,
        RightParenthesis,
        LeftCurlyBracket,
        RightCurlyBracket,
        LeftSquareBracket,
        RightSquareBracket,

        IntegerLiteral,
        Symbol,
        MathOperator,

        CodeEnder,
        NewLine,
        Comma,
        Equal,
        Colon
    }
}

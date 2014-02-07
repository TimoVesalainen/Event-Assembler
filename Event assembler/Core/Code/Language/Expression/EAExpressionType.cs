namespace Nintenlord.Event_Assembler.Core.Code.Language.Expression
{
    /// <summary>
    /// Types expressions can have
    /// </summary>
    public enum EAExpressionType
    {
        Scope,
        Code,
        Labeled,
        Assignment,

        XOR,
        AND,
        OR,

        LeftShift,
        RightShift,

        Division,
        Multiply,
        Modulus,

        Minus,
        Sum,

        Value,
        Symbol,
        List,
    }
}

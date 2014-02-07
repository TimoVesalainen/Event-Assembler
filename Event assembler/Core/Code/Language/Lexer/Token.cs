using System;
using Nintenlord.IO;

namespace Nintenlord.Event_Assembler.Core.Code.Language.Lexer
{
    public struct Token : IFilePositionable 
    {
        readonly FilePosition position;
        readonly TokenType type;
        readonly string value;

        public bool HasValue
        {
            get
            {
                return value != null;
            }
        }
        public TokenType Type
        {
            get { return type; }
        }
        public string Value
        {
            get 
            {
                if (value != null)
                {
                    return value;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }            
        }
        public FilePosition Position
        {
            get { return position; }
        }

        public Token(FilePosition position, TokenType type, string value = null)
        {
            this.position = position;
            this.type = type;
            this.value = value;
        }

        public override string ToString()
        {
            return this.type + ( value != null ? "(" + this.value + ")" : "");
        }
    }
}

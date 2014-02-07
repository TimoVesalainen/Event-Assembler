using System;
using System.Collections.Generic;
using Nintenlord.Event_Assembler.Core.IO.Input;
using Nintenlord.IO.Scanners;

namespace Nintenlord.Event_Assembler.Core.Code.Language.Lexer
{
    sealed class TokenScanner : IStoringScanner<Token>
    {
        readonly List<Token> readTokens;
        readonly IPositionableInputStream input;
        int tokenOffset;

        public TokenScanner(IPositionableInputStream input)
        {
            this.input = input;
            readTokens = new List<Token>(0x1000);
            tokenOffset = -1;
            IsAtEnd = false;
        }

        private Token AtOffset(int offset)
        {
            return readTokens[offset];
        }

        #region IScanner<Token> Members

        public bool IsAtEnd
        {
            get;
            private set;
        }

        public long Offset
        {
            get
            {
                return tokenOffset;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public bool MoveNext()
        {
            if (IsAtEnd)
                return false;

            tokenOffset++;
            while (tokenOffset >= readTokens.Count)
            {
                string line = input.ReadLine();
                if (line == null)
                {
                    break;
                }
                readTokens.AddRange(Tokeniser.TokeniseLine(
                    line, input.CurrentFile, input.LineNumber));
            }
            
            IsAtEnd = tokenOffset >= readTokens.Count;

            return !IsAtEnd;
        }

        public Token Current
        {
            get
            {
                if (tokenOffset < readTokens.Count)
                {
                    return readTokens[tokenOffset];
                }
                else
                {
                    throw tokenOffset == readTokens.Count ?
                        new InvalidOperationException("Shouldn't have removed the case.") :
                        new InvalidOperationException("End of tokens to read.");
                }
                //else if (tokenOffset == readTokens.Count)
                //{
                //    return new Token();
                //}
            }
        }
        
        public bool CanSeek
        {
            get { return false; }
        }
        
        #endregion

        #region IStoringScanner<Token> Members

        public Token this[long offset]
        {
            get { return this.AtOffset((int)offset); }
        }
        
        public bool IsStored(long offset)
        {
            return offset >= 0 && offset < readTokens.Count;
        }

        public bool IsStored(long offset, long length)
        {
            return offset >= 0 && offset + length <= readTokens.Count;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Nintenlord.Event_Assembler.Core.IO;
using Nintenlord.Event_Assembler.Core.IO.Input;

namespace Nintenlord.Event_Assembler.Core.Code.Preprocessors
{
    /// <summary>
    /// Preproserror that does nothing.
    /// </summary>
    sealed public class NopPreprocessor : IPreprocessor
    {
        #region IPreprocessor Members

        public void AddDefined(IEnumerable<string> original)
        {
            
        }

        public void AddReserved(IEnumerable<string> reserved)
        {
            
        }

        public string Process(string line, IInputStream inputStream)
        {
            return line;
        }

        public void Dispose()
        {
            
        }

        #endregion
    }
}

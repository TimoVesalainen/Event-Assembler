using System;
using System.Collections.Generic;
using Nintenlord.Event_Assembler.Core.IO.Input;
namespace Nintenlord.Event_Assembler.Core.Code.Preprocessors
{
    /// <summary>
    /// Preprocessor of code
    /// </summary>
    public interface IPreprocessor : IDisposable
    {
        string Process(string line, IInputStream inputStream);

        void AddDefined(IEnumerable<string> original);

        void AddReserved(IEnumerable<string> reserved);
    }
}
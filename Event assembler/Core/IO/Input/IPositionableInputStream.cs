using System;
namespace Nintenlord.Event_Assembler.Core.IO.Input
{
    public interface IPositionableInputStream
    {
        int LineNumber { get; }
        string CurrentFile { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>TODO: Is this actually needed?</remarks>
        /// <returns></returns>
        string PeekOriginalLine();

        string ReadLine();
    }
}

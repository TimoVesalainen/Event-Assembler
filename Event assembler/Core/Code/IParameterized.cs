using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nintenlord.Event_Assembler.Core.Code
{
    /// <summary>
    /// A code that can take a certain amount of parameters. 
    /// </summary>
    public interface IParameterized //Think a better name for this.
    {
        /// <summary>
        /// Minimun amount of parameters accepted or -1 if no minimun exists.
        /// </summary>
        int MinAmountOfParameters { get; }
        /// <summary>
        /// Maximun amount of parameters accepted or -1 if no maximun exists.
        /// </summary>
        int MaxAmountOfParameters { get; }
    }
}

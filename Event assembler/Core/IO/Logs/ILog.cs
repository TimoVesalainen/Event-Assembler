
using System.IO;
namespace Nintenlord.Event_Assembler.Core.IO.Logs
{
    /// <summary>
    /// Logs messages, errors and warnings.
    /// </summary>
    public interface ILog
    {
        int MessageCount { get; }
        int ErrorCount { get; }
        int WarningCount { get; }

        void AddError(string message);
        void AddError(string format, params object[] parameters);
        void AddError(string file, string line, string message);

        void AddWarning(string message);
        void AddWarning(string format, params object[] parameters);
        void AddWarning(string file, string line, string message);

        void AddMessage(string message);
        void AddMessage(string format, params object[] parameters);
        void AddMessage(string file, string line, string message);

        void WriteToStream(TextWriter writer);
    }
}

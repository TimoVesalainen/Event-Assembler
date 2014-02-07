using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nintenlord.Event_Assembler.Core.IO.Logs
{
    public sealed class MessageLog : ILog
    {
        List<string> messages;
        List<string> errors;
        List<string> warnings;

        public MessageLog()
        {
            messages = new List<string>();
            warnings = new List<string>();
            errors = new List<string>();
        }
        public MessageLog(int capacity)
        {
            messages = new List<string>(capacity);
            warnings = new List<string>(capacity);
            errors = new List<string>(capacity);
        }

        private string GetText()
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine("Finished.");

            if (messages.Count > 0)
            {
                message.AppendLine("Messages:");
                foreach (string item in messages)
                {
                    message.AppendLine(item);
                }
                message.AppendLine();
            }
            if (errors.Count > 0)
            {
                message.AppendLine(errors.Count + " errors encountered:");
                foreach (string item in errors)
                {
                    message.AppendLine(item);
                }
                message.AppendLine();
                message.AppendLine("No data written to output.");
                message.AppendLine();
            }
            if (warnings.Count > 0)
            {
                message.AppendLine(warnings.Count + " warnings encountered:");
                foreach (string item in warnings)
                {
                    message.AppendLine(item);
                }
                message.AppendLine();
            }
            if (warnings.Count == 0 && errors.Count == 0)
            {
                message.AppendLine("No errors or warnings.");
                message.AppendLine("Please continue being awesome.");
                message.AppendLine();
            }
            return message.ToString();
        }


        #region IMessageLog Members

        public void AddError(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException();
            }
            errors.Add(message);
        }

        public void AddWarning(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException();
            }
            warnings.Add(message);
        }

        public void AddMessage(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException();
            }
            messages.Add(message);
        }

        public void Clear()
        {
            messages.Clear();
            warnings.Clear();
            errors.Clear();
        }
        
        public void AddError(string file, string line, string message)
        {
            this.AddError(Path.GetFileName(file) + ": " + message + " : " + line);
        }

        public void AddWarning(string file, string line, string message)
        {
            this.AddWarning(Path.GetFileName(file) + ": " + message + " : " + line);
        }

        public void AddMessage(string file, string line, string message)
        {
            this.AddMessage(Path.GetFileName(file) + ": " + message + " : " + line);
        }

        public void AddError(string format, params object[] parameters)
        {
            this.AddError(string.Format(format, parameters));
        }

        public void AddWarning(string format, params object[] parameters)
        {
            this.AddWarning(string.Format(format, parameters));
        }

        public void AddMessage(string format, params object[] parameters)
        {
            this.AddMessage(string.Format(format, parameters));
        }
        
        public int MessageCount
        {
            get { return messages.Count; }
        }

        public int ErrorCount
        {
            get { return errors.Count; }
        }

        public int WarningCount
        {
            get { return warnings.Count; }
        }

        public void WriteToStream(TextWriter writer)
        {
            writer.WriteLine(GetText());
        }

        #endregion
    

}
}

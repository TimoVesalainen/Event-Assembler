using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nintenlord.Event_Assembler.Core.IO;
using Nintenlord.Utility;


namespace Nintenlord.Event_Assembler.Core.Code.Preprocessors.Directives
{
    class Include : IDirective
    {
        #region IDirective Members

        public string Name
        {
            get { return "include"; }
        }

        public bool RequireIncluding
        {
            get { return true; }
        }

        public int MinAmountOfParameters
        {
            get { return 1; }
        }

        public int MaxAmountOfParameters
        {
            get { return 1; }
        }

        public CanCauseError Apply(string[] parameters, IDirectivePreprocessor host)
        {
            string file = Nintenlord.Event_Assembler.Core.IO.IOHelpers.FindFile(host.Input.CurrentFile, parameters[0]);
            if (file.Length > 0)
            {
                host.Input.OpenSourceFile(file);
                return CanCauseError.NoError;
            }
            else
            {
                return CanCauseError.Error("File " + parameters[0] + " not found.");
            }
        }

        #endregion
    }
}

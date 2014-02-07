using System;
using Nintenlord.Utility;
using Nintenlord.Utility.Primitives;

namespace Nintenlord.Event_Assembler.Core.Code
{
    public static class ParameterizedHelpers
    {
        public static bool Matches(this IParameterized parameterized, string name, int paramCount, out string error)
        {
            int min = parameterized.MinAmountOfParameters;
            int max = parameterized.MaxAmountOfParameters;
            if ((min != -1 && max != -1 && paramCount.IsInRange(min, max)) ||
                (min == -1 && paramCount <= max) ||
                (max == -1 && paramCount >= min) ||
                (min == -1 && max == -1)
                )
            {
                error = String.Empty;
                return true;
            }
            else
            {
                string format;
                if (min == -1)
                {
                    format = "maximun of {1}";
                }
                else if (max == -1)
                {
                    format = "minimun of {2}";
                }
                else if (min == max)
                {
                    format = "{1}";
                }
                else
                {
                    format = "range {1}-{2}";
                }
                error =
                    string.Format(
                        "{0} requires " + format + " parameters",
                        name,
                        min,
                        max
                        );
                return false;
            }
        }

        public static CanCauseError Matches(this IParameterized parameterized, string name, int paramCount)
        {
            int min = parameterized.MinAmountOfParameters;
            int max = parameterized.MaxAmountOfParameters;
            if ((min != -1 && max != -1 && paramCount.IsInRange(min, max)) ||
                (min == -1 && paramCount <= max) ||
                (max == -1 && paramCount >= min) ||
                (min == -1 && max == -1)
                )
            {
                return CanCauseError.NoError;
            }
            else
            {
                string format;
                if (min == -1)
                {
                    format = "maximun of {1}";
                }
                else if (max == -1)
                {
                    format = "minimun of {2}";
                }
                else if (min == max)
                {
                    format = "{1}";
                }
                else
                {
                    format = "range {1}-{2}";
                }
                return CanCauseError.Error("{0} requires " + format + " parameters", name, min, max);
            }
        }
    }
}
using System.Collections.Generic;
using System.Reflection;
using Nintenlord.Utility.Primitives;

namespace Nintenlord.Event_Assembler.Core
{
    static class CoreInfo
    {
        //This is useless comment

        public static string[] DefaultLines(string game, string file, int offset, int? size)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetName();
            var version = name.Version;

            var lines = new List<string>(new []{
                    "Disassembled with Nintenlord's Event Assembler",
                    "Version: " + version.ToString(4),
                    "Game: " + game,
                    "File: " + file,
                    "Offset: " + offset.ToHexString("$")
                });
            if (size.HasValue)
            {
                lines.Add("Size: " + size.Value.ToHexString("0x"));
            }
            return lines.ToArray();
        }
    }
}

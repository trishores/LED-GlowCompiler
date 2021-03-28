/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */


using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ledartstudio
{
    internal static class FunctionTable
    {
        internal static List<Function> FuncList;

        static FunctionTable()
        {
            FuncList = new List<Function>();
            var launcherFunc = new Function("@launcher");   // pseudo-function used as an entry point to user code.
            launcherFunc.CodeLines.Add(new CodeLine("callasync: @start"));
            FuncList.Add(launcherFunc);
        }

        internal static bool Build()
        {
            return true;
        }
    }

    internal class Function
    {
        internal string Name;
        internal List<CodeLine> CodeLines = new List<CodeLine>();

        internal Function(string funcName)
        {
            // Use regex to parse function name:
            const string funcPattern = "(@[a-z][a-z_0-9]*)";
            var pattern = $"^{funcPattern}$";
            var match = Regex.Match(funcName, pattern);
            if (!match.Success)
            {
                Console.WriteLine("Invalid function name");
                throw new Exception();
            }
            Name = funcName;
        }
    }
}
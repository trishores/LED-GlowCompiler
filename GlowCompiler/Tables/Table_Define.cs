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
    internal static class DefineTable
    {
        internal static List<CodeLine> CodeLines = new List<CodeLine>();
        internal static List<Define> Defines;

        static DefineTable()
        {
            CodeLines = new List<CodeLine>();
            Defines = new List<Define>();
        }

        internal static bool Build()
        {
            // Add user defines:
            foreach (var codeLine in CodeLines)
            {
                try
                {
                    var newDef = codeLine.LineStr.ParseDefine();
                    Defines.Add(newDef);
                }
                catch
                {
                    Console.Write($"Error parsing line {codeLine.LineNb}");
                    throw new Exception();
                }
            }
            return true;
        }

        internal static string Dealias(string str)
        {
            foreach (var def in Defines)
            {
                string replacePtn = $@"\b{def.Alias}\b";   // capture a single alphanumeric word.
                string replaceWith = def.Value;
                str = Regex.Replace(str, replacePtn, replaceWith);
            }
            return str;
        }

        internal class Define
        {
            internal string Alias;
            internal string Value;
        }
    }

    internal static class LedTableExtensions
    {
        internal static DefineTable.Define ParseDefine(this string str)
        {
            // Use regex to parse define instruction:
            const string gap = @"[\s|\t]*";
            const string keyPtn = "([a-z][a-z_0-9]*)";
            const string valuePtn = "['\"](.*?)['\"]";
            var pattern = $"^define{gap}:{gap}{keyPtn}{gap}={gap}{valuePtn}$";
            var match = Regex.Match(str, pattern);
            if (match.Success)
            {
                var ledDefine = new DefineTable.Define
                {
                    Alias = match.Groups[1].Value,
                    Value = match.Groups[2].Value
                };
                return ledDefine;
            }
            Console.WriteLine("Invalid 'define' instruction");
            throw new Exception();
        }
    }
}

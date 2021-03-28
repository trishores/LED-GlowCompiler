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
    internal class GotoInstruction : Instruction
    {
        private string _targetName;
        internal string TargetName => $"{_targetName}_{FuncGuid}";
        internal Guid FuncGuid;
        internal override BitfieldWrapper BitfieldWrapper { get { return _bitfieldWrapper; } set { _bitfieldWrapper = (GotoBitfieldWrapper)value; } }
        private GotoBitfieldWrapper _bitfieldWrapper;

        private GotoInstruction()
        {
            Type = InstrType.Goto;
        }

        internal GotoInstruction(CodeLine codeLine, int path, int zOrder, Guid funcGuid) : this()
        {
            CodeLine = codeLine;
            Path = path;
            ZOrder = zOrder;
            FuncGuid = funcGuid;
            Parse();
        }

        internal void Parse()
        {
            CodeLine.LineStr = DefineTable.Dealias(CodeLine.LineStr);

            // Perform regex:
            const string instrPattern = "goto";
            const string gap = @"[\s|\t]*";
            const string funcPattern = "([a-z][a-z_0-9]*)";
            var pattern = $"^{instrPattern}{gap}:{gap}{funcPattern}$";
            var match = Regex.Match(CodeLine.LineStr, pattern);
            if (!match.Success)
            {
                Console.WriteLine("Invalid 'goto' instruction");
                throw new Exception();
            }

            // Get target bookmark name:
            _targetName = match.Groups[1].Value;
        }

        internal override void ToBitfieldWrapper()
        {
            _bitfieldWrapper = new GotoBitfieldWrapper();
        }

        internal override string ToString()
        {
            var sl = new List<string>();

            if (_bitfieldWrapper == null)
            {
                sl.Add($"goto: (path={Path}, z-order={ZOrder}, prioritizedThread={PrioritizedThread}, bitaddr={BitAddress}) {TargetName}");
            }
            else
            {
                sl.Add($"goto: (path={Path}, z-order={ZOrder}, prioritizedThread={PrioritizedThread}, bitaddr={BitAddress})");
                sl.Add(_bitfieldWrapper.Print());
            }

            return string.Join(Environment.NewLine, sl);
        }
    }
}

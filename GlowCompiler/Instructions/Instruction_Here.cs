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
    internal class HereInstruction : Instruction
    {
        private string _name;
        internal string Name => $"{_name}_{FuncGuid}";
        internal Guid FuncGuid;
        internal override BitfieldWrapper BitfieldWrapper { get { return _bitfieldWrapper; } set { _bitfieldWrapper = (HereBitfieldWrapper)value; } }
        private HereBitfieldWrapper _bitfieldWrapper;

        private HereInstruction()
        {
            Type = InstrType.Here;
        }

        internal HereInstruction(CodeLine codeLine, int path, int zOrder, Guid funcGuid) : this()
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
            const string instrPattern = "here";
            const string gap = @"[\s|\t]*";
            const string funcPattern = "([a-z][a-z_0-9]*)";
            var pattern = $"^{instrPattern}{gap}:{gap}{funcPattern}$";
            var match = Regex.Match(CodeLine.LineStr, pattern);
            if (!match.Success)
            {
                Console.WriteLine("Invalid 'here' instruction");
                throw new Exception();
            }

            // Get bookmark name:
            _name = match.Groups[1].Value;
        }

        internal override void ToBitfieldWrapper()
        {
            _bitfieldWrapper = new HereBitfieldWrapper();
        }

        internal override string ToString()
        {
            var sl = new List<string>();

            if (_bitfieldWrapper == null)
            {
                sl.Add($"here: (path={Path}, z-order={ZOrder}, prioritizedThread={PrioritizedThread}, bitaddr={BitAddress}) {Name}");
            }
            else
            {
                sl.Add($"here: (path={Path}, z-order={ZOrder}, prioritizedThread={PrioritizedThread}, bitaddr={BitAddress})");
                sl.Add(_bitfieldWrapper.Print());
            }

            return string.Join(Environment.NewLine, sl);
        }
    }
}

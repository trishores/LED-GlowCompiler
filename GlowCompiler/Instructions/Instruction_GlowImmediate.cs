/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ledartstudio
{
    internal class GlowImmediateInstruction : Instruction
    {
        internal List<int> LedIdxList = new List<int>();
        internal LedState LedColor = new LedState();
        internal override BitfieldWrapper BitfieldWrapper { get { return _bitfieldWrapper; } set { _bitfieldWrapper = (GlowImmediateBitfieldWrapper)value; } }
        private GlowImmediateBitfieldWrapper _bitfieldWrapper;

        internal GlowImmediateInstruction()
        {
            Type = InstrType.GlowImmediate;
        }

        internal GlowImmediateInstruction(CodeLine codeLine, int path, int zOrder) : this()
        {
            CodeLine = codeLine;
            Path = path;
            ZOrder = zOrder;
            Parse();
        }

        internal void Parse()
        {
            CodeLine.LineStr = DefineTable.Dealias(CodeLine.LineStr);

            // Perform regex:
            const string instrPattern = "glowimmediate";
            const string gap = @"[\s|\t]*";
            const string ledsPattern = @"\[(.*?)]";
            const string colorPattern = @"\((.*?)\)";
            var pattern = $"^{instrPattern}{gap}:{gap}{ledsPattern}{gap}{colorPattern}$";
            var match = Regex.Match(CodeLine.LineStr, pattern);
            if (!match.Success)
            {
                Console.WriteLine("Invalid 'glowImmediate' instruction");
                throw new Exception();
            }

            // Get zero-based led list:
            try
            {
                LedIdxList = match.Groups[1].Value.GetLeds();
            }
            catch
            {
                Console.WriteLine("Invalid LED(s) in 'glowImmediate' instruction");
                throw new Exception();
            }

            // Get color:
            try
            {
                LedColor = match.Groups[2].Value.GetColor();
            }
            catch
            {
                Console.WriteLine("Invalid color in 'glowImmediate' instruction");
                throw new Exception();
            }
        }

        internal override void ToBitfieldWrapper()
        {
            _bitfieldWrapper = new GlowImmediateBitfieldWrapper();
            _bitfieldWrapper.AddBitfield(_bitfieldWrapper.ColorBitmap);
            _bitfieldWrapper.AddBitfield(_bitfieldWrapper.ActionOpcode);

            // Set action opcode:
            _bitfieldWrapper.ActionOpcode.SetValue((int)ActionOpCode.SetVal);

            // Set color bitmap (using any led in group as they all have same color):
            if (LedColor.Red > -1)
            {
                _bitfieldWrapper.ColorBitmap.SetFlag((int)ColorSpecifier.Red);
                var valueBitfield = new Bitfield(width: 8, name: "red value");
                valueBitfield.SetValue((uint)LedColor.Red);
                _bitfieldWrapper.AddBitfield(valueBitfield);
            }
            if (LedColor.Green > -1)
            {
                _bitfieldWrapper.ColorBitmap.SetFlag((int)ColorSpecifier.Green);
                var valueBitfield = new Bitfield(width: 8, name: "green value");
                valueBitfield.SetValue((uint)LedColor.Green);
                _bitfieldWrapper.AddBitfield(valueBitfield);
            }
            if (LedColor.Blue > -1)
            {
                _bitfieldWrapper.ColorBitmap.SetFlag((int)ColorSpecifier.Blue);
                var valueBitfield = new Bitfield(width: 8, name: "blue value");
                valueBitfield.SetValue((uint)LedColor.Blue);
                _bitfieldWrapper.AddBitfield(valueBitfield);
            }
            if (LedColor.Bright > -1)
            {
                _bitfieldWrapper.ColorBitmap.SetFlag((int)ColorSpecifier.Bright);
                var valueBitfield = new Bitfield(width: 5, name: "bright value");
                valueBitfield.SetValue((uint)LedColor.Bright);
                _bitfieldWrapper.AddBitfield(valueBitfield);
            }

            // Add led bitmap (led index increases from left to right):
            _bitfieldWrapper.LedBitmap = new Bitfield(width: DeviceTable.Dev.LedCount, name: "led bitmap");
            _bitfieldWrapper.AddBitfield(_bitfieldWrapper.LedBitmap);
            LedIdxList.ToList().ForEach(idx => _bitfieldWrapper.LedBitmap.SetFlag((uint)(DeviceTable.Dev.LedCount - idx - 1)));
        }

        internal override string ToString()
        {
            var sl = new List<string>();

            if (_bitfieldWrapper == null)
            {
                sl.Add($"glowImmediate: (path={Path}, z-order={ZOrder}, prioritizedThread={PrioritizedThread}, bitaddr={BitAddress})  [{string.Join(",", LedIdxList)}] ({LedColor.Red},{LedColor.Green},{LedColor.Blue},{LedColor.Bright})");
            }
            else
            {
                sl.Add($"glowImmediate: (path={Path}, z-order={ZOrder}, prioritizedThread={PrioritizedThread}, bitaddr={BitAddress})");
                sl.Add(_bitfieldWrapper.Print());
            }

            return string.Join(Environment.NewLine, sl);
        }

        internal bool Equals(GlowImmediateInstruction mi)
        {
            return LedIdxList.OrderBy(x => x).SequenceEqual(mi.LedIdxList.OrderBy(x => x)) && LedColor.Equals(mi.LedColor);
        }
    }
}
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
    internal class PauseInstruction : Instruction
    {
        internal uint Millisecs;
        internal uint Ticks
        {
            get
            {
                if (Millisecs % DeviceTable.Dev.TickIntervalMs > 0)
                {
                    Console.WriteLine($"Pause duration must be multiple of tick interval at line {CodeLine.LineNb}");
                    throw new Exception();
                }

                var ticks = Millisecs / DeviceTable.Dev.TickIntervalMs;
                return ticks;
            }
        }

        internal override BitfieldWrapper BitfieldWrapper { get { return _bitfieldWrapper; } set { _bitfieldWrapper = (PauseBitfieldWrapper)value; } }
        
        private PauseBitfieldWrapper _bitfieldWrapper;

        internal PauseInstruction()
        {
            Type = InstrType.Pause;
        }

        internal PauseInstruction(CodeLine codeLine, int path, int zOrder) : this()
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
            const string instrPattern = "pause";
            const string gap = @"[\s|\t]*";
            const string timePattern = @"(\d+)";
            const string timeUnitPattern = @"(ms|s|m|h|t)";
            var pattern = $@"^{instrPattern}{gap}:{gap}{timePattern}{timeUnitPattern}$";
            var match = Regex.Match(CodeLine.LineStr, pattern);
            if (!match.Success)
            {
                Console.WriteLine("Invalid 'pause' instruction");
                throw new Exception();
            }

            // Get pause duration in millisecs:
            long millisecs = match.Groups[1].Value.GetDuration(match.Groups[2].Value);
            if (millisecs > 3888000000)    // 45-day upper limit.
            {
                Console.WriteLine("Pause duration exceed 45-days (3888000-seconds)");
                throw new Exception();
            }
            Millisecs = (uint)millisecs;
        }

        internal override void ToBitfieldWrapper()
        {
            _bitfieldWrapper = new PauseBitfieldWrapper();

            // Set byte width opcode:
            _bitfieldWrapper.TickOpcode.SetValue(MiscOps.GetOpcode2Bit(Ticks, out var tickValueBitWidth));
            var valueBitfield = new Bitfield(width: tickValueBitWidth, value: Ticks, name: "tick value");
            _bitfieldWrapper.AddBitfield(valueBitfield);
        }

        internal override string ToString()
        {
            var sl = new List<string>();

            if (_bitfieldWrapper == null)
            {
                sl.Add($"pause: (path={Path}, z-order={ZOrder}, prioritizedThread={PrioritizedThread}, bitaddr={BitAddress}) {Ticks} ticks");
            }
            else
            {
                sl.Add($"pause: (path={Path}, z-order={ZOrder}, prioritizedThread={PrioritizedThread}, bitaddr={BitAddress}) {Ticks} ticks");
                sl.Add(_bitfieldWrapper.Print());
            }

            return string.Join(Environment.NewLine, sl);
        }
    }
}

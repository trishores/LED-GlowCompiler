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
    internal class GlowRampInstruction : Instruction
    {
        internal List<int> LedIdxList = new List<int>();
        internal LedState LedColorFrom = new LedState();
        internal LedState LedColorTo = new LedState();
        internal LedState LedColorDiff = new LedState();
        internal LedState TickStep = new LedState(red: 1, green: 1, blue: 1, bright: 1);
        internal LedState ColorStep = new LedState();
        internal uint RampMs;
        internal uint RampTicks { get { return RampMs / DeviceTable.Dev.TickIntervalMs; } }
        internal GlowImmediateInstruction PreGlowImmediateInstruction;
        internal GlowImmediateInstruction PostGlowImmediateInstruction;
        internal LedstripState LedstripPostState = new LedstripState(DeviceTable.Dev.LedCount);
        internal override BitfieldWrapper BitfieldWrapper { get { return _bitfieldWrapper; } set { _bitfieldWrapper = (GlowRampBitfieldWrapper)value; } }
        private GlowRampBitfieldWrapper _bitfieldWrapper;
        internal static List<int> CrossPathLedList;

        private GlowRampInstruction()
        {
            Type = InstrType.GlowRamp;
        }

        internal GlowRampInstruction(CodeLine codeLine, int path, int zOrder) : this()
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
            const string instrPattern = "glowramp";
            const string gap = @"[\s|\t]*";
            const string ledsPattern = @"\[(.*?)]";
            const string colorPattern = @"\((.*?)\)";
            const string timePattern = @"(\d+)";
            const string timeUnitPattern = @"(ms|s|m|h|t)";
            var pattern = $@"^{instrPattern}{gap}:{gap}{ledsPattern}{gap}{colorPattern}{gap}to{gap}{colorPattern}{gap}in{gap}{timePattern}{timeUnitPattern}$";
            var match = Regex.Match(CodeLine.LineStr, pattern);
            if (!match.Success)
            {
                Console.WriteLine("Invalid 'glowRamp' instruction");
                throw new Exception();
            }

            // Get zero-based led list:
            try
            {
                LedIdxList = match.Groups[1].Value.GetLeds();
            }
            catch
            {
                Console.WriteLine("Invalid LED(s) in 'glowRamp' instruction");
                throw new Exception();
            }

            // Get color:
            try
            {
                // Get start color:
                LedColorFrom = match.Groups[2].Value.GetColor();

                // Get end color:
                LedColorTo = match.Groups[3].Value.GetColor();
            }
            catch
            {
                Console.WriteLine("Invalid color in 'glowRamp' instruction");
                throw new Exception();
            }

            // Get ramp duration in millisecs:
            long millisecs = match.Groups[4].Value.GetDuration(match.Groups[5].Value);
            if (millisecs > 3888000000)    // 45-day upper limit.
            {
                Console.WriteLine("Pause duration exceed 45-days (3888000-seconds)");
                throw new Exception();
            }
            RampMs = (uint)millisecs;

            // Calculate from-to delta (negative values indicate decrement):
            LedColorDiff.Red = LedColorTo.Red - LedColorFrom.Red;
            LedColorDiff.Green = LedColorTo.Green - LedColorFrom.Green;
            LedColorDiff.Blue = LedColorTo.Blue - LedColorFrom.Blue;
            LedColorDiff.Bright = LedColorTo.Bright - LedColorFrom.Bright;

            // Calculate red tick/color step to get as close as possible to target color in ramp ticks:
            if (LedColorDiff.Red != 0 && RampTicks >= Math.Abs(LedColorDiff.Red))
            {
                TickStep.Red = (int)Math.Floor((double)RampTicks / Math.Abs(LedColorDiff.Red));
                ColorStep.Red = 1;
            }
            else if (LedColorDiff.Red != 0 && RampTicks < Math.Abs(LedColorDiff.Red))
            {
                TickStep.Red = 1;
                ColorStep.Red = (int)Math.Floor((double)Math.Abs(LedColorDiff.Red) / RampTicks);
            }

            // Calculate green tick/color step to get as close as possible to target color in ramp ticks:
            if (LedColorDiff.Green != 0 && RampTicks >= Math.Abs(LedColorDiff.Green))
            {
                TickStep.Green = (int)Math.Floor((double)RampTicks / Math.Abs(LedColorDiff.Green));
                ColorStep.Green = 1;
            }
            else if (LedColorDiff.Green != 0 && RampTicks < Math.Abs(LedColorDiff.Green))
            {
                TickStep.Green = 1;
                ColorStep.Green = (int)Math.Floor((double)Math.Abs(LedColorDiff.Green) / RampTicks);
            }

            // Calculate blue tick/color step to get as close as possible to target color in ramp ticks:
            if (LedColorDiff.Blue != 0 && RampTicks >= Math.Abs(LedColorDiff.Blue))
            {
                TickStep.Blue = (int)Math.Floor((double)RampTicks / Math.Abs(LedColorDiff.Blue));
                ColorStep.Blue = 1;
            }
            else if (LedColorDiff.Blue != 0 && RampTicks < Math.Abs(LedColorDiff.Blue))
            {
                TickStep.Blue = 1;
                ColorStep.Blue = (int)Math.Floor((double)Math.Abs(LedColorDiff.Blue) / RampTicks);
            }

            // Calculate bright tick/color step to get as close as possible to target color in ramp ticks:
            if (LedColorDiff.Bright != 0 && RampTicks >= Math.Abs(LedColorDiff.Bright))
            {
                TickStep.Bright = (int)Math.Floor((double)RampTicks / Math.Abs(LedColorDiff.Bright));
                ColorStep.Bright = 1;
            }
            else if (LedColorDiff.Bright != 0 && RampTicks < Math.Abs(LedColorDiff.Bright))
            {
                TickStep.Bright = 1;
                ColorStep.Bright = (int)Math.Floor((double)Math.Abs(LedColorDiff.Bright) / RampTicks);
            }

            // Generate pre glow immediate instruction to initialize glow ramp:
            PreGlowImmediateInstruction = new GlowImmediateInstruction
            {
                LedIdxList = new List<int>(LedIdxList),
                LedColor = LedColorFrom.DeepCopy(),
                Path = Path,
                ZOrder = ZOrder
            };

            // Generate post glow immediate instruction to finalize glow ramp:
            PostGlowImmediateInstruction = new GlowImmediateInstruction
            {
                LedIdxList = new List<int>(LedIdxList),
                LedColor = LedColorTo.DeepCopy(),
                Path = Path,
                ZOrder = ZOrder
            };

            // Generate post-ramp ledstrip state:
            foreach (var ledIdx in LedIdxList)
            {
                LedstripPostState.LedStateList[ledIdx] = LedColorTo;
            }
        }

        internal override void ToBitfieldWrapper()
        {
            var isCrossPath = CrossPathLedList.Any(x => LedIdxList.Contains(x));

            _bitfieldWrapper = new GlowRampBitfieldWrapper();

            // Add ramp tick opcode:
            var opcode2Bit = MiscOps.GetOpcode2Bit(RampTicks, out var valueBitWidth);
            _bitfieldWrapper.RampTickOpcode.SetValue(opcode2Bit);
            _bitfieldWrapper.AddBitfield(_bitfieldWrapper.RampTickOpcode);

            // Add ramp tick value:
            _bitfieldWrapper.RampTickValue = new Bitfield(width: valueBitWidth, value: RampTicks, name: "ramp tick value");
            _bitfieldWrapper.AddBitfield(_bitfieldWrapper.RampTickValue);

            // Add color bitmap (flags set below):
            _bitfieldWrapper.AddBitfield(_bitfieldWrapper.ColorBitmap);

            if (LedColorDiff.Red != 0 || isCrossPath)
            {
                // Add color to bitmap:
                _bitfieldWrapper.ColorBitmap.SetFlag((uint)ColorSpecifier.Red);

                // Add initial color value:
                var bitfield = new Bitfield(width: 8, value: (uint)LedColorFrom.Red, name: "red init value");
                _bitfieldWrapper.AddBitfield(bitfield);

                // Set color inc/dec opcode:
                opcode2Bit = (uint)(LedColorDiff.Red == 0 ? 0 : LedColorDiff.Red > 0 ? 1 : 2);
                bitfield = new Bitfield(width: 2, value: opcode2Bit, name: "inc/dec opcode");
                _bitfieldWrapper.AddBitfield(bitfield);

                if (LedColorDiff.Red != 0)
                {
                    // Set color byte width opcode:
                    opcode2Bit = MiscOps.GetOpcode2Bit((uint)TickStep.Red, out valueBitWidth);
                    bitfield = new Bitfield(width: 2, value: opcode2Bit, name: "byte width opcode");
                    _bitfieldWrapper.AddBitfield(bitfield);

                    // Set tick step value:
                    bitfield = new Bitfield(width: valueBitWidth, value: (uint)TickStep.Red, name: "red tick step value");
                    _bitfieldWrapper.AddBitfield(bitfield);

                    // Set color step value:
                    bitfield = new Bitfield(width: 8, value: (uint)ColorStep.Red, name: "red color step value");
                    _bitfieldWrapper.AddBitfield(bitfield);
                }
            }

            if (LedColorDiff.Green != 0 || isCrossPath)
            {
                // Add color to bitmap:
                _bitfieldWrapper.ColorBitmap.SetFlag((uint)ColorSpecifier.Green);

                // Add initial color value:
                var bitfield = new Bitfield(width: 8, value: (uint)LedColorFrom.Green, name: "green init value");
                _bitfieldWrapper.AddBitfield(bitfield);

                // Set color inc/dec opcode:
                opcode2Bit = (uint)(LedColorDiff.Green == 0 ? 0 : LedColorDiff.Green > 0 ? 1 : 2);
                bitfield = new Bitfield(width: 2, value: opcode2Bit, name: "inc/dec opcode");
                _bitfieldWrapper.AddBitfield(bitfield);

                if (LedColorDiff.Green != 0)
                {
                    // Set color byte width opcode:
                    opcode2Bit = MiscOps.GetOpcode2Bit((uint)TickStep.Green, out valueBitWidth);
                    bitfield = new Bitfield(width: 2, value: opcode2Bit, name: "byte width opcode");
                    _bitfieldWrapper.AddBitfield(bitfield);

                    // Set tick step value:
                    bitfield = new Bitfield(width: valueBitWidth, value: (uint)TickStep.Green, name: "green tick step value");
                    _bitfieldWrapper.AddBitfield(bitfield);

                    // Set color step value:
                    bitfield = new Bitfield(width: 8, value: (uint)ColorStep.Green, name: "green color step value");
                    _bitfieldWrapper.AddBitfield(bitfield);
                }
            }

            if (LedColorDiff.Blue != 0 || isCrossPath)
            {
                // Add color to bitmap:
                _bitfieldWrapper.ColorBitmap.SetFlag((uint)ColorSpecifier.Blue);

                // Add initial color value:
                var bitfield = new Bitfield(width: 8, value: (uint)LedColorFrom.Blue, name: "blue init value");
                _bitfieldWrapper.AddBitfield(bitfield);

                // Set color inc/dec opcode:
                opcode2Bit = (uint)(LedColorDiff.Blue == 0 ? 0 : LedColorDiff.Blue > 0 ? 1 : 2);
                bitfield = new Bitfield(width: 2, value: opcode2Bit, name: "inc/dec opcode");
                _bitfieldWrapper.AddBitfield(bitfield);

                if (LedColorDiff.Blue != 0)
                {
                    // Set color byte width opcode:
                    opcode2Bit = MiscOps.GetOpcode2Bit((uint)TickStep.Blue, out valueBitWidth);
                    bitfield = new Bitfield(width: 2, value: opcode2Bit, name: "byte width opcode");
                    _bitfieldWrapper.AddBitfield(bitfield);

                    // Set tick step value:
                    bitfield = new Bitfield(width: valueBitWidth, value: (uint)TickStep.Blue, name: "blue tick step value");
                    _bitfieldWrapper.AddBitfield(bitfield);

                    // Set color step value:
                    bitfield = new Bitfield(width: 8, value: (uint)ColorStep.Blue, name: "blue color step value");
                    _bitfieldWrapper.AddBitfield(bitfield);
                }
            }

            if (LedColorDiff.Bright != 0 || isCrossPath)
            {
                // Add color to bitmap:
                _bitfieldWrapper.ColorBitmap.SetFlag((uint)ColorSpecifier.Bright);

                // Add initial color value:
                var bitfield = new Bitfield(width: 8, value: (uint)LedColorFrom.Bright, name: "bright init value");
                _bitfieldWrapper.AddBitfield(bitfield);

                // Set color inc/dec opcode:
                opcode2Bit = (uint)(LedColorDiff.Bright == 0 ? 0 : LedColorDiff.Bright > 0 ? 1 : 2);
                bitfield = new Bitfield(width: 2, value: opcode2Bit, name: "inc/dec opcode");
                _bitfieldWrapper.AddBitfield(bitfield);

                if (LedColorDiff.Bright != 0)
                {
                    // Set color byte width opcode:
                    opcode2Bit = MiscOps.GetOpcode2Bit((uint)TickStep.Bright, out valueBitWidth);
                    bitfield = new Bitfield(width: 2, value: opcode2Bit, name: "byte width opcode");
                    _bitfieldWrapper.AddBitfield(bitfield);

                    // Set tick step value:
                    bitfield = new Bitfield(width: valueBitWidth, value: (uint)TickStep.Bright, name: "bright tick step value");
                    _bitfieldWrapper.AddBitfield(bitfield);

                    // Set color step value:
                    bitfield = new Bitfield(width: 8, value: (uint)ColorStep.Bright, name: "bright color step value");
                    _bitfieldWrapper.AddBitfield(bitfield);
                }
            }

            // Add led bitmap (led index increases from left to right):
            _bitfieldWrapper.LedBitmap = new Bitfield(width: DeviceTable.Dev.LedCount, name: "led bitmap");
            LedIdxList.ToList().ForEach(idx => _bitfieldWrapper.LedBitmap.SetFlag((uint)(DeviceTable.Dev.LedCount - idx - 1)));
            _bitfieldWrapper.AddBitfield(_bitfieldWrapper.LedBitmap);
        }

        internal override string ToString()
        {
            var sl = new List<string>();

            if (_bitfieldWrapper == null)
            {
                sl.Add($"glowRamp: (path={Path}, z-order={ZOrder}, prioritizedThread={PrioritizedThread}, bitaddr={BitAddress})  " +
                    $"[{string.Join(",", LedIdxList)}] " +
                    $"({LedColorFrom.Red},{LedColorFrom.Green},{LedColorFrom.Blue},{LedColorFrom.Bright}) " +
                    $"({LedColorTo.Red},{LedColorTo.Green},{LedColorTo.Blue},{LedColorTo.Bright}) " +
                    $"{RampMs}");
            }
            else
            {
                sl.Add($"glowRamp: (path={Path}, z-order={ZOrder}, prioritizedThread={PrioritizedThread}, bitaddr={BitAddress})");
                sl.Add(_bitfieldWrapper.Print());
            }

            return string.Join(Environment.NewLine, sl);
        }
    }
}
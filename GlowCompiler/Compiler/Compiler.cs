/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ledartstudio
{
    internal class Compiler
    {
        private readonly ArgHandler _args;
        private const int BitByteShift = 3;
        private const int BitsPerByte = 8;

        internal Compiler(ArgHandler args)
        {
            _args = args;
        }

        internal bool Compile(out byte[] lightshowByteArray, out uint contextRegionByteSize, out uint maxInstrPathByteSize, out int threadCount)
        {
            if (DeviceTable.Dev.ProtocolVersion != Program.ProtocolVersion)
            {
                Console.WriteLine($"Device protocol version {DeviceTable.Dev.ProtocolVersion} does not match compiler protocol version {Program.ProtocolVersion}.");
                throw new Exception();
            }

            lightshowByteArray = null;

            PrintText(string.Empty);

            // Convert glowscript into a flat list of instructions (recursively processes start fn code):
            var rawInstrSet = new List<Instruction>();
            rawInstrSet.AddRange(new CallInstruction(new CodeLine("callasync: @launcher"), path: -1, zOrder: 0).InstrList);
            PrintInstructionSet(rawInstrSet);

            // Segregate instructions into prioritized threads by path/z-order:
            var instrSets = SeparatePathInstructions(rawInstrSet);
            PrintInstructionSets(instrSets);

            // Count threads:
            threadCount = instrSets.Count();
            if (threadCount > DeviceTable.Proto.MaxThreads)
            {
                Console.WriteLine($"Number of threads ({threadCount}) exceeds maximum ({DeviceTable.Proto.MaxThreads})");
                throw new Exception();
            }

            // Prune unused instruction set paths:
            //PruneUnusedInstructions(ref instrSet);     // TODO: commented until modified to process instructions same as mcu.
            //PrintInstructions(instrSet);

            // Build list of leds accessed by multiple paths:
            GenerateCrossPathLedList(ref instrSets);

            // Calculate max brightness coefficient for simulator (run prior to changesets which use -1 values):
            CalcSimulatorBrightnessCoeff(instrSets.SelectMany(instr => instr).ToList());

            // Generate an initial wrapped set of bitfields for each instruction:
            GenerateBitfieldWrapperForEachInstruction(ref instrSets);
            PrintInstructionSets(instrSets);

            // Add dynamic bitfields:
            AddDynamicBitfields(ref instrSets);
            PrintInstructionSets(instrSets);

            // Add dynamic data to existing bitfields:
            AddDynamicData(ref instrSets);
            PrintInstructionSets(instrSets);

            // Generate context-region bitfields:
            var contextRegion = BuildContextRegionBitfields(instrSets, out contextRegionByteSize, out maxInstrPathByteSize);
            PrintText(contextRegion.ToString());
            PrintInstructionSets(instrSets, append: true);

            // Concatenate context-region bitfields into a single aggregated bitfield:
            var contextRegionBitfield = ConcatenateContextRegionBitfields(contextRegion);
            PrintText("Context region bitfield:", append: true);
            PrintText(contextRegionBitfield.ToString(), append: true);

            // Concatenate all instruction bitfields into a single aggregated bitfield:
            var instructionRegionBitfield = ConcatenateAllInstructionsBitfields(ref instrSets);
            PrintText("Instruction region bitfield:", append: true);
            PrintText(instructionRegionBitfield.ToString(), append: true);

            // Concatenate context region bitfield and instructions bitfield:
            var lightshowBitfield = contextRegionBitfield.Concat(instructionRegionBitfield);
            PrintText("Lightshow bitfield (context region bitfield + instruction region bitfield):", append: true);
            PrintText(lightshowBitfield.ToString(), append: true);

            // Pad single aggregated bitfield to packet-size multiple:
            lightshowBitfield = PadBitfieldToPacketSizeMultiple(bitfield: lightshowBitfield);
            PrintText("Lightshow bitfield (padded):", append: true);
            PrintText(lightshowBitfield.ToString(), append: true);

            // Convert the single aggregated bitfield to a byte array:
            lightshowByteArray = lightshowBitfield.ToByteList().ToArray();

            return true;
        }

        #region Separate instructions into independent path instruction sets

        private List<List<Instruction>> SeparatePathInstructions(List<Instruction> instrSet)
        {
            // Order path instruction sets by z-order (highest z-order number executed last i.e. top layer animation):
            var instrSets = instrSet.Where(x => x.Path > -1).GroupBy(x => x.Path).Select(x => x.ToList())
                .OrderBy(x => x.First().ZOrder)
                .ThenBy(x => x.First().Path)
                .ToList();

            // Assign prioritized path numbers:
            var idx = 0;
            instrSets.ForEach(x =>
            {
                x.ForEach(y => y.PrioritizedThread = idx);
                idx++;
            });

            // Convert activate instruction target to prioritized thread number instead of path number:
            for (var i = 0; i < instrSets.Count; i++)
            {
                // Process each path activate instruction:
                foreach (var instr in instrSets[i])
                {
                    if (instr.Type == Instruction.InstrType.PathActivate)
                    {
                        var path = ((PathActivateInstruction)instr).TargetPathIdx;
                        var prioritizedThread = instrSets.SelectMany(x => x).First(y => y.Path == path).PrioritizedThread;
                        ((PathActivateInstruction)instr).TargetPathIdx = (uint)prioritizedThread;
                    }
                }
            }

            return instrSets;
        }

        #endregion

        #region Prune unused instructions

        /*private void PruneUnusedInstructions(ref List<List<Instruction>> instrSets)
        {
            var tempInstrSet = new List<Instruction>();
            var pathMax = instrSet.DistinctBy(x => x.Path).Count();

            for (var path = 0; path <= pathMax; path++)
            {
                var tempPath = path;
                var pathInstrList = new List<Instruction>(instrSet.Where(x => x.Path == tempPath));
                var i = 0;

                while (true)
                {
                    if (i > pathInstrList.Count - 1) break;
                    var currInstr = pathInstrList[i];
                    if (currInstr.Visited) break;
                    currInstr.Visited = true;

                    if (currInstr.Type == Instruction.InstrType.Goto)
                    {
                        var targetName = ((GotoInstruction)currInstr).TargetName;
                        var targetInstr = pathInstrList.First(x => x.Type == Instruction.InstrType.Here && targetName.Equals(((HereInstruction)x).Name));
                        i = instrSet.IndexOf(targetInstr);
                    }
                    else i++;
                }

                tempInstrSet.AddRange(pathInstrList.Where(instr => instr.Visited || instr.Type == Instruction.InstrType.PathEnd));
            }

            instrSet = tempInstrSet;
        }//*/

        #endregion

        #region Compile list of leds that are referenced in multiple paths

        private void GenerateCrossPathLedList(ref List<List<Instruction>> instrSets)
        {
            var crossPathLedList = new List<int>();
            var allPathsInstrs = instrSets.SelectMany(instr => instr);
            for (var ledIdx = 0; ledIdx < DeviceTable.Dev.LedCount; ledIdx++)
            {
                var ledGlowInstructions = allPathsInstrs.Where(instr =>
                    (instr.Type == Instruction.InstrType.GlowImmediate && ((GlowImmediateInstruction)instr).LedIdxList.Contains(ledIdx)) ||
                    (instr.Type == Instruction.InstrType.GlowRamp && ((GlowRampInstruction)instr).LedIdxList.Contains(ledIdx)));
                if (ledGlowInstructions.DistinctBy(instr => instr.Path).Count() > 1)
                {
                    crossPathLedList.Add(ledIdx);
                }
            }
            GlowRampInstruction.CrossPathLedList = crossPathLedList;
        }

        #endregion

        #region Calculate simulator brightness coefficient

        internal void CalcSimulatorBrightnessCoeff(List<Instruction> allPathsInstructions)
        {
            var colorStateList = new List<LedState>();

            foreach (var instr in allPathsInstructions)
            {
                if (instr.Type == Instruction.InstrType.GlowImmediate)
                {
                    colorStateList.Add(((GlowImmediateInstruction)instr).LedColor);
                }
                else if (instr.Type == Instruction.InstrType.GlowRamp)
                {
                    colorStateList.Add(((GlowRampInstruction)instr).LedColorFrom);
                    colorStateList.Add(((GlowRampInstruction)instr).LedColorTo);
                }
            }

            // Calculate simulator brightness coeff (this lightshow's max brightness):
            foreach (var colorState in colorStateList)
            {
                uint simBrightnessCoeff = (uint)((colorState.Red + colorState.Green + colorState.Blue) * colorState.Bright);
                if (DeviceTable.Dev.SimulatorBrightnessCoeff < simBrightnessCoeff) DeviceTable.Dev.SimulatorBrightnessCoeff = simBrightnessCoeff;
            }
        }

        #endregion

        #region Generate a set of bitfields for each instruction

        private void GenerateBitfieldWrapperForEachInstruction(ref List<List<Instruction>> instrSets)
        {
            for (var i = 0; i < instrSets.Count; i++)
            {
                instrSets[i].ForEach(instr => instr.ToBitfieldWrapper());
            }
        }

        #endregion

        #region Add dynamic instruction bitfields

        private void AddDynamicBitfields(ref List<List<Instruction>> instrSets)
        {
            // If necessary, add padding to last instruction of each instruction-set to byte-align next instruction set:
            // This makes path length an even number of bytes.
            for (var i = 0; i < instrSets.Count; i++)
            {
                // Add padding to byte-align next instruction set:
                var paddingWidth = BitsPerByte - (instrSets[i].Sum(instr => instr.BitWidth) % BitsPerByte);
                if (paddingWidth < 8)
                {
                    instrSets[i].Last().BitfieldWrapper.AddBitfield(new Bitfield((uint)paddingWidth, "byte-align padding"));
                }
            }
        }

        #endregion

        #region Add dynamic instruction data

        // Populate bitfield values (bitfield widths unchanged).
        private void AddDynamicData(ref List<List<Instruction>> instrSets)
        {
            var tempAggBitfield1 = new Bitfield(width: 0);

            // Populate each instructions's bit address relative to start of first path:
            for (var i = 0; i < instrSets.Count; i++)
            {
                var tempAggBitfield2 = new Bitfield(width: 0);

                foreach (var instr in instrSets[i])
                {
                    // Save instruction bit address:
                    instr.BitAddress = tempAggBitfield1.BitWidth;
                    instr.CurrentPathBitAddress = tempAggBitfield2.BitWidth;

                    // Append instruction bitfield to aggregated bitfield:
                    tempAggBitfield1 = tempAggBitfield1.Concat(instr.BitfieldWrapper.ToAggregatedBitfield());    // aggBitfield value is incomplete/unused.
                    tempAggBitfield2 = tempAggBitfield2.Concat(instr.BitfieldWrapper.ToAggregatedBitfield());    // aggBitfield value is incomplete/unused.
                }
            }

            // Populate each goto instruction's target bit address relative to start of current path:
            for (var i = 0; i < instrSets.Count; i++)
            {
                foreach (var instr in instrSets[i])
                {
                    if (instr.Type == Instruction.InstrType.Goto)
                    {
                        var gotoTargetName = ((GotoInstruction)instr).TargetName;
                        var matchingHere = instrSets[i].First(x => x.Type == Instruction.InstrType.Here && ((HereInstruction)x).Name == gotoTargetName);
                        // Set 'here' bit address relative to start of current path:
                        ((GotoBitfieldWrapper)((GotoInstruction)instr).BitfieldWrapper).TargetBitAddress.SetValue(matchingHere.CurrentPathBitAddress);
                    }
                }
            }
        }

        #endregion

        #region Build context-region bitfields

        private Instruction_ContextRegion BuildContextRegionBitfields(List<List<Instruction>> instrSets, out uint contextRegionByteSize, out uint maxInstrPathByteSize)
        {
            var contextRegion = new Instruction_ContextRegion();
            contextRegion.ToBitfieldWrapper();

            // Build common context-region:
            var contextRegionByteLenBitfield = new Bitfield(width: 16, name: "contx-reg byte len");
            var instrRegionByteLenBitfield = new Bitfield(width: 32, name: "instr-reg byte len");
            var ledCountBitfield = new Bitfield(width: 16, value: DeviceTable.Dev.LedCount, name: "total led len");
            var timerIntervalMs = new Bitfield(width: 16, value: DeviceTable.Dev.TickIntervalMs, name: "tick interval ms");
            var simBrightCoeffBitfield = new Bitfield(width: 16, value: DeviceTable.Dev.SimulatorBrightnessCoeff, name: "sim bright coeff");
            var totalPathsBitfield = new Bitfield(width: 8, value: (uint)instrSets.Count, name: "total paths");
            var pathEndedBitmap = new Bitfield(width: (uint)instrSets.Count, name: "path end bitmap");
            contextRegion.BitfieldWrapper.AddBitfield(contextRegionByteLenBitfield);
            contextRegion.BitfieldWrapper.AddBitfield(instrRegionByteLenBitfield);
            contextRegion.BitfieldWrapper.AddBitfield(ledCountBitfield);
            contextRegion.BitfieldWrapper.AddBitfield(timerIntervalMs);
            contextRegion.BitfieldWrapper.AddBitfield(simBrightCoeffBitfield);
            contextRegion.BitfieldWrapper.AddBitfield(totalPathsBitfield);
            contextRegion.BitfieldWrapper.AddBitfield(pathEndedBitmap);

            uint totalInstrRegionByteLen = 0;
            maxInstrPathByteSize = 0;

            // Build a context-region block for each path:
            for (var i = 0; i < instrSets.Count; i++)
            {
                // Mark current path as ended (except path 0):
                if (i > 0) pathEndedBitmap.SetFlag((uint)(instrSets.Count - i - 1));

                // Add start of current path byte-address bitfield:
                if ((instrSets[i].First().BitAddress - instrSets[0].First().BitAddress) % BitsPerByte > 0)
                {
                    Console.WriteLine("Byte alignment error");
                    throw new Exception();
                }
                var pathStartByteAddr = (instrSets[i].First().BitAddress - instrSets[0].First().BitAddress) >> BitByteShift;
                var opcode2Bit = MiscOps.GetOpcode2Bit(pathStartByteAddr, out var valueBitWidth);
                contextRegion.BitfieldWrapper.AddBitfield(new Bitfield(width: 2, value: opcode2Bit, name: "byte width opcode"));
                contextRegion.BitfieldWrapper.AddBitfield(new Bitfield(width: valueBitWidth, value: pathStartByteAddr, name: $"path={i} byte addr"));

                // Add current path byte-length bitfield:
                var instrPathByteLen = (instrSets[i].Last().BitAddress + instrSets[i].Last().BitfieldWrapper.BitWidth - instrSets[i].First().BitAddress) >> BitByteShift;
                opcode2Bit = MiscOps.GetOpcode2Bit(instrPathByteLen, out valueBitWidth);
                contextRegion.BitfieldWrapper.AddBitfield(new Bitfield(width: 2, value: opcode2Bit, name: "byte width opcode"));
                contextRegion.BitfieldWrapper.AddBitfield(new Bitfield(width: valueBitWidth, value: instrPathByteLen, name: $"path={i} byte len"));

                // Keep running total of paths length:
                totalInstrRegionByteLen += instrPathByteLen;
                maxInstrPathByteSize = Math.Max(maxInstrPathByteSize, instrPathByteLen);

                // Add current path instruction bit address bitfield:
                var instrBitAddr = instrPathByteLen << BitByteShift;    // max possible value is number of bits in path.
                opcode2Bit = MiscOps.GetOpcode2Bit(instrBitAddr, out valueBitWidth);
                contextRegion.BitfieldWrapper.AddBitfield(new Bitfield(width: 2, value: opcode2Bit, name: "byte width opcode"));
                contextRegion.BitfieldWrapper.AddBitfield(new Bitfield(width: valueBitWidth, value: 0, name: $"path={i} instr bit addr"));

                // Add current path extra-value bitfield:
                uint maxValue = 0;
                var rampInstrs = instrSets[i].Where(x => x.Type == Instruction.InstrType.GlowRamp);
                if (rampInstrs.Count() > 0) maxValue = rampInstrs.Max(x => ((GlowRampInstruction)x).RampTicks);
                var opcode3Bit = MiscOps.GetOpcode3Bit(maxValue, out valueBitWidth);
                contextRegion.BitfieldWrapper.AddBitfield(new Bitfield(width: 3, value: opcode3Bit, name: "byte width opcode"));
                if (valueBitWidth > 0) contextRegion.BitfieldWrapper.AddBitfield(new Bitfield(width: valueBitWidth, value: 0, name: $"path={i} extra value"));

                // Add current path pause-ticks bitfield:
                maxValue = 0;
                var pauseInstrs = instrSets[i].Where(x => x.Type == Instruction.InstrType.Pause);
                if (pauseInstrs.Count() > 0) maxValue = pauseInstrs.Max(x => ((PauseInstruction)x).Ticks);
                else if (rampInstrs.Count() > 0) maxValue = 1;
                opcode3Bit = MiscOps.GetOpcode3Bit(maxValue, out valueBitWidth);
                contextRegion.BitfieldWrapper.AddBitfield(new Bitfield(width: 3, value: opcode3Bit, name: "byte width opcode"));
                if (valueBitWidth > 0) contextRegion.BitfieldWrapper.AddBitfield(new Bitfield(width: valueBitWidth, value: 0, name: $"path={i} pause ticks"));
            }

            // Set total instruction region byte length:
            instrRegionByteLenBitfield.SetValue(totalInstrRegionByteLen);

            // Add padding to context-region so following instruction path memory is byte-aligned:
            var paddingWidth = BitsPerByte - (contextRegion.BitfieldWrapper.BitWidth % BitsPerByte);
            if (paddingWidth < 8)
            {
                contextRegion.BitfieldWrapper.AddBitfield(new Bitfield(paddingWidth, "byte-align padding"));
            }

            // Set total context-region byte length (convert from bits):
            contextRegionByteSize = contextRegion.BitfieldWrapper.BitWidth >> BitByteShift;
            contextRegionByteLenBitfield.SetValue(contextRegionByteSize);

            return contextRegion;
        }

        #endregion

        #region Concatenate all instruction bitfields into a single aggregated bitfield

        private Bitfield ConcatenateAllInstructionsBitfields(ref List<List<Instruction>> instrSets)
        {
            var aggBitfield = new Bitfield(width: 0);

            for (var i = 0; i < instrSets.Count; i++)
            {
                instrSets[i].ForEach(instr => aggBitfield = aggBitfield.Concat(instr.BitfieldWrapper.ToAggregatedBitfield()));
            }

            return aggBitfield;
        }

        #endregion

        #region Concatenate all context-region bitfields into a single aggregated bitfield

        private Bitfield ConcatenateContextRegionBitfields(Instruction_ContextRegion contextRegion)
        {
            var aggBitfield = contextRegion.BitfieldWrapper.ToAggregatedBitfield();

            return aggBitfield;
        }

        #endregion

        #region Pad bitfield to packet-size multiple

        private Bitfield PadBitfieldToPacketSizeMultiple(Bitfield bitfield)
        {
            var packetBitLen = DeviceTable.Dev.UsbPacketByteLen * BitsPerByte;
            if (bitfield.BitWidth % packetBitLen == 0) return bitfield;

            var packetSizeMultiple = bitfield.BitWidth;
            while (packetSizeMultiple % packetBitLen != 0) { packetSizeMultiple++; }
            var paddingBitfield = new Bitfield(width: packetSizeMultiple - bitfield.BitWidth);
            paddingBitfield.BitArray.SetAll(true);  // fill with 1's.
            bitfield = bitfield.Concat(paddingBitfield);

            return bitfield;
        }

        #endregion

        #region Print methods

        [Conditional("DEBUG")]  // Method output is discarded for Release builds.
        private void PrintInstructionSets(List<List<Instruction>> instrSets, bool append = false)
        {
            var outputDebugFilePath = Path.Combine(Path.GetDirectoryName(_args.InputFilePath), Path.GetFileNameWithoutExtension(_args.InputFilePath) + ".dbg");

            var strList = new List<string>();
            for (var i = 0; i < instrSets.Count; i++)
            {
                strList.AddRange(instrSets[i].Select(instr => instr.ToString()));
                strList.Add(Environment.NewLine + "-----------------" + Environment.NewLine);
            }

            if (append) File.AppendAllLines(outputDebugFilePath, strList);
            else File.WriteAllLines(outputDebugFilePath, strList);
        }

        [Conditional("DEBUG")]  // Method output is discarded for Release builds.
        private void PrintInstructionSet(List<Instruction> instrSet, bool append = false)
        {
            var outputDebugFilePath = Path.Combine(Path.GetDirectoryName(_args.InputFilePath), Path.GetFileNameWithoutExtension(_args.InputFilePath) + ".dbg");

            var strList = new List<string>();
            instrSet.ForEach(instr => strList.Add(instr.ToString()));

            if (append) File.AppendAllLines(outputDebugFilePath, strList);
            else File.WriteAllLines(outputDebugFilePath, strList);
        }
        
        [Conditional("DEBUG")]  // Method output is discarded for Release builds.
        private void PrintText(string text, bool append = false)
        {
            var outputDebugFilePath = Path.Combine(Path.GetDirectoryName(_args.InputFilePath), Path.GetFileNameWithoutExtension(_args.InputFilePath) + ".dbg");

            if (append) File.AppendAllText(outputDebugFilePath, Environment.NewLine + text);
            else File.WriteAllText(outputDebugFilePath, text);
        }

        #endregion
    }
}
 
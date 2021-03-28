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
    internal class CallInstruction : Instruction
    {
        internal string FuncName;
        internal int CallCount = 1;
        internal static int ExecutionPathIndex = -1;
        internal List<Instruction> InstrList = new List<Instruction>();
        static bool recursiveTip = true;

        private CallInstruction()
        {
            Type = InstrType.Call;
        }

        internal CallInstruction(CodeLine codeLine, int path, int zOrder) : this()
        {
            CodeLine = codeLine;
            Path = path;
            ZOrder = zOrder;
            Parse();
        }

        internal void Parse()
        {
            // Do not use dealias, as aliasing is not permitted for call instructions.

            // Get target function name:
            const string instrPattern = "(?:call|callasync)";
            const string gapPattern = @"[\s|\t]*";
            const string funcPattern = "(@[a-z][a-z_0-9]*)";
            var pattern = $"^{instrPattern}{gapPattern}:{gapPattern}{funcPattern}"; // ignore z-order or repeat parameters.
            var match = Regex.Match(CodeLine.LineStr, pattern);
            if (!match.Success)
            {
                Console.WriteLine("Invalid 'call/callAsync' instruction");
                throw new Exception();
            }
            FuncName = match.Groups[1].Value;

            // Find target function in table:
            var targetFunc = FunctionTable.FuncList.FirstOrDefault(func => func.Name.Equals(FuncName));
            if (targetFunc == null)
            {
                Console.WriteLine($"Function not found: {FuncName}");
                throw new Exception();
            }

            // Recursively process target function code lines:
            var instrSet = ConvertToInstructions(targetFunc.CodeLines, Path, ZOrder);
            InstrList.AddRange(instrSet);
        }

        internal static List<Instruction> ConvertToInstructions(List<CodeLine> codeLines, int path, int zOrder)
        {
            var instrSet = new List<Instruction>();
            var funcGuid = Guid.NewGuid();
            var currLineNb = 1;

            try
            {
                foreach (var codeLine in codeLines)
                {
                    currLineNb = codeLine.LineNb;

                    if (codeLine.LineStr.IsGlowImmediateInstruction())
                    {
                        var instr = new GlowImmediateInstruction(codeLine, path, zOrder);
                        instrSet.Add(instr);
                    }
                    else if (codeLine.LineStr.IsGlowRampInstruction())
                    {
                        var instr = new GlowRampInstruction(codeLine, path, zOrder);
                        instrSet.Add(instr.PreGlowImmediateInstruction);
                        instrSet.Add(instr);
                        instrSet.Add(instr.PostGlowImmediateInstruction);
                    }
                    else if (codeLine.LineStr.IsCallInstruction(out var repeatCount))
                    {
                        while (repeatCount-- > 0)
                        {
                            var instr = new CallInstruction(codeLine, path, zOrder);
                            instrSet.AddRange(instr.InstrList);
                        }
                    }
                    else if (codeLine.LineStr.IsCallAsyncInstruction(zOrder, out int newZOrder))
                    {
                        var nextExecutionPathIndex = ++ExecutionPathIndex;
                        var pathActivateInstruction = new PathActivateInstruction(path, zOrder)   // use prev zOrder as this instruction is not in new path.
                        {
                            TargetPathIdx = (uint)nextExecutionPathIndex
                        };
                        var callInstruction = new CallInstruction(codeLine, nextExecutionPathIndex, newZOrder);
                        var pathEndInstruction = new PathEndInstruction(nextExecutionPathIndex, newZOrder);
                        instrSet.Add(pathActivateInstruction);
                        instrSet.AddRange(callInstruction.InstrList);
                        instrSet.Add(pathEndInstruction);
                    }
                    else if (codeLine.LineStr.IsPauseInstruction())
                    {
                        var instr = new PauseInstruction(codeLine, path, zOrder);
                        instrSet.Add(instr);
                    }
                    else if (codeLine.LineStr.IsHereInstruction())
                    {
                        var instr = new HereInstruction(codeLine, path, zOrder, funcGuid);
                        instrSet.Add(instr);
                    }
                    else if (codeLine.LineStr.IsGotoInstruction())
                    {
                        var instr = new GotoInstruction(codeLine, path, zOrder, funcGuid);
                        instrSet.Add(instr);
                    }
                    else
                    {
                        Console.WriteLine($"Unknown instruction");
                        throw new Exception();
                    }
                }
            }
            catch
            {
                if (recursiveTip)
                {
                    recursiveTip = false;
                    Console.Write($"Error parsing line {currLineNb}");
                }
                throw new Exception();
            }

            return instrSet;
        }
    }
}
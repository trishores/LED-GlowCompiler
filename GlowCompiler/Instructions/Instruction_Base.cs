/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */


using System;

namespace ledartstudio
{
    internal abstract class Instruction
    {
        internal enum InstrType
        {
            Control = 0,
            Here = 1,
            Goto = 2,
            Pause = 3,
            GlowImmediate = 4,
            GlowRamp = 5,
            ContextRegion = 13,
            PathActivate = 14,
            PathEnd = 15,
            // Value 100+ are irrelevant to device:
            Call = 100,
        }
        internal InstrType Type;
        internal CodeLine CodeLine;
        internal bool Visited = false;
        internal int Path = -1;
        internal int ZOrder = -1;
        internal int PrioritizedThread = -1;
        internal uint BitAddress;   // bit address relative to start of path 0.
        internal uint CurrentPathBitAddress;   // bit address relative to start of instruction path.
        internal uint BitWidth { get { return BitfieldWrapper.BitWidth; } }
        internal virtual BitfieldWrapper BitfieldWrapper { get { throw new InvalidOperationException(); } set { throw new InvalidOperationException(); } }

        internal new virtual string ToString() { throw new InvalidOperationException(); }
        internal virtual void ToBitfieldWrapper() { throw new InvalidOperationException(); }
    }
}

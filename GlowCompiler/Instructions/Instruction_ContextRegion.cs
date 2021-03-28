/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */


using System;
using System.Collections.Generic;

namespace ledartstudio
{
    internal class Instruction_ContextRegion : Instruction
    {
        internal override BitfieldWrapper BitfieldWrapper { get { return _bitfieldWrapper; } set { _bitfieldWrapper = (ContextRegionBitfieldWrapper)value; } }
        private ContextRegionBitfieldWrapper _bitfieldWrapper;

        internal Instruction_ContextRegion()
        {
            Type = InstrType.ContextRegion;
        }

        internal override void ToBitfieldWrapper()
        {
            _bitfieldWrapper = new ContextRegionBitfieldWrapper();
        }

        internal new string ToString()
        {
            var sl = new List<string>();

            sl.Add($"context-region:");
            sl.Add(_bitfieldWrapper.Print());
            sl.Add(Environment.NewLine + "-----------------" + Environment.NewLine + Environment.NewLine);

            return string.Join(Environment.NewLine, sl);
        }
    }
}

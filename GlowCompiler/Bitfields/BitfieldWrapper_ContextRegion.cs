/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */

namespace ledartstudio
{
    internal class ContextRegionBitfieldWrapper : BitfieldWrapper
    {
        internal Bitfield Type = new Bitfield(width: 4, value: (int)Instruction.InstrType.ContextRegion, name: "type");

        internal ContextRegionBitfieldWrapper()
        {
            AddBitfield(Type);
        }
    }
}

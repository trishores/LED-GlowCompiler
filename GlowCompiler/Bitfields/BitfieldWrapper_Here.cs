/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */

namespace ledartstudio
{
    internal class HereBitfieldWrapper : BitfieldWrapper
    {
        internal Bitfield Type = new Bitfield(width: 4, name: "type");

        internal HereBitfieldWrapper()
        {
            AddBitfield(Type);

            Type.SetValue((int)Instruction.InstrType.Here);
        }
    }
}

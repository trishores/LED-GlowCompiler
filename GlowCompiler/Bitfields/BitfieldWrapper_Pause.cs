/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */

namespace ledartstudio
{
    internal class PauseBitfieldWrapper : BitfieldWrapper
    {
        internal Bitfield Type = new Bitfield(width: 4, name: "type");
        internal Bitfield TickOpcode = new Bitfield(width: 2, name: "byte width opcode");

        internal PauseBitfieldWrapper()
        {
            AddBitfield(Type);
            AddBitfield(TickOpcode);

            Type.SetValue((int)Instruction.InstrType.Pause);
        }
    }
}
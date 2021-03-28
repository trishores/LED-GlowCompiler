/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */

namespace ledartstudio
{
    internal enum MemoryOpcode
    {
        Sram = 0,
        Nvm = 1
    }
    internal enum ControlOpcode
    {
        PauseLightshow = 0,
        ResumeLightshow = 1,
        RestartLightshow = 2,
        StoreNewPackets = 3
    }

    internal class ControlBitfieldWrapper : BitfieldWrapper
    {
        internal Bitfield Type = new Bitfield(width: 4, name: "type");
        internal Bitfield MemoryOpcode = new Bitfield(width: 1, name: "memory opcode");
        internal Bitfield ControlOpcode = new Bitfield(width: 3, name: "control opcode");

        internal ControlBitfieldWrapper()
        {
            AddBitfield(Type);
            AddBitfield(MemoryOpcode);
            AddBitfield(ControlOpcode);

            Type.SetValue((int)Instruction.InstrType.Control);
        }
    }
}

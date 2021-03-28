/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */

namespace ledartstudio
{
    internal class GlowRampBitfieldWrapper : BitfieldWrapper
    {
        internal Bitfield Type = new Bitfield(width: 4, name: "type");
        internal Bitfield LedBitmap;
        internal Bitfield RampTickOpcode = new Bitfield(width: 2, name: "byte width opcode");
        internal Bitfield RampTickValue;
        internal Bitfield ColorBitmap = new Bitfield(width: 4, name: "color bitmap");

        internal GlowRampBitfieldWrapper()
        {
            AddBitfield(Type);

            Type.SetValue((int)Instruction.InstrType.GlowRamp);
        }
    }
}

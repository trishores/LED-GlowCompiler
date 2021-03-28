/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */

namespace ledartstudio
{
    internal enum ColorSpecifier { Red = 3, Green = 2, Blue = 1, Bright = 0 }

    internal enum ActionOpCode {
        SetVal = 0,
        SetAllZeroThenVal = 1,
        SetVal0 = 2,
        SetValRandom = 3
    }

    internal class GlowImmediateBitfieldWrapper : BitfieldWrapper
    {
        internal Bitfield Type = new Bitfield(width: 4, name: "type");
        internal Bitfield LedBitmap;
        internal Bitfield ColorBitmap = new Bitfield(width: 4, name: "color bitmap");
        internal Bitfield ActionOpcode = new Bitfield(width: 2, name: "action opcode");

        internal GlowImmediateBitfieldWrapper()
        {
            AddBitfield(Type);

            Type.SetValue((int)Instruction.InstrType.GlowImmediate);
        }
    }
}

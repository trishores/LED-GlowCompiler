/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */

namespace ledartstudio
{
    internal class GotoBitfieldWrapper : BitfieldWrapper
    {
        internal Bitfield Type = new Bitfield(width: 4, name: "type");
        internal Bitfield TargetBitAddress = new Bitfield(width: 32, name: "target bit addr");

        internal GotoBitfieldWrapper()
        {
            AddBitfield(Type);
            AddBitfield(TargetBitAddress);

            Type.SetValue((int)Instruction.InstrType.Goto);
        }
    }
}

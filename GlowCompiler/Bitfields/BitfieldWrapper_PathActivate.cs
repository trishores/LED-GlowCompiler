/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */

namespace ledartstudio
{
    internal class PathActivateBitfieldWrapper : BitfieldWrapper
    {
        internal Bitfield Type = new Bitfield(width: 4, name: "type");
        internal Bitfield PathIdx = new Bitfield(width: 8, name: "path index");

        internal PathActivateBitfieldWrapper()
        {
            AddBitfield(Type);
            AddBitfield(PathIdx);

            Type.SetValue((int)Instruction.InstrType.PathActivate);
        }
    }
}

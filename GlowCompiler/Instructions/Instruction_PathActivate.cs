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
    internal class PathActivateInstruction : Instruction
    {
        internal uint TargetPathIdx;
        internal override BitfieldWrapper BitfieldWrapper { get { return _bitfieldWrapper; } set { _bitfieldWrapper = (PathActivateBitfieldWrapper)value; } }
        private PathActivateBitfieldWrapper _bitfieldWrapper;

        private PathActivateInstruction()
        {
            Type = InstrType.PathActivate;
        }

        internal PathActivateInstruction(int path, int zOrder) : this()
        {
            Path = path;
            ZOrder = zOrder;
        }

        internal override void ToBitfieldWrapper()
        {
            _bitfieldWrapper = new PathActivateBitfieldWrapper();
            _bitfieldWrapper.PathIdx.SetValue(TargetPathIdx);
        }

        internal override string ToString()
        {
            var sl = new List<string>();

            if (_bitfieldWrapper == null)
            {
                sl.Add($"path activate: (path={Path}, z-order={ZOrder}, prioritizedThread={PrioritizedThread}, bitaddr={BitAddress})  {TargetPathIdx}");
            }
            else
            {
                sl.Add($"path activate: (path={Path}, z-order={ZOrder}, prioritizedThread={PrioritizedThread}, bitaddr={BitAddress})  {TargetPathIdx}");
                sl.Add(_bitfieldWrapper.Print());
            }

            return string.Join(Environment.NewLine, sl);
        }
    }
}

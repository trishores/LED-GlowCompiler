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
    internal class PathEndInstruction : Instruction
    {
        internal override BitfieldWrapper BitfieldWrapper { get { return _bitfieldWrapper; } set { _bitfieldWrapper = (PathEndBitfieldWrapper)value; } }
        private PathEndBitfieldWrapper _bitfieldWrapper;

        private PathEndInstruction()
        {
            Type = InstrType.PathEnd;
        }

        internal PathEndInstruction(int path, int zOrder) : this()
        {
            Path = path;
            ZOrder = zOrder;
        }

        internal override void ToBitfieldWrapper()
        {
            _bitfieldWrapper = new PathEndBitfieldWrapper();
        }

        internal override string ToString()
        {
            var sl = new List<string>();

            if (_bitfieldWrapper == null)
            {
                sl.Add($"path end: (path={Path}, z-order={ZOrder}, prioritizedThread={PrioritizedThread}, bitaddr={BitAddress})  {Path}");
            }
            else
            {
                sl.Add($"path end: (path={Path}, z-order={ZOrder}, prioritizedThread={PrioritizedThread}, bitaddr={BitAddress})  {Path}");
                sl.Add(_bitfieldWrapper.Print());
            }

            return string.Join(Environment.NewLine, sl);
        }
    }
}

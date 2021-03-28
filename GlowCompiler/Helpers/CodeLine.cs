/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */
 
 namespace ledartstudio
{
    internal class CodeLine
    {
        internal int LineNb { get; private set; }
        internal string LineStr { get; set; }

        internal CodeLine(string lineStr, int lineNb = -1)
        {
            LineNb = lineNb;
            LineStr = lineStr;
        }

        internal CodeLine ToLower()
        {
            LineStr = LineStr.ToLower();
            return this;
        }
    }
}

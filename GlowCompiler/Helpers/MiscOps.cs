/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */

namespace ledartstudio
{
    internal enum Opcode2Bit { Bytes1 = 0, Bytes2 = 1, Bytes3 = 2, Bytes4 = 3 }
    internal enum Opcode3Bit { Bytes0 = 0, Bytes1 = 1, Bytes2 = 2, Bytes3 = 3, Bytes4 = 4 }
    //internal enum TickCompressedOpcode { Tick0 = 0, Tick1 = 1, Tick2 = 2, Tick3 = 3, Tick4 = 4, Bytes1 = 5, Bytes2 = 6, Bytes3 = 7 }

    internal static class MiscOps
    {
        internal static uint ToRange(uint value, uint lowerLimit, uint upperLimit)
        {
            //return value;
            var range = upperLimit - lowerLimit + 1;
            if (value < lowerLimit)
            {
                while (value < lowerLimit)
                {
                    value += range;
                }
            }
            else if (value > upperLimit)
            {
                while (value > upperLimit)
                {
                    value -= range;
                }
            }
            return value;
        }

        internal static uint GetOpcode2Bit(uint value, out uint valueBitWidth)
        {
            if (value < 256)
            {
                valueBitWidth = 8;
                return (uint)Opcode2Bit.Bytes1;
            }

            if (value < 65536)
            {
                valueBitWidth = 16;
                return (uint)Opcode2Bit.Bytes2;
            }

            if (value < 16777216)
            {
                valueBitWidth = 24;
                return (uint)Opcode2Bit.Bytes3;
            }

            valueBitWidth = 32;
            return (uint)Opcode2Bit.Bytes4;
        }

        internal static uint GetOpcode3Bit(uint value, out uint valueWidth)
        {
            if (value == 0)
            {
                valueWidth = 0;
                return (uint)Opcode3Bit.Bytes0;
            }

            if (value < 256)
            {
                valueWidth = 8;
                return (uint)Opcode3Bit.Bytes1;
            }

            if (value < 65536)
            {
                valueWidth = 16;
                return (uint)Opcode3Bit.Bytes2;
            }

            if (value < 16777216)
            {
                valueWidth = 24;
                return (uint)Opcode3Bit.Bytes3;
            }

            valueWidth = 32;
            return (uint)Opcode3Bit.Bytes4;
        }

        /*internal static uint GetTickCompressedOpcode(uint tickValue, out uint tickValueWidth)
        {
            tickValueWidth = 0;

            switch (tickValue)
            {
                case 0:
                    return (uint)TickCompressedOpcode.Tick0;
                case 1:
                    return (uint)TickCompressedOpcode.Tick1;
                case 2:
                    return (uint)TickCompressedOpcode.Tick2;
                case 3:
                    return (uint)TickCompressedOpcode.Tick3;
                case 4:
                    return (uint)TickCompressedOpcode.Tick4;
                default:
                    if (tickValue < 256)
                    {
                        tickValueWidth = 8;
                        return (uint)TickCompressedOpcode.Bytes1;
                    }
                    else if (tickValue < 65536)
                    {
                        tickValueWidth = 16;
                        return (uint)TickCompressedOpcode.Bytes2;
                    }
                    else if (tickValue < 16777216)
                    {
                        tickValueWidth = 24;
                        return (uint)TickCompressedOpcode.Bytes3;
                    }
                    else 
                    {
                        Console.WriteLine(Tick value exceeds max value (2^24).");
                        throw new Exception();
                    }
                }
            }
        }//*/
    }
}

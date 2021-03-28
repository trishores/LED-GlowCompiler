/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ledartstudio
{
    internal abstract class BitfieldWrapper
    {
        private readonly List<Bitfield> _bitfieldList = new List<Bitfield>();

        internal uint BitWidth { get { return (uint)_bitfieldList.Sum(bf => bf.BitWidth); } }

        internal void AddBitfield(Bitfield bitfield)
        {
            _bitfieldList.Add(bitfield);
        }

        internal byte[] ToByteBuffer()
        {
            var byteList = new List<byte>();
            foreach (var bitfield in _bitfieldList)
            {
                byteList.AddRange(bitfield.ToByteList());
            }
            return byteList.ToArray();
        }

        // Instance method to concatenate all bitfields in this wrapper.
        internal virtual Bitfield ToAggregatedBitfield()
        {
            var aggBitfield = _bitfieldList.Aggregate((bf, nextBf) => bf.Concat(nextBf));
            return aggBitfield;
        }

        internal string Print()
        {
            var sb = new StringBuilder();
            uint aggBitfieldWidth = 0;
            
            foreach (var bitfield in _bitfieldList)
            {
                sb.Append(bitfield.Name.Length > 0 ? $"\t{bitfield.Name.PadRight(24)}: " : "");
                sb.Append(bitfield.ToString());
                if (!bitfield.Name.ToLower().EndsWith("opcode"))
                {
                    sb.AppendLine($" (width={bitfield.BitWidth}, value={bitfield.GetValue()}, startbit={aggBitfieldWidth})");
                }
                else if (bitfield.Name.ToLower().EndsWith("action opcode"))
                {
                    var enumStr = Enum.GetName(typeof(ActionOpCode), bitfield.GetValue());
                    sb.AppendLine($" (width={bitfield.BitWidth}, value={enumStr}, startbit={aggBitfieldWidth})");
                }
                else if (bitfield.Name.ToLower().EndsWith("byte width opcode"))
                {
                    string enumStr = "unknown";
                    if (bitfield.BitWidth == 2) enumStr = Enum.GetName(typeof(Opcode2Bit), bitfield.GetValue());
                    if (bitfield.BitWidth == 3) enumStr = Enum.GetName(typeof(Opcode3Bit), bitfield.GetValue());
                    sb.AppendLine($" (width={bitfield.BitWidth}, value={enumStr}, startbit={aggBitfieldWidth})");
                }
                else sb.AppendLine();
                aggBitfieldWidth += bitfield.BitWidth;
            }
            sb.AppendLine($"\t{"Concat bitfields".PadRight(24)}: {ToAggregatedBitfield().ToString()}");

            return sb.ToString();
        }
    }
}
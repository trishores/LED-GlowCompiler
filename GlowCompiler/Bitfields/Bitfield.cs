/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ledartstudio
{
    internal class Bitfield
    {
        protected internal uint BitWidth { get { return (uint)BitArray.Length; } }
        protected internal BitArray BitArray;
        protected internal string Name;

        internal Bitfield(uint width, string name = "")
        {
            BitArray = new BitArray((int)width);
            Name = name;
        }

        internal Bitfield(uint width, uint value, string name = "")
        {
            BitArray = new BitArray((int)width);
            SetBitfieldValue(value);
            Name = name;
        }

        internal int GetValue()
        {
            return GetBitfieldValue();
        }

        internal void SetValue(uint value)
        {
            SetBitfieldValue(value);
        }

        internal void SetFlag(uint flagIndex)
        {
            SetBitfieldFlagValue(flagIndex);
        }

        internal new string ToString()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < BitWidth; i++)
            {
                sb.Insert(0, BitArray.Get(i) ? 1 : 0);
            }
            return sb.ToString();
        }

        internal List<byte> ToByteList()
        {
            var byteList = new List<byte>();
            byte byteVal = 0, bitIndex = 0;
            for (var i = 0; i < BitWidth; i++)
            {
                if (bitIndex > 7)
                {
                    byteList.Add(byteVal);
                    bitIndex = 0;
                    byteVal = 0;
                }
                var bitVal = BitArray.Get((int)BitWidth - 1 - i) ? 1 : 0;
                byteVal += (byte)(bitVal << (7 - bitIndex));
                bitIndex++;
            }
            if (bitIndex > 0) byteList.Add(byteVal);
            return byteList;
        }

        #region Helper methods

        private int GetBitfieldValue()
        {
            var val = 0;
            for (var i = 0; i < BitWidth; i++)
            {
                var bitVal = BitArray.Get(i) ? 1 : 0;
                val += bitVal << i;
            }
            return val;
        }

        private void SetBitfieldValue(uint value)
        {
            var val = value;
            for (var i = 0; i < BitWidth; i++)
            {
                BitArray.Set(i, val % 2 > 0);
                val = val >> 1;
            }
            if (val > 0)
            {
                Console.WriteLine($"Bitfield value {value} exceeds bitfield width {BitWidth}");
                throw new Exception();
            }
        }

        private void SetBitfieldFlagValue(uint flagIndex)
        {
            if (flagIndex > BitWidth - 1)
            {
                Console.WriteLine($"Flag index {flagIndex} exceeds bitfield width {BitWidth}");
                throw new Exception();
            }
            BitArray.Set((int)flagIndex, true);
        }

        #endregion
    }

    internal static class BitfieldExtensions
    {
        internal static Bitfield Concat(this Bitfield primaryBitfield, Bitfield followingBitfield)
        {
            var newBitfield = new Bitfield(primaryBitfield.BitWidth + followingBitfield.BitWidth);
            var j = 0;
            for (var i = 0; i < newBitfield.BitWidth; i++)
            {
                newBitfield.BitArray.Set(i, i < followingBitfield.BitArray.Length ? followingBitfield.BitArray.Get(i) : primaryBitfield.BitArray.Get(j++));
            }
            return newBitfield;
        }

        internal static Bitfield InsertAt(this Bitfield primaryBitfield, int primaryIndex, Bitfield insertBitfield)
        {
            var rightToLeftPrimaryIndex = primaryBitfield.BitWidth - primaryIndex;
            var newBitfield = new Bitfield(primaryBitfield.BitWidth + insertBitfield.BitWidth);
            var primaryBitfieldIterator = 0;
            var insertBitfieldIterator = 0;
            for (var i = 0; i < newBitfield.BitWidth; i++)
            {
                newBitfield.BitArray.Set(i, 
                    (i < rightToLeftPrimaryIndex || i >= (rightToLeftPrimaryIndex + insertBitfield.BitWidth)) ? 
                    primaryBitfield.BitArray.Get(primaryBitfieldIterator++) : 
                    insertBitfield.BitArray.Get(insertBitfieldIterator++));
            }
            return newBitfield;
        }
    }
}

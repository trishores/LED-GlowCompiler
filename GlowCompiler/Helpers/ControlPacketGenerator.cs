/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */


using System;
using System.Linq;

namespace ledartstudio
{
    internal class ControlPacketGenerator
    {
        private readonly ArgHandler _args;
        private const int BitsPerByte = 8;

        internal ControlPacketGenerator(ArgHandler args)
        {
            _args = args;
        }

        internal byte[] GetPauseLightshowPacket()
        {
            var ctrlStopPacket = GenerateControlPacket(DeviceTable.Dev.SaveToRom ? MemoryOpcode.Nvm : MemoryOpcode.Sram, ControlOpcode.PauseLightshow).ToByteList();
            return ctrlStopPacket.ToArray();
        }

        internal byte[] GetResumeLightshowPacket()
        {
            var ctrlResumePacket = GenerateControlPacket(DeviceTable.Dev.SaveToRom ? MemoryOpcode.Nvm : MemoryOpcode.Sram, ControlOpcode.ResumeLightshow).ToByteList();
            return ctrlResumePacket.ToArray();
        }

        internal byte[] GetStartLightshowPacket()
        {
            var ctrlRestartPacket = GenerateControlPacket(DeviceTable.Dev.SaveToRom ? MemoryOpcode.Nvm : MemoryOpcode.Sram, ControlOpcode.RestartLightshow).ToByteList();
            return ctrlRestartPacket.ToArray();
        }

        internal byte[] GetDownloadLightshowPackets(byte[] lightshowByteArray)
        {
            var ctrlStorePacket = GenerateControlPacket(DeviceTable.Dev.SaveToRom ? MemoryOpcode.Nvm : MemoryOpcode.Sram, ControlOpcode.StoreNewPackets).ToByteList();
            return ctrlStorePacket.Concat(lightshowByteArray).ToArray();
        }

        #region Helper methods

        private Bitfield GenerateControlPacket(MemoryOpcode memoryOpcode, ControlOpcode controlOpcode)
        {
            if (DeviceTable.Dev.UsbPacketByteLen == 0)
            {
                Console.WriteLine("Invalid packet byte length.");
                throw new Exception();
            }
            var packetBitLen = DeviceTable.Dev.UsbPacketByteLen * BitsPerByte;

            // Generate run control bitfield:
            var controlInstrBitfield = new ControlBitfieldWrapper();
            controlInstrBitfield.MemoryOpcode.SetValue((uint)memoryOpcode);
            controlInstrBitfield.ControlOpcode.SetValue((uint)controlOpcode);

            // Add padding to get packet-sized bitfield:
            var padBitfield = new Bitfield(width: packetBitLen - controlInstrBitfield.BitWidth);
            padBitfield.BitArray.SetAll(true);  // pad with 1's.
            controlInstrBitfield.AddBitfield(padBitfield);

            return controlInstrBitfield.ToAggregatedBitfield();
        }

        #endregion
    }
}
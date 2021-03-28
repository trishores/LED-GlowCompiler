/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */


using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ledartstudio
{
    internal static class DeviceTable
    {
        internal static List<CodeLine> CodeLines = new List<CodeLine>();
        internal static Device Dev = new Device();
        internal static Protocol Proto = new Protocol();

        internal static bool Build()
        {
            if (CodeLines.Count == 0)
            {
                Console.WriteLine("No device specifications found.");
                return false;
            }

            // Read device parameter values:
            foreach (var codeLine in CodeLines)
            {
                var value = "";
                try
                {
                    if (GetDeviceValue(codeLine.LineStr, "name", out value))
                    {
                        Dev.Name = value;  // leave in mixed case for readability.
                    }
                    else if (GetDeviceValue(codeLine.LineStr, "type", out value))
                    {
                        Dev.Type = value.ToLower();  // convert to lower case.
                    }
                    else if (GetDeviceValue(codeLine.LineStr, "usbvendorid", out value))
                    {
                        Dev.UsbVendorId = value.ToLower();  // convert to lower case.
                    }
                    else if (GetDeviceValue(codeLine.LineStr, "usbproductid", out value))
                    {
                        Dev.UsbProductId = value.ToLower();  // convert to lower case.
                    }
                    else if (GetDeviceValue(codeLine.LineStr, "usbpacketbytelen", out value))
                    {
                        Dev.UsbPacketByteLen = ParseIntStr(value);
                    }
                    else if (GetDeviceValue(codeLine.LineStr, "tickintervalmillisecs", out value))
                    {
                        Dev.TickIntervalMs = ParseIntStr(value);
                    }
                    else if (GetDeviceValue(codeLine.LineStr, "ramspacebytes", out value))
                    {
                        Dev.RamSpaceBytes = ParseIntStr(value);
                    }
                    else if (GetDeviceValue(codeLine.LineStr, "romspacebytes", out value))
                    {
                        Dev.RomSpaceBytes = ParseIntStr(value);
                    }
                    else if (GetDeviceValue(codeLine.LineStr, "ledcount", out value))
                    {
                        Dev.LedCount = ParseIntStr(value);
                    }
                    else if (GetDeviceValue(codeLine.LineStr, "protocolversion", out value))
                    {
                        Dev.ProtocolVersion = value.ToLower();
                    }
                    else if (GetDeviceValue(codeLine.LineStr, "savetorom", out value))
                    {
                        Dev.SaveToRom = ParseBoolString(value);
                    }
                    else
                    {
                        Console.WriteLine($"Invalid 'device' instruction");
                        throw new Exception();
                    }
                }
                catch
                {
                    Console.Write($"Error parsing line {codeLine.LineNb}");
                    throw new Exception();
                }
            }
            return true;
        }

        private static bool GetDeviceValue(string str, string key, out string strVal)
        {
            strVal = "";

            // Use regex to parse device instruction:
            const string gap = @"[\s|\t]*";
            const string valuePtn = "['\"](.*?)['\"]";
            var pattern = $"^device{gap}:{gap}{key}{gap}={gap}{valuePtn}$";
            var match = Regex.Match(str, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                strVal = match.Groups[1].Value;
                return true;
            }
            return false;
        }

        internal static uint ParseIntStr(string strVal)
        {
            if (uint.TryParse(strVal, out var intVal)) return intVal;
            Console.WriteLine($"Error parsing integer value: {strVal}");
            throw new Exception();
        }

        internal static bool ParseBoolString(string strVal)
        {
            switch (strVal.ToLower())
            {
                case "true":
                case "yes":
                    return true;
                case "false":
                case "no":
                    return false;
                default:
                    Console.WriteLine($"Error parsing boolean value: {strVal}");
                    throw new Exception();
            }
        }

        internal class Device
        {
            // Set device spec defaults:
            internal string Name = "unknown";
            internal string Type = "unknown";
            internal uint TickIntervalMs = 0;
            internal string ProtocolVersion = "unknown";
            internal uint LedCount = 0;
            internal uint RamSpaceBytes = 0;
            internal uint RomSpaceBytes = 0;
            internal string UsbVendorId = "unknown";
            internal string UsbProductId = "unknown";
            internal uint UsbPacketByteLen = 64;
            internal bool SaveToRom = false;
            internal uint SimulatorBrightnessCoeff = 0; // simulator alpha value for a given lightshow.
        }

        internal class Protocol
        {
            // Set protocol limits for validation of user input:
            internal uint MaxPauseMillisecs = (uint)Math.Pow(2, 32);
            internal uint MaxThreads = 256;
        }
    }
}

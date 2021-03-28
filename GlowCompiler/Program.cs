/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ledartstudio
{
    internal enum ExitCode { Success = 0, Fail = 1 }

    internal class Program
    {
        internal enum LineType { Undefined, Function }
        internal static string ProtocolVersion = "1.0";

        private static int Main(string[] userArgs)
        {
            Console.WriteLine("Starting build...");

            try
            {
                // Handle args:
                var args = new ArgHandler(userArgs);

                if (args.Lines == null && args.Lines.Count() == 0)
                {
                    Console.WriteLine($"Empty or invalid code file.");
                    throw new Exception();
                }

                // Remove comments, extra whitespace, tabs, empty lines, etc:
                var codeLines = CodeCleanup(args.Lines);

                // Store code lines in their respective table:
                PopulateCodeTables(codeLines);

                // Build tables:
                DeviceTable.Build();
                DefineTable.Build();
                FunctionTable.Build();

                // Compile/link start function:
                var compiler = new Compiler(args);
                var isSuccessful = compiler.Compile(out byte[] lightshowByteArray, out uint contextRegionByteSize, out uint maxInstrPathByteSize, out int threadCount);
                if (lightshowByteArray == null)
                {
                    Console.WriteLine("No build data generated.");
                    throw new Exception();
                }
                if (DeviceTable.Dev.SimulatorBrightnessCoeff == 0)
                {
                    Console.WriteLine("Invalid simulator brightness coefficient.");
                    throw new Exception();
                }
                Console.WriteLine($"Device threads usage: {Math.Round(100f * threadCount / DeviceTable.Proto.MaxThreads, 1):F1}% ({threadCount} of {DeviceTable.Proto.MaxThreads} threads).");
                CheckMemoryUsage(lightshowByteArray, contextRegionByteSize, maxInstrPathByteSize);

                Console.WriteLine("Build completed successfully.");

                // Generate output files (xml/bin):
                var controlPacketGenerator = new ControlPacketGenerator(args);
                var downloadLightshowByteArray = controlPacketGenerator.GetDownloadLightshowPackets(lightshowByteArray);
                var startLightshowByteArray = controlPacketGenerator.GetStartLightshowPacket();
                var pauseLightshowByteArray = controlPacketGenerator.GetPauseLightshowPacket();
                var resumeLightshowByteArray = controlPacketGenerator.GetResumeLightshowPacket();
                var outputXmlFilePath = Path.Combine(Path.GetDirectoryName(args.InputFilePath), Path.GetFileNameWithoutExtension(args.InputFilePath) + ".xml");
                var outputBinFilePath = Path.Combine(Path.GetDirectoryName(args.InputFilePath), Path.GetFileNameWithoutExtension(args.InputFilePath) + ".bin");
                PrintOutput(
                    lightshowByteArray, downloadLightshowByteArray, startLightshowByteArray, pauseLightshowByteArray, resumeLightshowByteArray,
                    outputXmlFilePath, outputBinFilePath);

                return (int)ExitCode.Success;
            }
            catch
            {
                return (int)ExitCode.Fail;
            }
        }

        #region Helper methods

        private static void PrintOutput(
            byte[] lightshowByteArray, byte[] downloadLightshowByteArray, byte[] startLightshowByteArray, byte[] pauseLightshowByteArray, byte[] resumeLightshowByteArray,
            string outputXmlFilePath, string outputBinFilePath)
        {
            var packetByteLen = DeviceTable.Dev.UsbPacketByteLen;

            // Generate xml file:
            var deviceElement = new XElement("device");
            deviceElement.Add(new XElement("name", DeviceTable.Dev.Name));
            deviceElement.Add(new XElement("type", DeviceTable.Dev.Type));
            deviceElement.Add(new XElement("tickIntervalMillisecs", DeviceTable.Dev.TickIntervalMs));
            deviceElement.Add(new XElement("protocolVersion", DeviceTable.Dev.ProtocolVersion));
            deviceElement.Add(new XElement("ledCount", DeviceTable.Dev.LedCount));
            deviceElement.Add(new XElement("ramSpaceBytes", DeviceTable.Dev.RamSpaceBytes));
            deviceElement.Add(new XElement("romSpaceBytes", DeviceTable.Dev.RomSpaceBytes));
            if (DeviceTable.Dev.Type == "usb")
            {
                deviceElement.Add(new XElement("usbVendorId", DeviceTable.Dev.UsbVendorId));
                deviceElement.Add(new XElement("usbProductId", DeviceTable.Dev.UsbProductId));
                deviceElement.Add(new XElement("usbPacketByteLen", DeviceTable.Dev.UsbPacketByteLen));
                deviceElement.Add(new XElement("saveToRom", DeviceTable.Dev.SaveToRom));
            }

            var xdoc = new XDocument(
                new XElement("compiler",
                    deviceElement,
                    new XElement("downloadLightshowPackets", GetFormattedByteArrayString(downloadLightshowByteArray, DeviceTable.Dev.UsbPacketByteLen)),
                    new XElement("startLightshowPackets", GetFormattedByteArrayString(startLightshowByteArray, DeviceTable.Dev.UsbPacketByteLen)),
                    new XElement("pauseLightshowPackets", GetFormattedByteArrayString(pauseLightshowByteArray, DeviceTable.Dev.UsbPacketByteLen)),
                    new XElement("resumeLightshowPackets", GetFormattedByteArrayString(resumeLightshowByteArray, DeviceTable.Dev.UsbPacketByteLen))
            ));
            File.WriteAllText(outputXmlFilePath, xdoc.ToString());

            // Generate simulator bin file:
            using (FileStream stream = new FileStream(outputBinFilePath, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(lightshowByteArray);
                    writer.Close();
                }
            }
        }

        private static string GetFormattedByteArrayString(byte[] byteArray, uint packetByteLen, int width = 8)
        {
            var sb = new StringBuilder(Environment.NewLine);
            for (var i = 0; i < byteArray.Length; i++)
            {
                if (i % width == 0) sb.Append("    ");
                sb.Append(string.Format("0x{0,2:X2}, ", byteArray[i]));
                if (i % width == width - 1) sb.AppendLine();
                if (i % packetByteLen == (packetByteLen - 1) && i != byteArray.Length - 1) sb.AppendLine();
                if (i == byteArray.Length - 1) sb.Append("  ");
            }
            return sb.ToString();
        }

        private static void CheckMemoryUsage(byte[] lightshowByteArray, uint contextRegionByteSize, uint maxInstrPathByteSize)
        {
            uint ramBytes, romBytes;

            // Check whether device has sufficient lightshow storage capacity:
            if (DeviceTable.Dev.SaveToRom)
            {
                ramBytes = contextRegionByteSize + maxInstrPathByteSize;
                romBytes = (uint)lightshowByteArray.Length;
            }
            else
            {
                ramBytes = (uint)lightshowByteArray.Length;
                romBytes = 0;
            }
            var ramPercent = Math.Round(100f * ramBytes / DeviceTable.Dev.RamSpaceBytes, 1);
            var romPercent = Math.Round(100f * romBytes / DeviceTable.Dev.RomSpaceBytes, 1);
            Console.WriteLine(string.Format("Device RAM usage: {0:F1}% ({1:n0} of {2:n0} bytes).", ramPercent, ramBytes, DeviceTable.Dev.RamSpaceBytes));
            Console.WriteLine(string.Format("Device ROM usage: {0:F1}% ({1:n0} of {2:n0} bytes).", romPercent, romBytes, DeviceTable.Dev.RomSpaceBytes));

            if (ramBytes > DeviceTable.Dev.RamSpaceBytes || romBytes > DeviceTable.Dev.RomSpaceBytes)
            {
                Console.WriteLine("Exceeded available memory.");
                throw new Exception();
            }
        }

        private static CodeLine[] CodeCleanup(string[] lines)
        {
            var codeLineList = new List<CodeLine>();

            for (var i = 0; i < lines.Count(); i++)
            {
                var line = lines[i];
                // Remove commented lines or trailing comments:
                line = line.Substring(0, line.IndexOf("//", StringComparison.Ordinal) < 0 ? line.Length : line.IndexOf("//", StringComparison.Ordinal));
                // Remove leading/trailing whitespace/tabs:
                line = line.Trim(' ', '\t');
                // Convert internal tab(s) to whitespace:
                line = Regex.Replace(line, @"\t+", @" ");
                // Remove duplicate internal whitespace:
                line = Regex.Replace(line, @"\s+", @" ");
                // Skip empty lines:
                if (string.IsNullOrWhiteSpace(line)) continue;

                codeLineList.Add(new CodeLine(line, i + 1));
            }
            return codeLineList.ToArray();
        }

        private static void PopulateCodeTables(CodeLine[] codeLines)
        {
            var lineType = LineType.Undefined;

            foreach (var codeLine in codeLines)
            {
                try
                {
                    if (codeLine.LineStr.ToLower().StartsWith("device:"))
                    {
                        DeviceTable.CodeLines.Add(codeLine);  // do not convert to lower case yet.
                    }
                    else if (codeLine.LineStr.ToLower().StartsWith("define:"))
                    {
                        DefineTable.CodeLines.Add(codeLine.ToLower());  // convert to lower case.
                    }
                    else if (codeLine.LineStr.ToLower().StartsWith("@"))
                    {
                        lineType = LineType.Function;
                        FunctionTable.FuncList.Add(new Function(funcName: codeLine.LineStr.ToLower()));  // convert to lower case.
                    }
                    else if (lineType == LineType.Function)
                    {
                        FunctionTable.FuncList.LastOrDefault()?.CodeLines.Add(codeLine.ToLower());  // convert to lower case.
                    }
                    else
                    {
                        Console.WriteLine($"Unknown code line");
                        throw new Exception();
                    }
                }
                catch
                {
                    Console.Write($"Error parsing line {codeLine.LineNb}");
                    throw new Exception();
                }
            }
        }

        #endregion
    }
}

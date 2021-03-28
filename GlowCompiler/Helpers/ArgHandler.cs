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

namespace ledartstudio
{
    internal class ArgHandler
    {
        private enum ExitCode
        {
            Success,
            InvalidArgs,
            InvalidFileInputPath,
            InvalidUsbExecFileInputPath,
            ReadError
        }
        private const string argInputFilePath = "-InputFilePath";  // file containing lightshow code (output files are saved in same dir).
        private const string argBuild = "-Build";   // command to build lightshow code.
        internal string[] Lines;
        internal string InputFilePath;
        internal bool BuildLightshow;
        private List<string> _switchArgs = new List<string>();

        internal ArgHandler(string[] args)
        {
            Parse(args);
        }

        internal void Parse(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("One or more args expected.");
                Environment.Exit((int)ExitCode.InvalidArgs);
            }

            // Build list of accepted switches:
            _switchArgs.Add(argInputFilePath);
            _switchArgs.Add(argBuild);

            string prevSwitchArg = null;
            foreach (var currArg in args)
            {
                if (currArg.StartsWith("-"))
                {
                    if (!_switchArgs.ToList().Any(x => x.Equals(currArg, StringComparison.OrdinalIgnoreCase)))
                    {
                        Console.WriteLine($"Unrecognized switch '{currArg}'.");
                        Environment.Exit((int)ExitCode.InvalidArgs);
                    }

                    prevSwitchArg = null;

                    if (currArg.Equals(argBuild, StringComparison.OrdinalIgnoreCase))
                    {
                        BuildLightshow = true;
                    }
                    else
                    {
                        prevSwitchArg = currArg;
                    }

                    continue;
                }
                else if (!currArg.StartsWith("-") && prevSwitchArg == null)
                {
                    Console.WriteLine($"Unrecognized arg '{currArg}'.");
                    Environment.Exit((int)ExitCode.InvalidArgs);
                }
                else if (!currArg.StartsWith("-") && prevSwitchArg != null)
                {
                    if (prevSwitchArg.Equals(argInputFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        InputFilePath = currArg;
                    }
                    else
                    {
                        Console.WriteLine($"Unrecognized switch arg '{prevSwitchArg}'.");
                        Environment.Exit((int)ExitCode.InvalidArgs);
                    }

                    prevSwitchArg = null;
                    continue;
                }
            }

            if (InputFilePath == null)
            {
                Console.WriteLine("Input file path arg missing.");
                Environment.Exit((int)ExitCode.InvalidFileInputPath);
            }
            if (InputFilePath != null && !File.Exists(InputFilePath))
            {
                Console.WriteLine("Invalid input file path.");
                Environment.Exit((int)ExitCode.InvalidFileInputPath);
            }

            if (!BuildLightshow)
            {
                Console.WriteLine("Try using -build switch.");
                Environment.Exit((int)ExitCode.InvalidArgs);
            }

            try
            {
                Lines = File.ReadAllLines(InputFilePath);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Input file read error: {e.Message}");
                Environment.Exit((int)ExitCode.ReadError);
            }
        }
    }
}

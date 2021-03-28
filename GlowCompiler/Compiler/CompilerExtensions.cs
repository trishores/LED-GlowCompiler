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
using System.Text.RegularExpressions;

namespace ledartstudio
{
    internal static class CompilerExtensions
    {
        internal static bool IsGlowImmediateInstruction(this string str)
        {
            const string instrPattern = "glowimmediate";
            const string gap = @"[\s|\t]*";
            var pattern = $"^{instrPattern}{gap}:";
            var match = Regex.Match(str, pattern);
            return match.Success;
        }

        internal static bool IsGlowRampInstruction(this string str)
        {
            const string instrPattern = "glowramp";
            const string gap = @"[\s|\t]*";
            var pattern = $"^{instrPattern}{gap}:";
            var match = Regex.Match(str, pattern);
            return match.Success;
        }

        internal static bool IsCallInstruction(this string str, out int repeatCount)
        {
            repeatCount = 1;

            const string instrPattern = "call";
            const string gap = @"[\s|\t]*";
            const string funcPattern = "(@[a-z][a-z_0-9]*)";
            const string repeatPattern = @"\(repeat=(\d+)\)";
            var pattern1 = $"^{instrPattern}{gap}:{gap}{funcPattern}$";
            var pattern2 = $@"^{instrPattern}{gap}:{gap}{funcPattern}{gap}{repeatPattern}$";

            var match1 = Regex.Match(str, pattern1);
            if (match1.Success)
            {
                return true;
            }
            
            var match2 = Regex.Match(str, pattern2);
            if (match2.Success)
            {
                repeatCount = int.Parse(match2.Groups[2].Value);

                // Validate repeat value:
                if (repeatCount < 1 || repeatCount > 2000000000)
                {
                    Console.WriteLine($"Invalid repeat value: {repeatCount}");
                    throw new Exception();
                }

                return true;
            }

            return false;
        }

        internal static bool IsCallAsyncInstruction(this string str, int oldZOrder, out int newZOrder)
        {
            newZOrder = oldZOrder;

            const string instrPattern = "callasync";
            const string gap = @"[\s|\t]*";
            const string funcPattern = "(@[a-z][a-z_0-9]*)";
            const string zOrderPattern = @"\(zorder=(\d+)\)";

            var pattern1 = $"^{instrPattern}{gap}:{gap}{funcPattern}$";
            var match1 = Regex.Match(str, pattern1);
            if (match1.Success)
            {
                return true;
            }

            var pattern2 = $"^{instrPattern}{gap}:{gap}{funcPattern}{gap}{zOrderPattern}$";
            var match2 = Regex.Match(str, pattern2);
            if (match2.Success)
            {
                newZOrder = int.Parse(match2.Groups[2].Value);

                // Validate zorder value:
                if (newZOrder < 0 || newZOrder > 1000)
                {
                    Console.WriteLine($"Invalid z-order value: {newZOrder}");
                    throw new Exception();
                }

                return true;
            }

            return false;
        }

        internal static bool IsPauseInstruction(this string str)
        {
            const string instrPattern = "pause";
            const string gap = @"[\s|\t]*";
            var pattern = $"^{instrPattern}{gap}:";
            var match = Regex.Match(str, pattern);
            return match.Success;
        }

        internal static bool IsHereInstruction(this string str)
        {
            const string instrPattern = "here";
            const string gap = @"[\s|\t]*";
            var pattern = $"^{instrPattern}{gap}:";
            var match = Regex.Match(str, pattern);
            return match.Success;
        }

        internal static bool IsGotoInstruction(this string str)
        {
            const string instrPattern = "goto";
            const string gap = @"[\s|\t]*";
            var pattern = $"^{instrPattern}{gap}:";
            var match = Regex.Match(str, pattern);
            return match.Success;
        }

        internal static List<int> GetLeds(this string str)
        {
            // Remove any internal spaces/tabs:
            str = str.RemoveChars(new[] { ' ', '\t' });

            // Expand hyphenated ranges:
            string expandAction(string lowerValStr, string upperValStr)
            {
                var lowerVal = int.Parse(lowerValStr);
                var upperVal = int.Parse(upperValStr);
                var intList = new List<int>();
                do
                {
                    intList.Add(lowerVal);
                }
                while (lowerVal++ < upperVal);
                return string.Join(",", intList);
            }
            var pattern = @"(\d+)-(\d+)";
            var matches = Regex.Matches(str, pattern);
            var expStr = str;
            foreach (Match m in matches)
            {
                expStr = expStr.Replace($"{m.Groups[1].Value}-{m.Groups[2].Value}", expandAction(m.Groups[1].Value, m.Groups[2].Value));
            }

            // Convert from 1-based to 0-based led index, and remove any duplicates:
            var ledIdxList = expStr.Split(',').Select(x => int.Parse(x) - 1).Distinct().ToList();

            // Validate led indices:
            if (ledIdxList.Any(ledIdx => ledIdx < 0 || ledIdx >= DeviceTable.Dev.LedCount))
            {
                Console.WriteLine("LED index value(s) invalid");
                throw new Exception();
            }

            return ledIdxList;
        }

        internal static LedState GetColor(this string str)
        {
            // Remove any internal spaces/tabs:
            str = str.RemoveChars(new[] { ' ', '\t' });

            string pattern = @"(\d+,\d+,\d+,\d+)";
            var match = Regex.Match(str, pattern);
            if (!match.Success) throw new Exception();

            var color = new LedState
            {
                // Convert from 1-based to 0-based led index:
                Red = int.Parse(str.Split(',')[0]),
                Green = int.Parse(str.Split(',')[1]),
                Blue = int.Parse(str.Split(',')[2]),
                Bright = int.Parse(str.Split(',')[3])
            };

            // Validation color component values:
            if ((color.Red < 0 || color.Red > 255) ||
                (color.Green < 0 || color.Green > 255) ||
                (color.Blue < 0 || color.Blue > 255) ||
                (color.Bright < 0 || color.Bright > 31))
            {
                Console.WriteLine("Color value(s) outside allowed range");
                throw new Exception();
            }
            
            return color;
        }

        internal static long GetDuration(this string str, string timeUnit)
        {
            // Get duration in millisecs:
            long durationMs = 0;
            if (timeUnit == "ms") durationMs = long.Parse(str);
            if (timeUnit == "s") durationMs = long.Parse(str) * 1000;
            if (timeUnit == "m") durationMs = long.Parse(str) * 60000;
            if (timeUnit == "h") durationMs = long.Parse(str) * 3600000;
            if (timeUnit == "t") durationMs = long.Parse(str) * DeviceTable.Dev.TickIntervalMs;

            return durationMs;
        }

        internal static string NoWhitespace(this string str)
        {
            return str.Replace(" ", "");
        }

        internal static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
        {
            return items.GroupBy(property).Select(x => x.First());
        }

        internal static string RemoveChars(this string s, IEnumerable<char> excludeChars)
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (!excludeChars.Contains(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }
}

/*  
 *  Copyright 2018-2021 ledmaker.org
 *  
 *  This file is part of Glow Compiler.
 *  
 */


using System.Collections.Generic;

namespace ledartstudio
{
    internal class LedstripState
    {
        internal List<LedState> LedStateList = new List<LedState>();

        internal LedstripState() { }

        internal LedstripState(uint numLeds, int initVal = 0)
        {
            for (var i = 0; i < numLeds; i++) LedStateList.Add(new LedState(red: initVal, green: initVal, blue: initVal, bright: initVal));
        }

        internal LedstripState DeepCopy()
        {
            var ledstripState = new LedstripState();
            LedStateList.ForEach(led => ledstripState.LedStateList.Add(led.DeepCopy()));
            return ledstripState;
        }
    }

    internal class LedState
    {
        internal int Red;
        internal int Green;
        internal int Blue;
        internal int Bright;

        internal LedState(int red = 0, int green = 0, int blue = 0, int bright = 0)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Bright = bright;
        }

        internal LedState DeepCopy()
        {
            var ledState = new LedState(Red, Green, Blue, Bright);
            return ledState;
        }

        internal bool Equals(LedState otherLedState)
        {
            return (
                Red == otherLedState.Red &&
                Green == otherLedState.Green &&
                Blue == otherLedState.Blue &&
                Bright == otherLedState.Bright);
        }
    }

    internal static class LedStateExtensions
    {
        internal static bool IsColor(this LedState ledState, int red, int green, int blue, int bright)
        {
            return ledState.Red == red &&
                   ledState.Green == green &&
                   ledState.Blue == blue &&
                   ledState.Bright == bright;
        }
    }
}

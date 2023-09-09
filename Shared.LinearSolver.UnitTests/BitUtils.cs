using System.Runtime.InteropServices;
using System;

namespace Shared.LinearSolver.UnitTests
{
    internal static class BitUtils
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct ConverterStruct
        {
            [FieldOffset(0)] public int asInt;
            [FieldOffset(0)] public float asFloat;
        }

        public static string FormatAsBits(float val)
        {
            ConverterStruct a; a.asInt = 0; a.asFloat = val;
            return FormatAsBits(a.asInt);
        }

        public static string FormatAsBits(int val)
        {
            var str = Convert.ToString(val, 2).PadLeft(32, '0');
            return string.Concat(str[0], " ", str.Substring(1, 8), " ", str.Substring(9));
        }

        public static string FormatAsBits(uint val)
        {
            var str = Convert.ToString(val, 2).PadLeft(32, '0');
            return string.Concat(str[0], " ", str.Substring(1, 8), " ", str.Substring(9));
        }
    }
}
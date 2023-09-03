using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Shared.LinearSolver
{
    internal static class MathOp
    {
        public static int Gcd(int a, int b)
        {
            if (b > a)
            {
                Math.DivRem(b, a, out b);
            }
            while (b > 0)
            {
                Math.DivRem(a, b, out a);
                if (a == 0) return b;
                Math.DivRem(b, a, out b);
            }
            return a;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct ConverterStruct
        {
            [FieldOffset(0)] public int asInt;
            [FieldOffset(0)] public float asFloat;
        }

        /// <summary>
        /// Fast FLOOR(LOG2(float)) using bitwise ops to extract the exponent of a normalised float.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2Floor(float val)
        {
            ConverterStruct a; a.asInt = 0; a.asFloat = val;
            return ((a.asInt >> 23) + 1) & 0x7F;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2FloorReference(float val) => (int)Math.Log(val, 2);
    }
}

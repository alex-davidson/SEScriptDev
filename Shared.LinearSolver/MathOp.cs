using System;
using System.Collections.Generic;
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
    }
}

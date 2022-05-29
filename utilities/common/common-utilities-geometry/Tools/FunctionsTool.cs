/// Copyright 2022- Burak Kara, All rights reserved.

using System;

namespace CommonUtilities.Geometry.Tools
{
    public static class FunctionsTool
    {
        public static readonly double MinLogValue = 1.0E-40;

        public static double Atan2(double _X, double _Y) { return _X == 0 && _Y == 0 ? double.NaN : Math.Atan2(_Y, _X); }
        public static double Sinc(double _X) { if (_X == 0.0) return 1.0; _X *= Math.PI; return Math.Sin(_X) / _X; }

        public static double Length(double _X, double _Y, double _Z) { return Math.Sqrt(_X * _X + _Y * _Y + _Z * _Z); }

        public static double Hypotenuse(double _A, double _B)
        {
            if (Math.Abs(_A) > Math.Abs(_B))
            {
                var R = _B / _A;
                return Math.Abs(_A) * Math.Sqrt(1 + R * R);
            }
            if (_B != 0)
            {
                var R = _A / _B;
                return Math.Abs(_B) * Math.Sqrt(1 + R * R);
            }
            return 0.0;
        }
    }
}
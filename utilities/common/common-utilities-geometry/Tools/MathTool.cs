/// Copyright 2022- Burak Kara, All rights reserved.

using System;

namespace CommonUtilities.Geometry.Tools
{
    public static class MathTool
    {
        public const int RandomSeed = 22141514;
        public const double G = 9.80665;
        public const double Sqrt2 = 1.4142135623730950488016887;
        public const double InvSqrt2 = 1.0 / Sqrt2;
        public const double HalfSqrt2 = Sqrt2 / 2;

        public static string ToString(double _Value, int _Decimals)
        {
            if (_Decimals == 0)
                return IntByRound(_Value).ToString();

            return string.Format($"{{0:0.{new string('0', _Decimals)}}}", _Value);
        }

        public static int IntByFloor(double _X) { return Convert.ToInt32(Math.Floor(_X)); }

        public static int IntByRound(double _X) { return (int)Math.Round(_X, MidpointRounding.AwayFromZero); }

        public static int IntByCeiling(double _X) { return Convert.ToInt32(Math.Ceiling(_X)); }

        public static int Min(int _A, int _B, int _C) { return Math.Min(_A, Math.Min(_B, _C)); }

        public static int Max(int _A, int _B, int _C) { return Math.Max(_A, Math.Max(_B, _C)); }

        public static double AbsMin(double _A, double _B) { return Math.Min(Math.Abs(_A), Math.Abs(_B)); }

        public static double AbsMax(double _A, double _B) { return Math.Max(Math.Abs(_A), Math.Abs(_B)); }

        public static int AbsMinIndex(double _A, double _B) { return Math.Abs(_A) < Math.Abs(_B) ? 0 : 1; }

        public static int AbsMaxIndex(double _A, double _B) { return Math.Abs(_A) > Math.Abs(_B) ? 0 : 1; }

        public static int AbsMinIndex(double _A, double _B, double c) { return Math.Abs(c) < AbsMin(_A, _B) ? 2 : AbsMinIndex(_A, _B); }
        
        public static int AbsMaxIndex(double _A, double _B, double c) { return Math.Abs(c) > AbsMax(_A, _B) ? 2 : AbsMaxIndex(_A, _B); }

        public static int MinIndex(int _A, int _B, int _C) { return _A < _B && _A < _C ? 0 : (_B < _C ? 1 : 2); }

        public static int MaxIndex(int _A, int _B, int _C) { return _A > _B && _A > _C ? 0 : (_B > _C ? 1 : 2); }
    }
}
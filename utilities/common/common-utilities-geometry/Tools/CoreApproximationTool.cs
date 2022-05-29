/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Runtime.CompilerServices;

namespace CommonUtilities.Geometry.Tools
{
    public static class CoreApproximationTool
    {
        public static readonly double MatrixEpsilon = Math.Pow(2.0, -52.0);
        public static readonly float FloatEpsilon = 4.76837158203125E-7f;
        public static readonly double DoubleEpsilon = 8.8817841970012523233891E-16;
        public static readonly double ErrorTolerance = 1.0E-10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(this double _X) { return IsZero(_X, ErrorTolerance); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(this double _X, double _Tolerance) { return _X < 0 ? _X >= -_Tolerance : _X <= _Tolerance; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRelEqual(this double _X, double _Y) { return IsRelEqual(_X, _Y, ErrorTolerance); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRelEqual(this double _X, double _Y, double _Tolerance)
        {
            if (_X == _Y)
                return true;

            var Error = _X - _Y;
            if (Error < 0)
                Error = -Error;

            if (_X == 0 || _Y == 0 || Error <= DoubleEpsilon)
                return Error <= _Tolerance;

            if (_X < 0)
                _X = -_X;
            if (_Y < 0)
                _Y = -_Y;

            var average = (_X + _Y) / 2;
            Error /= average;
            return Error <= _Tolerance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqual(this double _X, double _Y) { return IsZero(_X - _Y); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqual(this double _X, double _Y, double _Tolerance) { return IsZero(_X - _Y, _Tolerance); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLessOrEqual(this double _Small, double _Large) { return _Small < _Large || IsRelEqual(_Small, _Large); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGreaterOrEqual(this double _Large, double _Small) { return _Large > _Small || IsRelEqual(_Small, _Large); }
    }
}
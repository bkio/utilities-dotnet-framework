/// Copyright 2022- Burak Kara, All rights reserved.

using System;

namespace CommonUtilities.Geometry.Tools
{
    public static class GeometryApproximationTool
    {
        public static double VectorTolerance = 1.0E-04;
        public static double DotTolerance = 1.0E-10;
        public static double LengthTolerance = 1.0E-10;
        public static double SquareLengthTolerance = LengthTolerance * LengthTolerance;

        public static bool IsVectorNull(double _A) { return Math.Abs(_A) <= VectorTolerance; }
        public static bool IsDotNull(double _A) { return Math.Abs(_A) <= DotTolerance; }
        public static bool IsLengthNull(double _A) { return _A <= LengthTolerance; }
        public static bool IsSquareLengthNull(double _A) { return _A <= LengthTolerance * LengthTolerance; }

        public static bool IsVectorEqual(double _A, double _B) { return Math.Abs(_A - _B) <= VectorTolerance; }
        public static bool IsDotEqual(double _A, double _B) { return Math.Abs(_A - _B) <= DotTolerance; }
        public static bool IsLengthEqual(double _A, double _B) { return Math.Abs(_A - _B) <= LengthTolerance; }
        public static bool IsSquareLengthEqual(double _A, double _B) { return Math.Abs(_A - _B) <= SquareLengthTolerance; }

    }
}
/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using CommonUtilities.Geometry.Numerics;

namespace CommonUtilities.Geometry.Tools
{
    public class GeometryTool
    {
        public static bool IsCCW(Vector3 _A, Vector3 _B, Vector3 _C) { return Cross2(_A, _B, _C) >= 0; }

        public static double Dot3(Vector3 _A, Vector3 _B, Vector3 _C) { return (_B.X - _A.X) * (_C.X - _A.X) + (_B.Y - _A.Y) * (_C.Y - _A.Y) + (_B.Z - _A.Z) * (_C.Z - _A.Z); }

        public static double Cross2(Vector3 _A, Vector3 _B, Vector3 _C) { return (_B.X - _A.X) * (_C.Y - _A.Y) - (_B.Y - _A.Y) * (_C.X - _A.X); }
        
        public static Vector3 Normal(Vector3 _A, Vector3 _B, Vector3 _C) { return Cross3(_A, _B, _C).Normalized; }

        public static Vector3 Cross3(Vector3 _A, Vector3 _B, Vector3 _C)
        {
            var BaX = _B.X - _A.X;
            var BaY = _B.Y - _A.Y; 
            var BaZ = _B.Z - _A.Z;  
            var CaX = _C.X - _A.X;  
            var CaY = _C.Y - _A.Y;
            var CaZ = _C.Z - _A.Z;
            var X = BaY * CaZ - BaZ * CaY;
            var Y = BaZ * CaX - BaX * CaZ;
            var Z = BaX * CaY - BaY * CaX;
            return new Vector3(X, Y, Z);
        }

        public static double CrossLengthSquared(Vector3 _A, Vector3 _B, Vector3 _C)
        {
            var BaX = _B.X - _A.X;
            var BaY = _B.Y - _A.Y;   
            var BaZ = _B.Z - _A.Z; 
            var CaX = _C.X - _A.X; 
            var CaY = _C.Y - _A.Y;
            var CaZ = _C.Z - _A.Z;
            var X = BaY * CaZ - BaZ * CaY;
            var Y = BaZ * CaX - BaX * CaZ;
            var Z = BaX * CaY - BaY * CaX;
            return X * X + Y * Y + Z * Z;
        }

        public static double CrossLength(Vector3 _A, Vector3 _B, Vector3 _C)
        {
            return Math.Sqrt(CrossLengthSquared(_A, _B, _C));
        }
    }
}
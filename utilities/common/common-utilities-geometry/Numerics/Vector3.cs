/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using CommonUtilities.Geometry.Tools;
using CommonUtilities.Geometry.Utilities;

namespace CommonUtilities.Geometry.Numerics
{
    [JsonObject(MemberSerialization.OptIn)]
    public partial struct Vector3 : IComparable<Vector3>
    {
        public static readonly Vector3 Zero = new Vector3(0);
        public static readonly Vector3 Unit = new Vector3(1);
        public static readonly Vector3 NaN = new Vector3(double.NaN, double.NaN, double.NaN);
        public static readonly Vector3 MinValue = new Vector3(double.MinValue, double.MinValue, double.MinValue);
        public static readonly Vector3 MaxValue = new Vector3(double.MaxValue, double.MaxValue, double.MaxValue);

        public static readonly Vector3 XAxis = new Vector3(1, 0, 0);
        public static readonly Vector3 YAxis = new Vector3(0, 1, 0);
        public static readonly Vector3 ZAxis = new Vector3(0, 0, 1);

        public static readonly Vector3 North = new Vector3(0, 1, 0);
        public static readonly Vector3 South = new Vector3(0, -1, 0);
        public static readonly Vector3 East = new Vector3(1, 0, 0);
        public static readonly Vector3 West = new Vector3(-1, 0, 0);
        public static readonly Vector3 Up = new Vector3(0, 0, 1);
        public static readonly Vector3 Down = new Vector3(0, 0, -1);

        public static readonly Vector3 NorthEast = new Vector3(1, 1, 0);
        public static readonly Vector3 SouthEast = new Vector3(1, -1, 0);
        public static readonly Vector3 NorthWest = new Vector3(-1, 1, 0);
        public static readonly Vector3 SouthWest = new Vector3(-1, -1, 0);

        public const string X_PROPERTY = "x";
        public const string Y_PROPERTY = "y";
        public const string Z_PROPERTY = "z";

        [JsonProperty(X_PROPERTY)]
        public double X;
        [JsonProperty(Y_PROPERTY)]
        public double Y;
        [JsonProperty(Z_PROPERTY)]
        public double Z;

        public Vector3(double _XYZ) { X = Y = Z = _XYZ; }
        public Vector3(double _X, double _Y, double _Z) { X = _X; Y = _Y; Z = _Z; }
        public Vector3(Vector3 _From, Vector3 _To) { X = _To.X - _From.X; Y = _To.Y - _From.Y; Z = _To.Z - _From.Z; }

        public int AbsMinDim => MathTool.AbsMinIndex(X, Y, Z);
        public int AbsMaxDim => MathTool.AbsMaxIndex(X, Y, Z);

        public double Area => X * Y;
        public double Volume => X * Y * Z;

        public double Length => Math.Sqrt(LengthSquared);
        public double LengthSquared => X * X + Y * Y + Z * Z;

        public Vector3 Normalized { get { var V = this; V.Normalize(); return V; } }

        public Index3 Floor => new Index3(MathTool.IntByFloor(X), MathTool.IntByFloor(Y), MathTool.IntByFloor(Z));
        public Index3 Ceiling => new Index3(MathTool.IntByCeiling(X), MathTool.IntByCeiling(Y), MathTool.IntByCeiling(Z));
        

        public bool IsNaN => double.IsNaN(X) || double.IsNaN(Y) || double.IsNaN(Z);
        public bool IsZero => this == Zero;
        public bool IsAlmostNull => GeometryApproximationTool.IsVectorNull(X) && GeometryApproximationTool.IsVectorNull(Y) && GeometryApproximationTool.IsVectorNull(Z);

        public string ToString(int decimals) => $"({MathTool.ToString(X, decimals)}, {MathTool.ToString(Y, decimals)}, {MathTool.ToString(Z, decimals)})";
        public override string ToString() => $"(X={X:0.000000}, Y={Y:0.000000}, Z={Z:0.000000})";
        public override bool Equals(object _Object) => _Object is Vector3 _Vector3 && this == _Vector3;
        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();

        public int CompareTo(Vector3 _Other)
        {
            var Result = X.CompareTo(_Other.X);
            if (Result != 0)
                return Result;
            Result = Y.CompareTo(_Other.Y);
            return Result != 0 ? Result : Z.CompareTo(_Other.Z);
        }

        public bool IsEqual(Vector3 _A) => GeometryApproximationTool.IsVectorEqual(X, _A.X) && GeometryApproximationTool.IsVectorEqual(Y, _A.Y) && GeometryApproximationTool.IsVectorEqual(Z, _A.Z);
        public bool IsEqual(Vector3 _A, double _Tolerance) => X.IsEqual(_A.X, _Tolerance) && Y.IsEqual(_A.Y, _Tolerance) && Z.IsEqual(_A.Z, _Tolerance);

        public bool IsInSameHemisphere(Vector3 _Other) => Dot(_Other) > 0;

        public double Dot(Vector3 _A) => X * _A.X + Y * _A.Y + Z * _A.Z;

        public Vector3 Cross(Vector3 _A) => new Vector3(Y * _A.Z - Z * _A.Y, Z * _A.X - X * _A.Z, X * _A.Y - Y * _A.X);

        public Vector3 Normal(Vector3 _A) { return Cross(_A).Normalized; }

        public Vector3 GetProjected(Vector3 _Vector)
        {
            var DotV = Dot(_Vector);
            var Result = this;
            Result.Mul(DotV);
            return Result;
        }

        public void Add(Vector3 _A) { X += _A.X; Y += _A.Y; Z += _A.Z; }
        public void Add(double _A) { X += _A; Y += _A; Z += _A; }

        public void Sub(Vector3 _A) { X -= _A.X; Y -= _A.Y; Z -= _A.Z; }
        public void Sub(double _A) { X -= _A; Y -= _A; Z -= _A; }

        public void Mul(Vector3 _A) { X *= _A.X; Y *= _A.Y; Z *= _A.Z; }
        public void Mul(double _A) { X *= _A; Y *= _A; Z *= _A; }

        public void Div(Vector3 _A) { X /= _A.X; Y /= _A.Y; Z /= _A.Z; }
        public void Div(double _A) { _A = 1.0 / _A; X *= _A; Y *= _A; Z *= _A; }

        public void Negate() { X = -X; Y = -Y; Z = -Z; }
        public void ForceUp() { if (Z < 0) Negate(); }

        public bool Normalize()
        {
            var LenSquared = LengthSquared;
            if (GeometryApproximationTool.IsSquareLengthNull(LenSquared))
                return false;

            Div(Math.Sqrt(LenSquared));
            return true;
        }

        public void Derotate(Angle _Angle)
        {
            var XVal = X;
            X = XVal * _Angle.Cos + Y * _Angle.Sin;
            Y = -XVal * _Angle.Sin + Y * _Angle.Cos;
        }

        public void Rotate(Angle _Angle, double _X, double _Y)
        {
            var Dx = X - _X;
            var Dy = Y - _Y;
            X = Dx * _Angle.Cos - Dy * _Angle.Sin + _X;
            Y = Dx * _Angle.Sin + Dy * _Angle.Cos + _Y;
        }

        public void Rotate(Angle _Angle, Vector3 _Origin) { Rotate(_Angle, _Origin.X, _Origin.Y); }

        public static bool operator ==(Vector3 _A, Vector3 _B) => _A.X == _B.X && _A.Y == _B.Y && _A.Z == _B.Z;
        public static bool operator !=(Vector3 _A, Vector3 _B) => _A.X != _B.X || _A.Y != _B.Y || _A.Z != _B.Z;

        public static bool operator >(Vector3 _A, double _B) => _A.X > _B && _A.Y > _B && _A.Z > _B;
        public static bool operator >=(Vector3 _A, double _B) => _A.X >= _B && _A.Y >= _B && _A.Z >= _B;
        public static bool operator <(Vector3 _A, double _B) => _A.X < _B && _A.Y < _B && _A.Z < _B;
        public static bool operator <=(Vector3 _A, double _B) => _A.X <= _B && _A.Y <= _B && _A.Z <= _B;

        public static Vector3 operator +(Vector3 _A, Vector3 _B) => new Vector3(_A.X + _B.X, _A.Y + _B.Y, _A.Z + _B.Z);
        public static Vector3 operator -(Vector3 _A, Vector3 _B) => new Vector3(_A.X - _B.X, _A.Y - _B.Y, _A.Z - _B.Z);
        public static Vector3 operator -(Vector3 _A) => new Vector3(-_A.X, -_A.Y, -_A.Z);
        public static Vector3 operator *(Vector3 _A, double _B) => new Vector3(_A.X * _B, _A.Y * _B, _A.Z * _B);
        public static Vector3 operator *(Vector3 _A, Vector3 _B) => new Vector3(_A.X * _B.X, _A.Y * _B.Y, _A.Z * _B.Z);
        public static Vector3 operator *(Vector3 _A, Index3 _B) => new Vector3(_A.X * _B.I, _A.Y * _B.J, _A.Z * _B.K);
        public static Vector3 operator /(Vector3 _A, double _B) => new Vector3(_A.X / _B, _A.Y / _B, _A.Z / _B);
        public static Vector3 operator /(Vector3 _A, Vector3 _B) => new Vector3(_A.X / _B.X, _A.Y / _B.Y, _A.Z / _B.Z);
        public static Vector3 operator /(Vector3 _A, Index3 _B) => new Vector3(_A.X / _B.I, _A.Y / _B.J, _A.Z / _B.K);

        public double this[int _Dimension]
        {
            get
            {
                return _Dimension switch
                {
                    0 => X,
                    1 => Y,
                    2 => Z,
                    _ => throw new IndexOutOfRangeException(),
                };
            }
            set
            {
                switch (_Dimension)
                {
                    case 0:
                        X = value;
                        break;
                    case 1:
                        Y = value;
                        break;
                    case 2:
                        Z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public static int CompareX(Vector3 _A, Vector3 _B) => _A.X.CompareTo(_B.X);

        public static int CompareY(Vector3 _A, Vector3 _B) => _A.Y.CompareTo(_B.Y);

        public static int CompareZ(Vector3 _A, Vector3 _B) => _A.Z.CompareTo(_B.Z);

        public static int CompareXY(Vector3 _A, Vector3 _B)
        {
            var R = _A.X.CompareTo(_B.X);
            return R != 0 ? R : _A.Y.CompareTo(_B.Y);
        }

        public static int CompareYX(Vector3 _A, Vector3 _B)
        {
            var R = _A.Y.CompareTo(_B.Y);
            return R != 0 ? R : _A.X.CompareTo(_B.X);
        }

        public static int CompareXYZ(Vector3 _A, Vector3 _B) => _A.CompareTo(_B);

        public static int CompareYXZ(Vector3 _A, Vector3 _B)
        {
            var Result = _A.Y.CompareTo(_B.Y);
            if (Result != 0)
                return Result;

            Result = _A.X.CompareTo(_B.X);
            if (Result != 0)
                return Result;

            return _A.Z.CompareTo(_B.Z);
        }

        public static int CompareZXY(Vector3 _A, Vector3 _B)
        {
            var Result = _A.Z.CompareTo(_B.Z);
            if (Result != 0)
                return Result;

            Result = _A.X.CompareTo(_B.X);
            if (Result != 0)
                return Result;

            return _A.Y.CompareTo(_B.Y);
        }

        public class XComparer : IComparer<Vector3> { public int Compare(Vector3 _A, Vector3 _B) => CompareX(_A, _B); }
        public class YComparer : IComparer<Vector3> { public int Compare(Vector3 _A, Vector3 _B) => CompareY(_A, _B); }
        public class ZComparer : IComparer<Vector3> { public int Compare(Vector3 _A, Vector3 _B) => CompareZ(_A, _B); }

        public class XYComparer : IComparer<Vector3> { public int Compare(Vector3 _A, Vector3 _B) => CompareXY(_A, _B); }
        public class YXComparer : IComparer<Vector3> { public int Compare(Vector3 _A, Vector3 _B) => CompareYX(_A, _B); }

        public class XYZComparer : IComparer<Vector3> { public int Compare(Vector3 _A, Vector3 _B) => _A.CompareTo(_B); }
        public class YXZComparer : IComparer<Vector3> { public int Compare(Vector3 _A, Vector3 _B) => CompareYXZ(_A, _B); }
        public class ZXYComparer : IComparer<Vector3> { public int Compare(Vector3 _A, Vector3 _B) => CompareZXY(_A, _B); }

        public class Comparer : IComparer<Vector3>
        {
            private readonly int Dimension;
            public Comparer(int _Dim) { Dimension = _Dim; }
            public int Compare(Vector3 _A, Vector3 _B)
            {
                return _A[Dimension].CompareTo(_B[Dimension]);
            }
        }

        public static Vector3 FromBytes(byte[] _Bytes, ref int _Head, bool _bSinglePrecision)
        {
            var Result = new Vector3();

            if (_bSinglePrecision)
            {
                ByteTools.BytesToValue(out float XTmp, _Bytes, ref _Head);
                ByteTools.BytesToValue(out float YTmp, _Bytes, ref _Head);
                ByteTools.BytesToValue(out float ZTmp, _Bytes, ref _Head);

                Result.X = XTmp;
                Result.Y = YTmp;
                Result.Z = ZTmp;
            }
            else
            {
                ByteTools.BytesToValue(out Result.X, _Bytes, ref _Head);
                ByteTools.BytesToValue(out Result.Y, _Bytes, ref _Head);
                ByteTools.BytesToValue(out Result.Z, _Bytes, ref _Head);
            }

            return Result;
        }
    }
}

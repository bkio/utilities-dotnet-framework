/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using Newtonsoft.Json;
using CommonUtilities.Geometry.Tools;
using CommonUtilities.Geometry.Triangularization;

namespace CommonUtilities.Geometry.Numerics
{
    public partial struct Matrix3x4 : IEquatable<Matrix3x4>, ITransformer3, IMatrix
    {
        public static readonly Matrix3x4 Zero = new Matrix3x4(Vector3.Zero);
        public static readonly Matrix3x4 Identity = new Matrix3x4(Vector3.Unit);

        public static readonly double GimbalLockCheck = 1 - 1e-10;

        public const string M11_PROPERTY = "m11";
        public const string M12_PROPERTY = "m12";
        public const string M13_PROPERTY = "m13";
        public const string M21_PROPERTY = "m21";
        public const string M22_PROPERTY = "m22";
        public const string M23_PROPERTY = "m23";
        public const string M31_PROPERTY = "m31";
        public const string M32_PROPERTY = "m32";
        public const string M33_PROPERTY = "m33";
        public const string X_PROPERTY = "x";
        public const string Y_PROPERTY = "y";
        public const string Z_PROPERTY = "z";

        [JsonProperty(M11_PROPERTY)]
        public double M11;
        [JsonProperty(M12_PROPERTY)]
        public double M12;
        [JsonProperty(M13_PROPERTY)]
        public double M13;
        [JsonProperty(M21_PROPERTY)]
        public double M21;
        [JsonProperty(M22_PROPERTY)]
        public double M22;
        [JsonProperty(M23_PROPERTY)]
        public double M23;
        [JsonProperty(M31_PROPERTY)]
        public double M31;
        [JsonProperty(M32_PROPERTY)]
        public double M32;
        [JsonProperty(M33_PROPERTY)]
        public double M33;
        [JsonProperty(X_PROPERTY)]
        public double X;
        [JsonProperty(Y_PROPERTY)]
        public double Y;
        [JsonProperty(Z_PROPERTY)]
        public double Z;

        public double Determinant => M11 * M22 * M33 + M12 * M23 * M31 + M13 * M21 * M32 - M13 * M22 * M31 - M11 * M23 * M32 - M12 * M21 * M33;
        public double Trace => M11 + M22 + M33;

        private double Row1Length => FunctionsTool.Length(M11, M21, M31);
        private double Row2Length => FunctionsTool.Length(M12, M22, M32);
        private double Row3Length => FunctionsTool.Length(M13, M23, M33);

        private double ScaleX => Row1Length;
        private double ScaleY => Row2Length;
        private double ScaleZ => Row3Length;

        public Vector3 Translation => new Vector3(X, Y, Z);
        public Vector3 Scale => new Vector3(ScaleX, ScaleY, ScaleZ);

        public double ZRotation
        {
            get
            {
                var LocalM31 = M31 / Row1Length;
                // Special case: Gimbal lock, pitch = 90 deg
                if (Math.Abs(LocalM31) > GimbalLockCheck)
                {
                    var LocalM23 = M23 / Row3Length;
                    var LocalM22 = M22 / Row2Length;
                    return Math.Atan2(-LocalM23, LocalM22) * Math.Sign(LocalM31);
                }
                return Math.Atan2(M21, M11);
            }

        }

        public Vector3 Rotation
        {
            get
            {
                var LocalM31 = M31 / Row1Length;
                var LocalM32 = M32 / Row2Length;
                var LocalM33 = M33 / Row3Length;

                // Special case: Gimbal lock, pitch = 90 deg
                if (Math.Abs(LocalM31) > GimbalLockCheck)
                    return new Vector3(0, -0.5 * Math.PI * Math.Sign(LocalM31), ZRotation);

                return new Vector3(
                  Math.Atan2(LocalM32, LocalM33),
                  Math.Atan2(-LocalM31, Math.Sqrt(LocalM32 * LocalM32 + LocalM33 * LocalM33)),
                  ZRotation);
            }
        }

        public Matrix3x4(
          double _M11, double _M12, double _M13,
          double _M21, double _M22, double _M23,
          double _M31, double _M32, double _M33
          )
        {
            M11 = _M11;
            M12 = _M12;
            M13 = _M13;
            M21 = _M21;
            M22 = _M22;
            M23 = _M23;
            M31 = _M31;
            M32 = _M32;
            M33 = _M33;
            X = 0;
            Y = 0;
            Z = 0;
        }

        public Matrix3x4(
          double _M11, double _M12, double _M13, double _X,
          double _M21, double _M22, double _M23, double _Y,
          double _M31, double _M32, double _M33, double _Z
          )
        {
            M11 = _M11;
            M12 = _M12;
            M13 = _M13;
            M21 = _M21;
            M22 = _M22;
            M23 = _M23;
            M31 = _M31;
            M32 = _M32;
            M33 = _M33;
            X = _X;
            Y = _Y;
            Z = _Z;
        }

        private Matrix3x4(Vector3 _Scale)
        {
            M11 = _Scale.X;
            M12 = 0;
            M13 = 0;
            M21 = 0;
            M22 = _Scale.Y;
            M23 = 0;
            M31 = 0;
            M32 = 0;
            M33 = _Scale.Z;
            X = 0;
            Y = 0;
            Z = 0;
        }

        public int NumRows => 3;
        public int NumCols => 4;
        public double GetAt(int _Row, int _Col) { return this[_Row, _Col]; }
        public bool IsSymmetric => M12 == M21 && M13 == M31 && M23 == M32;

        public override int GetHashCode()
        {
            return
              M11.GetHashCode() ^ M12.GetHashCode() ^ M13.GetHashCode() ^
              M21.GetHashCode() ^ M22.GetHashCode() ^ M23.GetHashCode() ^
              M31.GetHashCode() ^ M32.GetHashCode() ^ M33.GetHashCode() ^
              X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        public override bool Equals(object _Object)
        {
            return _Object is Matrix3x4 OtherMatrix && Equals(OtherMatrix);
        }

        public override string ToString()
        {
            return $"Matrix: (3x4)\n{Matrix.GetString(M11, M12, M13, X)}{Matrix.GetString(M21, M22, M23, Y)}{Matrix.GetString(M31, M32, M33, Z)}";
        }

        public bool Equals(Matrix3x4 _Other)
        {
            if (!M11.IsEqual(_Other.M11))
                return false;
            if (!M12.IsEqual(_Other.M12))
                return false;
            if (!M13.IsEqual(_Other.M13))
                return false;
            if (!M21.IsEqual(_Other.M21))
                return false;
            if (!M22.IsEqual(_Other.M22))
                return false;
            if (!M23.IsEqual(_Other.M23))
                return false;
            if (!M31.IsEqual(_Other.M31))
                return false;
            if (!M32.IsEqual(_Other.M32))
                return false;
            if (!M33.IsEqual(_Other.M33))
                return false;
            if (!X.IsEqual(_Other.X, 0.001))
                return false;
            if (!Y.IsEqual(_Other.Y, 0.001))
                return false;
            if (!Z.IsEqual(_Other.Z, 0.001))
                return false;
            return true;
        }

        public bool IsZero => M11 == 0 && M12 == 0 && M13 == 0 && X == 0 &&
                              M21 == 0 && M22 == 0 && M23 == 0 && Y == 0 &&
                              M31 == 0 && M32 == 0 && M33 == 0 && Z == 0;


        public bool IsIdentity => M11 == 1 && M12 == 0 && M13 == 0 && X == 0 &&
                                  M21 == 0 && M22 == 1 && M23 == 0 && Y == 0 &&
                                  M31 == 0 && M32 == 0 && M33 == 1 && Z == 0;

        public void Add(Matrix3x4 _Other)
        {
            M11 += _Other.M11;
            M12 += _Other.M12;
            M13 += _Other.M13;

            M21 += _Other.M21;
            M22 += _Other.M22;
            M23 += _Other.M23;

            M31 += _Other.M31;
            M32 += _Other.M32;
            M33 += _Other.M33;

            X += _Other.X;
            Y += _Other.Y;
            Z += _Other.Z;
        } 

        public void Sub(Matrix3x4 _Other)
        {
            M11 -= _Other.M11;
            M12 -= _Other.M12;
            M13 -= _Other.M13;

            M21 -= _Other.M21;
            M22 -= _Other.M22;
            M23 -= _Other.M23;

            M31 -= _Other.M31;
            M32 -= _Other.M32;
            M33 -= _Other.M33;

            X -= _Other.X;
            Y -= _Other.Y;
            Z -= _Other.Z;
        }

        public void Multiply(Matrix3x4 _Other)
        {
            MultiplyTranslate(_Other.X, _Other.Y, _Other.Z);
            Multiply3x3(_Other);
        }

        public void Multiply3x3(Matrix3x4 _Other)
        {
            var m11 = M11 * _Other.M11 + M12 * _Other.M21 + M13 * _Other.M31;
            var m12 = M11 * _Other.M12 + M12 * _Other.M22 + M13 * _Other.M32;
            var m13 = M11 * _Other.M13 + M12 * _Other.M23 + M13 * _Other.M33;

            var m21 = M21 * _Other.M11 + M22 * _Other.M21 + M23 * _Other.M31;
            var m22 = M21 * _Other.M12 + M22 * _Other.M22 + M23 * _Other.M32;
            var m23 = M21 * _Other.M13 + M22 * _Other.M23 + M23 * _Other.M33;

            var m31 = M31 * _Other.M11 + M32 * _Other.M21 + M33 * _Other.M31;
            var m32 = M31 * _Other.M12 + M32 * _Other.M22 + M33 * _Other.M32;
            var m33 = M31 * _Other.M13 + M32 * _Other.M23 + M33 * _Other.M33;

            M11 = m11;
            M12 = m12;
            M13 = m13;
            M21 = m21;
            M22 = m22;
            M23 = m23;
            M31 = m31;
            M32 = m32;
            M33 = m33;
        }

        public void Multiply(double _Other)
        {
            M11 *= _Other;
            M12 *= _Other;
            M13 *= _Other;
            M21 *= _Other;
            M22 *= _Other;
            M23 *= _Other;
            M31 *= _Other;
            M32 *= _Other;
            M33 *= _Other;
        }

        public void Transform(ref Vector3 _Point, bool _AsVector = false)
        {
            // Same as Matrix * point
            var X = _Point.X;
            var Y = _Point.Y;
            var Z = _Point.Z;
            _Point.X = M11 * X + M12 * Y + M13 * Z;
            _Point.Y = M21 * X + M22 * Y + M23 * Z;
            _Point.Z = M31 * X + M32 * Y + M33 * Z;
            if (!_AsVector)
            {
                _Point.X += X;
                _Point.Y += Y;
                _Point.Z += Z;
            }
        }

        public Vector3 GetTransformed(Vector3 _Point, bool _AsVector = false)
        {
            Transform(ref _Point, _AsVector);
            return _Point;
        }

        public Quaternion GetQuaternion()
        {
            var TraceV = Trace;
            if (TraceV > CoreApproximationTool.MatrixEpsilon)
            {
                var S = 0.5 / Math.Sqrt(TraceV + 1.0);
                return new Quaternion
                {
                    W = 0.25 / S,
                    V = {
                        X = (M32 - M23) * S,
                        Y = (M13 - M31) * S,
                        Z = (M21 - M12) * S
                    }
                };
            }
            if (M11 > M22 && M11 > M33)
            {
                var S = 2 * Math.Sqrt(1 + M11 - M22 - M33);
                return new Quaternion
                {
                    W = (M32 - M23) / S,
                    V = {
                        X = 0.25 * S,
                        Y = (M12 + M21) / S,
                        Z = (M13 + M31) / S
                    }
                };
            }

            if (M22 > M33)
            {
                var S = 2 * Math.Sqrt(1 + M22 - M11 - M33);
                return new Quaternion
                {
                    W = (M13 - M31) / S,
                    V = {
                        X = (M12 + M21) / S,
                        Y = 0.25 * S,
                        Z = (M23 + M32) / S
                    }
                };
            }
            else
            {
                var S = 2 * Math.Sqrt(1 + M33 - M11 - M22);
                return new Quaternion
                {
                    W = (M21 - M12) / S,
                    V = {
                        X = (M13 + M31) / S,
                        Y = (M23 + M32) / S,
                        Z = 0.25 * S
                    }
                };
            }
        }

        public void Decompose(out Vector3 _Translation, out Quaternion _Rotation, out Vector3 _Scale)
        {
            _Translation = Translation;
            _Scale = Scale;
            _Rotation = GetQuaternion();
        }

        public void MultiplyTranslate(double _X, double _Y, double _Z)
        {
            X += M11 * _X + M12 * _Y + M13 * _Z;
            Y += M21 * _X + M22 * _Y + M23 * _Z;
            Z += M31 * _X + M32 * _Y + M33 * _Z;
        }

        public static bool operator ==(Matrix3x4 _A, Matrix3x4 _B) { return _A.Equals(_B); }
        public static bool operator !=(Matrix3x4 _A, Matrix3x4 _B) { return !_A.Equals(_B); }
        public static Matrix3x4 operator +(Matrix3x4 _A, Matrix3x4 _B) { var result = _A; result.Add(_B); return result; }
        public static Matrix3x4 operator -(Matrix3x4 _A, Matrix3x4 _B) { var result = _A; result.Sub(_B); return result; }
        public static Matrix3x4 operator *(Matrix3x4 _A, Matrix3x4 _B) { var result = _A; result.Multiply(_B); return result; }
        public static Vector3 operator *(Matrix3x4 _A, Vector3 _B) { return _A.GetTransformed(_B); }

        public double this[int _Row, int _Col]
        {
            get
            {
                switch (_Row)
                {
                    case 0:
                        switch (_Col) { case 0: return M11; case 1: return M12; case 2: return M13; case 3: return X; }
                        break;
                    case 1:
                        switch (_Col) { case 0: return M21; case 1: return M22; case 2: return M23; case 3: return Y; }
                        break;
                    case 2:
                        switch (_Col) { case 0: return M31; case 1: return M32; case 2: return M33; case 3: return Z; }
                        break;
                }
                throw new IndexOutOfRangeException();
            }
            set
            {
                switch (_Row)
                {
                    case 0:
                        switch (_Col) { case 0: M11 = value; return; case 1: M12 = value; return; case 2: M13 = value; return; case 3: X = value; return; }
                        break;
                    case 1:
                        switch (_Col) { case 0: M21 = value; return; case 1: M22 = value; return; case 2: M23 = value; return; case 3: Y = value; return; }
                        break;
                    case 2:
                        switch (_Col) { case 0: M31 = value; return; case 1: M32 = value; return; case 2: M33 = value; return; case 3: Z = value; return; }
                        break;
                }
                throw new IndexOutOfRangeException();
            }
        }
    }
}
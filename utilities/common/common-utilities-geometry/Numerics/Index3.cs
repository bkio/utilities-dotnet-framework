/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using CommonUtilities.Geometry.Tools;

namespace CommonUtilities.Geometry.Numerics
{
    [JsonObject(MemberSerialization.OptIn)]
    public struct Index3 : IComparable<Index3>
    {
        public const string I_PROPERTY = "i";
        public const string J_PROPERTY = "j";
        public const string K_PROPERTY = "k";

        [JsonProperty(I_PROPERTY)]
        public int I;
        [JsonProperty(J_PROPERTY)]
        public int J;
        [JsonProperty(K_PROPERTY)]
        public int K;

        public Index3(int _IJK) { I = J = K = _IJK; }
        public Index3(int _I, int _J, int _K) { I = _I; J = _J; K = _K; }

        public static readonly Index3 NaN = new Index3(int.MinValue);
        public static readonly Index3 Zero = new Index3(0);
        public static readonly Index3 Unit = new Index3(1);

        public static readonly Index3 XAxis = new Index3(1, 0, 0);
        public static readonly Index3 YAxis = new Index3(0, 1, 0);
        public static readonly Index3 ZAxis = new Index3(0, 0, 1);

        public static readonly Index3 North = new Index3(0, 1, 0);
        public static readonly Index3 South = new Index3(0, -1, 0);
        public static readonly Index3 East = new Index3(1, 0, 0);
        public static readonly Index3 West = new Index3(-1, 0, 0);
        public static readonly Index3 Up = new Index3(0, 0, 1);
        public static readonly Index3 Down = new Index3(0, 0, -1);

        public static readonly Index3 NorthEast = new Index3(1, 1, 0);
        public static readonly Index3 SouthEast = new Index3(-1, 1, 0);
        public static readonly Index3 NorthWest = new Index3(1, -1, 0);
        public static readonly Index3 SouthWest = new Index3(-1, -1, 0);

        public int MinCoord => MathTool.Min(I, J, K);
        public int MaxCoord => MathTool.Max(I, J, K);

        public int MinDim => MathTool.MinIndex(I, J, K);
        public int MaxDim => MathTool.MaxIndex(I, J, K);

        public int Size => I * J * K;

        public override string ToString() { return $"(I={I}, J={J}, K={K})"; }
        public override bool Equals(object _Obj) { return _Obj is Index3 && this == (Index3)_Obj; }
        public override int GetHashCode() { return I.GetHashCode() ^ J.GetHashCode() ^ K.GetHashCode(); }

        public int CompareTo(Index3 _Other)
        {
            var Result = I.CompareTo(_Other.I);
            if (Result != 0)
                return Result;
            Result = J.CompareTo(_Other.J);
            if (Result != 0)
                return Result;
            return K.CompareTo(_Other.K);
        }

        public void SetMax(Index3 _A) { if (_A.I > I) I = _A.I; if (_A.J > J) J = _A.J; if (_A.K > K) K = _A.K; }

        public void Add(int _A) { I += _A; J += _A; K += _A; }

        public void Sub(int _A) { I -= _A; J -= _A; K -= _A; }

        public void Mul(int _A) { I *= _A; J *= _A; K *= _A; }

        public void Div(int _A) { I /= _A; J /= _A; K /= _A; }

        public static explicit operator Vector3(Index3 _V) { return new Vector3(_V.I, _V.J, _V.K); }
        public static bool operator ==(Index3 _A, Index3 _B) { return _A.I == _B.I && _A.J == _B.J && _A.K == _B.K; }
        public static bool operator !=(Index3 _A, Index3 _B) { return _A.I != _B.I || _A.J != _B.J || _A.K != _B.K; }
        public static Index3 operator +(Index3 _A, Index3 _B) { return new Index3(_A.I + _B.I, _A.J + _B.J, _A.K + _B.K); }
        public static Index3 operator -(Index3 _A, Index3 _B) { return new Index3(_A.I - _B.I, _A.J - _B.J, _A.K - _B.K); }
        public static Index3 operator -(Index3 _A) { return new Index3(-_A.I, -_A.J, -_A.K); }
        public static Index3 operator *(Index3 _A, int _B) { return new Index3(_A.I * _B, _A.J * _B, _A.K * _B); }
        public static Index3 operator *(Index3 _A, Index3 _B) { return new Index3(_A.I * _B.I, _A.J * _B.J, _A.K * _B.K); }
        public static Index3 operator /(Index3 _A, int _B) { return new Index3(_A.I / _B, _A.J / _B, _A.K / _B); }
        public static Index3 operator /(Index3 _A, Index3 _B) { return new Index3(_A.I / _B.I, _A.J / _B.J, _A.K / _B.K); }

        public int this[int _Dim]
        {
            get
            {
                switch (_Dim)
                {
                    case 0:
                        return I;
                    case 1:
                        return J;
                    case 2:
                        return K;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (_Dim)
                {
                    case 0:
                        I = value;
                        break;
                    case 1:
                        J = value;
                        break;
                    case 2:
                        K = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public static int CompareI(Index3 _A, Index3 _B) { return _A.I.CompareTo(_B.I); }
        public static int CompareJ(Index3 _A, Index3 _B) { return _A.J.CompareTo(_B.J); }
        public static int CompareK(Index3 _A, Index3 _B) { return _A.K.CompareTo(_B.K); }
        public static int CompareIJ(Index3 _A, Index3 _B) { var R = _A.I.CompareTo(_B.I); return R != 0 ? R : _A.J.CompareTo(_B.J); }
        public static int CompareJI(Index3 _A, Index3 _B) { var R = _A.J.CompareTo(_B.J); return R != 0 ? R : _A.I.CompareTo(_B.I); }
        public static int CompareIJK(Index3 _A, Index3 _B) { return _A.CompareTo(_B); }

        public static int CompareJIK(Index3 _A, Index3 _B)
        {
            var Result = _A.J.CompareTo(_B.J);
            if (Result != 0)
                return Result;
            Result = _A.I.CompareTo(_B.I);
            return Result != 0 ? Result : _A.K.CompareTo(_B.K);
        }

        public class IComparer : IComparer<Index3> { public int Compare(Index3 _A, Index3 _B) { return CompareI(_A, _B); } }
        public class JComparer : IComparer<Index3> { public int Compare(Index3 _A, Index3 _B) { return CompareJ(_A, _B); } }
        public class KComparer : IComparer<Index3> { public int Compare(Index3 _A, Index3 _B) { return CompareK(_A, _B); } }
        public class IJComparer : IComparer<Index3> { public int Compare(Index3 _A, Index3 _B) { return CompareIJ(_A, _B); } }
        public class JIComparer : IComparer<Index3> { public int Compare(Index3 _A, Index3 _B) { return CompareJI(_A, _B); } }
        public class IJKComparer : IComparer<Index3> { public int Compare(Index3 _A, Index3 _B) { return _A.CompareTo(_B); } }

        public class Comparer : IComparer<Index3>
        {
            private readonly int Dimension;
            private readonly int Value;

            public Comparer(int _Dimension, int _Value) { Dimension = _Dimension; Value = _Value; }
            public int Compare(Index3 _A, Index3 _B)
            {
                return _A[Dimension].CompareTo(Value);
            }
        }
    }
}
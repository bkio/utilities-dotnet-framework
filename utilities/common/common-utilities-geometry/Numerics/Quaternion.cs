/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using Newtonsoft.Json;
using CommonUtilities.Geometry.Tools;
using CommonUtilities.Geometry.Triangularization;
using CommonUtilities.Geometry.Utilities;

namespace CommonUtilities.Geometry.Numerics
{
    public struct Quaternion : ITransformer3
    {
        public static readonly Quaternion Zero = new Quaternion(Vector3.Zero, 0);
        public static readonly Quaternion Identity = new Quaternion(Vector3.Zero, 1);
        public static readonly Quaternion NaN = new Quaternion(Vector3.NaN, double.NaN);

        public const string V_PROPERTY = "v";
        public const string W_PROPERTY = "w";

        [JsonProperty(V_PROPERTY)]
        public Vector3 V;
        [JsonProperty(W_PROPERTY)]
        public double W;

        public Quaternion(Vector3 _V, double _W) { V = _V; W = _W; }

        public double Length => Math.Sqrt(W * W + V.LengthSquared);
        public double LengthSquared => W * W + V.LengthSquared;

        public Quaternion Normalized { get { var Q = this; Q.Normalize(); return Q; } }

        public double Dot(Quaternion _A) { return V.Dot(_A.V) + W * _A.W; }
        public double Cos(Quaternion _A) { return Dot(_A) / (Length * _A.Length); }

        public override string ToString()
        {
            return $"V: {V}, W: {W}";
        }

        public override bool Equals(object _Other)
        {
            if (_Other is Quaternion == false)
                return false;
            return this == (Quaternion)_Other;
        }

        public override int GetHashCode()
        {
            return V.GetHashCode() ^ W.GetHashCode();
        }

        public void Add(double _A) { V.Add(_A); W += _A; }
        public void Sub(double _A) { V.Sub(_A); W -= _A; }
        public void Mul(double _A) { V.Mul(_A); W *= _A; }
        public void Div(double _A) { V.Div(_A); W /= _A; }


        public void Transform(ref Vector3 _Vector, bool _AsVector)
        {
            var Tmp = V.Cross(_Vector);
            var Tmp2 = _Vector;
            Tmp2.Mul(W);
            Tmp.Add(Tmp2);
            Tmp = V.Cross(Tmp);
            Tmp.Mul(2);
            _Vector.Add(Tmp);
        }

        public bool Normalize()
        {
            var Length = LengthSquared;
            if (GeometryApproximationTool.IsSquareLengthNull(Length)) 
                return false;
            if (!GeometryApproximationTool.IsSquareLengthEqual(Length, 1))
                Div(Math.Sqrt(Length));
            return true;
        }

        public void Conjugate()
        {
            V = -V;
        }

        public static bool operator ==(Quaternion _Left, Quaternion _Right)
        {
            return _Left.Equals(_Right);
        }

        public static bool operator !=(Quaternion _Left, Quaternion _Right)
        {
            return !_Left.Equals(_Right);
        }

        public static Quaternion FromBytes(byte[] _Bytes, ref int _Head, bool _bSinglePrecision)
        {
            var Result = new Quaternion();

            if (_bSinglePrecision)
            {
                ByteTools.BytesToValue(out float XTmp, _Bytes, ref _Head);
                ByteTools.BytesToValue(out float YTmp, _Bytes, ref _Head);
                ByteTools.BytesToValue(out float ZTmp, _Bytes, ref _Head);
                ByteTools.BytesToValue(out float WTmp, _Bytes, ref _Head);

                Result.V.X = XTmp;
                Result.V.Y = YTmp;
                Result.V.Z = ZTmp;
                Result.W = WTmp;
            }
            else
            {
                ByteTools.BytesToValue(out Result.V.X, _Bytes, ref _Head);
                ByteTools.BytesToValue(out Result.V.Y, _Bytes, ref _Head);
                ByteTools.BytesToValue(out Result.V.Z, _Bytes, ref _Head);
                ByteTools.BytesToValue(out Result.W, _Bytes, ref _Head);
            }

            return Result;
        }
    }
}
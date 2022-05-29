/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CommonUtilities.Geometry.Numerics
{
    [JsonObject(MemberSerialization.OptIn)]
    public struct Range3
    {
        public const double IsRelEqualByDistanceRelTolerance = 0.035;

        public static readonly Range3 Zero = new Range3(Range1.Zero, Range1.Zero, Range1.Zero);
        public static readonly Range3 PositiveUnit = new Range3(Range1.PositiveUnit, Range1.PositiveUnit, Range1.PositiveUnit);
        public static readonly Range3 CenterUnit = new Range3(Range1.CenterUnit, Range1.CenterUnit, Range1.CenterUnit);
        public static readonly Range3 Empty = new Range3(Range1.Empty, Range1.Empty, Range1.Empty);
        public static readonly Range3 Infinity = new Range3(Range1.Infinity, Range1.Infinity, Range1.Infinity);
        public static readonly Range3 Test = new Range3(Range1.TestXY, Range1.TestXY, Range1.TestZ);
        public static readonly Range3 Test3 = new Range3(Range1.TestXY, Range1.TestXY, Range1.TestXY);

        public const string X_PROPERTY = "x";
        public const string Y_PROPERTY = "y";
        public const string Z_PROPERTY = "z";

        [JsonProperty(X_PROPERTY)]
        public Range1 X;
        [JsonProperty(Y_PROPERTY)]
        public Range1 Y;
        [JsonProperty(Z_PROPERTY)]
        public Range1 Z;

        public Range3(Vector3 _Point0, Vector3 _Point1) { X = new Range1(_Point0.X, _Point1.X); Y = new Range1(_Point0.Y, _Point1.Y); Z = new Range1(_Point0.Z, _Point1.Z); }

        public Range3(Range1 _X, Range1 _Y, Range1 _Z) { X = _X; Y = _Y; Z = _Z; }

        public bool IsSingular => X.IsSingular && Y.IsSingular && Z.IsSingular;

        public Vector3 Min => new Vector3(X.Min, Y.Min, Z.Min);
        public Vector3 Max => new Vector3(X.Max, Y.Max, Z.Max);
        public Vector3 Delta { get => new Vector3(X.Delta, Y.Delta, Z.Delta); set => SetCenterDelta(Center, value); }
        public Vector3 Center { get => new Vector3(X.Center, Y.Center, Z.Center); set => SetCenterDelta(value, Delta); }

        public Vector3 NorthEastBase => new Vector3(X.Max, Y.Max, Z.Min);
        public Vector3 SouthEastBase => new Vector3(X.Max, Y.Min, Z.Min);
        public Vector3 NorthWestBase => new Vector3(X.Min, Y.Max, Z.Min);
        public Vector3 SouthWestBase => new Vector3(X.Min, Y.Min, Z.Min);

        public Vector3 NorthEastTop => new Vector3(X.Max, Y.Max, Z.Max);
        public Vector3 SouthEastTop => new Vector3(X.Max, Y.Min, Z.Max);
        public Vector3 NorthWestTop => new Vector3(X.Min, Y.Max, Z.Max);
        public Vector3 SouthWestTop => new Vector3(X.Min, Y.Min, Z.Max);

        public override string ToString() { return $"(X={X}, Y={Y}, Z={Z})"; }
        public override bool Equals(object _Obj) { return _Obj is Range3 _Range && this == _Range; }
        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();

        public IEnumerable<Vector3> Points
        {
            get
            {
                yield return SouthWestTop;
                yield return SouthWestBase;
                yield return SouthEastBase;
                yield return SouthEastTop;
                yield return NorthEastTop;
                yield return NorthEastBase;
                yield return NorthWestBase;
                yield return NorthWestTop;
                yield return SouthWestTop;
                yield return SouthEastTop;
                yield return SouthEastBase;
                yield return NorthEastBase;
                yield return NorthEastTop;
                yield return NorthWestTop;
                yield return NorthWestBase;
                yield return SouthWestBase;
                yield return SouthWestTop;
            }
        }

        public void SetCenterDelta(Vector3 _Center, Vector3 _Delta)
        {
            X.SetCenterDelta(_Center.X, _Delta.X);
            Y.SetCenterDelta(_Center.Y, _Delta.Y);
            Z.SetCenterDelta(_Center.Z, _Delta.Z);
        }

        public void Add(Vector3 _A)
        {
            X.Add(_A.X);
            Y.Add(_A.Y);
            Z.Add(_A.Z);
        }

        public void Add(IEnumerable<Vector3> _Points)
        {
            foreach (var Point in _Points)
                Add(Point);
        }

        public void ExtendByMargin(double _Margin)
        {
            X.ExtendByMargin(_Margin);
            Y.ExtendByMargin(_Margin);
            Z.ExtendByMargin(_Margin);
        }

        public static bool operator ==(Range3 a, Range3 b) { return a.X == b.X && a.Y == b.Y && a.Z == b.Z; }
        public static bool operator !=(Range3 a, Range3 b) { return a.X != b.X || a.Y != b.Y || a.Z != b.Z; }

        public Range1 this[int _Dimension]
        {
            get
            {
                return _Dimension switch
                {
                    0 => X,
                    1 => Y,
                    2 => Z,
                    _ => throw new IndexOutOfRangeException("dimension"),
                };
            }
            set
            {
                switch (_Dimension)
                {
                    case 0: X = value; break;
                    case 1: Y = value; break;
                    case 2: Z = value; break;
                    default: throw new IndexOutOfRangeException("dimension");
                }
            }
        }

        public static Range3 Create(IEnumerable<Vector3> _List, double _Margin = 0)
        {
            var Range = Empty;
            foreach (var Point in _List)
                Range.Add(Point);
            Range.ExtendByMargin(_Margin);
            return Range;
        }
    }
}
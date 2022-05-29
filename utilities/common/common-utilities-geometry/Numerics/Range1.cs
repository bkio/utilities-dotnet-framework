/// Copyright 2022- Burak Kara, All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;
using CommonUtilities.Geometry.Tools;

namespace CommonUtilities.Geometry.Numerics
{
    [JsonObject(MemberSerialization.OptIn)]
    public struct Range1 : IValueAdder
    {
        public static readonly Range1 Zero = new Range1(0, 0);
        public static readonly Range1 PositiveUnit = new Range1(0, 1);
        public static readonly Range1 CenterUnit = new Range1(-1, 1);
        public static readonly Range1 Empty = new Range1 { Min = double.NaN, Max = double.NaN, IsEmpty = true };
        public static readonly Range1 Infinity = new Range1(double.MinValue, double.MaxValue);
        public static readonly Range1 Positive = new Range1(double.Epsilon, double.MaxValue);
        public static readonly Range1 Negative = new Range1(double.MinValue, -double.Epsilon);
        public static readonly Range1 TestXY = new Range1(0, 1000);
        public static readonly Range1 TestZ = new Range1(0, 300);
        public static readonly Range1 TestU = new Range1(2, 6);

        public const string IS_EMPTY_PROPERTY = "isEmpty";
        public const string MIN_PROPERTY = "min";
        public const string MAX_PROPERTY = "max";

        [JsonProperty(IS_EMPTY_PROPERTY)]
        public bool IsEmpty { get; private set; }
        [JsonProperty(MIN_PROPERTY)]
        public double Min;
        [JsonProperty(MAX_PROPERTY)]
        public double Max;

        public Range1(double _A, double _B)
        { 
            if (_A < _B) {
                Min = _A;
                Max = _B;
            }
            else {
                Min = _B;
                Max = _A;
            } 
            IsEmpty = false;
        }
        
        public bool IsNaN => double.IsNaN(Min) || double.IsNaN(Max);
        public bool IsSingular => Min == Max;
        public bool HasSpan => !IsEmpty && !IsSingular;

        public double Delta { get => Max - Min; set => SetCenterDelta(Center, value); }
        public double Center { get => (Max + Min) / 2; set => SetCenterDelta(value, Delta); }

        public override string ToString() { return !IsEmpty ? $"({Min:0.0000}, {Max:0.0000})" : "(unset)"; }
        public override bool Equals(object _Obj) { return _Obj is Range1 _Range && this == _Range; }
        public override int GetHashCode() { return Min.GetHashCode() ^ Max.GetHashCode(); }

        public void Clear() { IsEmpty = true; }

        public void Add(double _A)
        {
            if (double.IsNaN(_A))
                return;

            if (IsEmpty)
                Set(_A);
            else if (_A < Min)
                Min = _A;
            else if (_A > Max)
                Max = _A;
        }

        public void AddSimple(double _A)
        {
            if (IsEmpty)
                Set(_A);
            else if (_A < Min)
                Min = _A;
            else if (_A > Max)
                Max = _A;
        }

        public void Add(IEnumerable<double> _Values)
        {
            foreach (var Value in _Values)
                Add(Value);
        }

        public void Set(double _A) { Min = _A; Max = _A; IsEmpty = false; }
        public void Set(double _Min, double _Max) { Min = _Min; Max = _Max; IsEmpty = false; CoreTool.Order(ref Min, ref Max); }

        public void SetCenterDelta(double _Center, double _Delta)
        {
            Min = _Center - _Delta / 2;
            Max = _Center + _Delta / 2;
            CoreTool.Order(ref Min, ref Max);
        }

        public void Add(Range1 _A)
        {
            if (_A.IsEmpty)
                return;

            if (IsEmpty)
                Set(_A.Min, _A.Max);
            else
            {
                if (_A.Min < Min)
                    Min = _A.Min;
                if (_A.Max > Max)
                    Max = _A.Max;
            }
        }

        public void ExtendByMargin(double _Margin)
        {
            if (IsEmpty)
                return;

            Min -= _Margin;
            Max += _Margin;
            if (Min > Max)
                Min = Max = Center;
        }

        public static bool operator ==(Range1 _A, Range1 _B) { return _A.Min == _B.Min && _A.Max == _B.Max; }
        public static bool operator !=(Range1 _A, Range1 _B) { return _A.Min != _B.Min || _A.Max != _B.Max; }
    }
}
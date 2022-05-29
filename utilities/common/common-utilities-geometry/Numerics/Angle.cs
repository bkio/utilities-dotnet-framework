/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using Newtonsoft.Json;
using CommonUtilities.Geometry.Tools;

namespace CommonUtilities.Geometry.Numerics
{
    [JsonObject(MemberSerialization.OptIn)]
    public struct Angle : IComparable<Angle>
    {
        public const double TwoPI = PI * 2;
        public const double PI = Math.PI;
        public const double PIHalf = PI / 2;
        public const double PIQuart = PI / 4;
        public const double DegToRad = PI / 180;

        public static readonly Angle Zero = new Angle(0);
        public static readonly Angle NaN = new Angle(double.NaN);
        public static readonly Angle TwoPi = new Angle(TwoPI);
        public static readonly Angle Pi = new Angle(PI);
        public static readonly Angle PiHalf = new Angle(PIHalf);
        public static readonly Angle PiQuart = new Angle(PIQuart);
        public static readonly Angle North = new Angle(PIHalf);
        public static readonly Angle East = new Angle(0);
        public static readonly Angle South = new Angle(-PIHalf);
        public static readonly Angle West = new Angle(PI);

        private double RadiansValue;
        private double SinValue;
        private bool bHasSin;
        private double CosValue;
        private bool bHasCos;

        public const string RAD_PROPERTY = "rad";

        [JsonProperty(RAD_PROPERTY)]
        public double Rad { get => RadiansValue; set => RadiansValue = value; }
        public double Radians { get => RadiansValue; set { RadiansValue = value; Normalize(); } }
        public double Degrees { get => ToDegrees(Radians); set => Radians = ToRadians(value); }
        public double PositiveDeg { get => ToPositiveDeg(Degrees); set => Radians = ToRadians(value); }
        public double PositiveRad => ToPositiveRad(Radians);

        public double CompassDeg { get => ToCompassDeg(Degrees); set => Radians = ToRadians(90 - value); }
        public double CompassRad { get => ToCompassRad(Radians); set => Radians = PIHalf - value; }

        public double Sin { get { if (!bHasSin) { SinValue = Math.Sin(RadiansValue); bHasSin = true; } return SinValue; } }
        public double Cos { get { if (!bHasCos) { CosValue = Math.Cos(RadiansValue); bHasCos = true; } return CosValue; } }

        public double Sinc => FunctionsTool.Sinc(RadiansValue);
        public double Tan => Math.Tan(RadiansValue);

        public bool IsZero => RadiansValue == 0;

        public Angle(double _Radians) { 
            RadiansValue = _Radians; 
            SinValue = CosValue = 0; 
            bHasSin = bHasCos = false; 
            Normalize();
        }

        public Angle(double _X, double _Y) { 
            RadiansValue = FunctionsTool.Atan2(_X, _Y); 
            SinValue = CosValue = 0; 
            bHasSin = bHasCos = false; 
            Normalize();
        }

        public override string ToString() { return $"(Radians={Radians:0.000}, Degrees={Degrees:0.00})"; }
        public override bool Equals(object _Object) { return _Object is Angle _Angle && this == _Angle; }
        public override int GetHashCode() { return RadiansValue.GetHashCode(); }

        public int CompareTo(Angle _Other) { return RadiansValue.CompareTo(_Other.Radians); }

        public static bool operator ==(Angle _A, Angle _B) { return _A.Radians == _B.Radians; }
        public static bool operator !=(Angle _A, Angle _B) { return _A.Radians != _B.Radians; }
        public static bool operator <(Angle _A, Angle _B) { return _A.Radians < _B.Radians; }
        public static bool operator <=(Angle _A, Angle _B) { return _A.Radians <= _B.Radians; }
        public static bool operator >(Angle _A, Angle _B) { return _A.Radians > _B.Radians; }
        public static bool operator >=(Angle _A, Angle _B) { return _A.Radians >= _B.Radians; }
        public static Angle operator +(Angle _A, double _B) { return new Angle(_A.Radians + _B); }
        public static Angle operator +(Angle _A, Angle _B) { return new Angle(_A.Radians + _B.Radians); }
        public static Angle operator -(Angle _A, double _B) { return new Angle(_A.Radians - _B); }
        public static Angle operator -(Angle _A, Angle _B) { return new Angle(_A.Radians - _B.Radians); }
        public static Angle operator *(Angle _A, double _B) { return new Angle(_A.Radians * _B); }
        public static Angle operator /(Angle _A, double _B) { return new Angle(_A.Radians / _B); }
        public static Angle operator -(Angle _A) { return new Angle(-_A.Radians); }

        public void Normalize()
        {
            bHasSin = false;
            bHasCos = false;

            if (double.IsNaN(RadiansValue))
                return;

            Normalize(ref RadiansValue);
        }

        public static void Normalize(ref double _Radians)
        {
            while (_Radians > PI)
                _Radians -= TwoPI;
            while (_Radians <= -PI)
                _Radians += TwoPI;
        }


        public static double ToRadians(double _Degrees) { return _Degrees * DegToRad; }
        public static double ToDegrees(double _Radians) { return _Radians / DegToRad; }

        public static void ToRadians(ref double _Value) { _Value *= DegToRad; }
        public static void ToDegrees(ref double _Value) { _Value /= DegToRad; }

        private static double ToPositiveDeg(double _Degrees) { return _Degrees >= 0 ? _Degrees : _Degrees + 360; }
        private static double ToCompassDeg(double _Degrees) { return ToPositiveDeg(90 - _Degrees); }

        private static double ToPositiveRad(double _Radians) { return _Radians >= 0 ? _Radians : _Radians + TwoPI; }
        private static double ToCompassRad(double _Radians) { return ToPositiveRad(PIHalf - _Radians); }

        public static Angle FromDegrees(double _Degrees)
        {
            return new Angle(ToRadians(_Degrees));
        }

        public static void ForceBetween0AndPiHalf(ref double _Value)
        {
            while (_Value < 0)
                _Value += PIHalf;
            while (_Value >= PIHalf)
                _Value -= PIHalf;
        }

        public static void ForceBetween0AndPi(ref double _Value)
        {
            while (_Value < 0)
                _Value += PI;
            while (_Value >= PI)
                _Value -= PI;
        }

        public static void ForceBetween0And2Pi(ref double _Value)
        {
            while (_Value < 0)
                _Value += TwoPI;
            while (_Value >= TwoPI)
                _Value -= TwoPI;
        }

        public static void ForceBetween0And360(ref double _Value)
        {
            while (_Value < 0)
                _Value += 360;
            while (_Value >= 360)
                _Value -= 360;
        }
    }
}
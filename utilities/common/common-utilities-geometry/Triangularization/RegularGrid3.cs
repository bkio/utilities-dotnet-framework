/// Copyright 2022- Burak Kara, All rights reserved.

using Newtonsoft.Json;
using CommonUtilities.Geometry.Numerics;

namespace CommonUtilities.Geometry.Triangularization
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RegularGrid3 : Grid3, IShape3
    {
        public const string ORIGIN_PROPERTY = "origin";
        public const string INC_PROPERTY = "inc";
        public const string ROTATION_PROPERTY = "rotation";

        [JsonProperty(ORIGIN_PROPERTY)]
        public Vector3 Origin = Vector3.Zero;

        [JsonProperty(INC_PROPERTY)]
        public Vector3 Inc = Vector3.Unit;

        [JsonProperty(ROTATION_PROPERTY)]
        public Angle Rotation
        {
            get => RotationValue;
            set { RotationValue = value; IsRotated = !RotationValue.IsZero; }
        }

        protected Angle RotationValue = Angle.Zero;
        public bool IsRotated;

        public Vector3 UnrotatedMin
        {
            get => Origin;
            set => Origin = value;
        }
        public Vector3 UnrotatedMax => Origin + Inc * CellSize;
        public Range3 UnrotatedRange => new Range3(UnrotatedMin, UnrotatedMax);

        public RegularGrid3(Index3 _NodeSize, Range3 _UnrotatedRange, Angle _Rotation)
          : base(_NodeSize)
        {
            Rotation = _Rotation;
            Inc = _UnrotatedRange.Delta / CellSize;
            Origin = _UnrotatedRange.Min;
        }

        public override string ToString() { return $"Inc={Inc} Ori={Origin}{(IsRotated ? $" Rot={Rotation}" : " Nonerot")} {base.ToString()}"; }

        public override void ExpandRangeZ(ref Range1 _Range)
        {
            _Range.Add(UnrotatedRange.Z);
        }

        public override void ExpandRange(ref Range3 _Range)
        {
            _Range.Add(GetCornerPoints());
        }

        public Vector3 GetPoint3(Index3 _Node) { return GetPoint3(_Node.I, _Node.J, _Node.K); }
        public Vector3 GetCornerPoint(int _Corner) { return GetPoint3(GetCornerNode(_Corner)); }

        public Vector3[] GetCornerPoints()
        {
            var Corners = new Vector3[8];
            for (var Corner = 0; Corner < Corners.Length; Corner++)
                Corners[Corner] = GetCornerPoint(Corner);
            return Corners;
        }

        public Vector3 GetPoint3(int _I, int _J, int _K)
        {
            var Point = new Vector3
            {
                X = Origin.X + Inc.X * _I,
                Y = Origin.Y + Inc.Y * _J,
                Z = Origin.Z + Inc.Z * _K
            };
            if (IsRotated)
                Point.Rotate(RotationValue, Origin);
            return Point;
        }

        public void ConvertToGridCoord(ref Vector3 _Point)
        {
            _Point.Sub(Origin);
            if (IsRotated)
                _Point.Derotate(RotationValue);
            _Point.Div(Inc);
        }

        public void ConvertToFloatCell(ref Vector3 _Point)
        {
            ConvertToGridCoord(ref _Point);

            if (MaxNodeValue_IMinusEpsilon < _Point.X && _Point.X <= MaxNodeValue_IPlusEpsilon)
                _Point.X = MaxNodeValue_IMinusEpsilon;
            else if (MinusEpsilon <= _Point.X && _Point.X < 0)
                _Point.X = 0;

            if (MaxNodeValue_JMinusEpsilon < _Point.Y && _Point.Y <= MaxNodeValue_JPlusEpsilon)
                _Point.Y = MaxNodeValue_JMinusEpsilon;
            else if (MinusEpsilon <= _Point.Y && _Point.Y < 0)
                _Point.Y = 0;

            if (MaxNodeValue_KMinusEpsilon < _Point.Z && _Point.Z <= MaxNodeValue_KPlusEpsilon)
                _Point.Z = MaxNodeValue_KMinusEpsilon;
            else if (MinusEpsilon <= _Point.Z && _Point.Z < 0)
                _Point.Z = 0;
        }

        public bool GetCellByPoint(Vector3 _Point, out Index3 _Cell, bool _ForceInside = true)
        {
            ConvertToFloatCell(ref _Point);
            _Cell = _Point.Floor;
            if (IsCellInside(_Point))
                return true;

            if (_ForceInside)
                ForceCellInside(ref _Cell);
            return false;
        }
    }
}

/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using CommonUtilities.Geometry.Numerics;
using CommonUtilities.Geometry.Tools;

namespace CommonUtilities.Geometry.Triangularization
{
    [JsonObject(MemberSerialization.OptIn)]
    public partial class Triangle : IShape3, IPolygon3, IComparable<Triangle>
    {
        public const string ID_PROPERTY = "id";
        public const string A_PROPERTY = "a";
        public const string B_PROPERTY = "b";
        public const string C_PROPERTY = "c";
        public const string TYPE_PROPERTY = "type";

        [JsonProperty(ID_PROPERTY)]
        public short Id = -1;
        [JsonProperty(A_PROPERTY)]
        public Vector3 A;
        [JsonProperty(B_PROPERTY)]
        public Vector3 B;
        [JsonProperty(C_PROPERTY)]
        public Vector3 C;
        [JsonProperty(TYPE_PROPERTY)]
        public ETriangleType Type = ETriangleType.None;

        public Vector3 Normal => GeometryTool.Normal(A, B, C);
        public Vector3 Cross => GeometryTool.Cross3(A, B, C);
        public double CrossLength => GeometryTool.CrossLength(A, B, C);
        public Vector3 Center => (A + B + C) / 3;
        public double Area => CrossLength / 2;
        public bool IsCCW => GeometryTool.IsCCW(A, B, C);
        public bool IsNaN => A.IsNaN || B.IsNaN || C.IsNaN;

        public Triangle() { A = Vector3.Zero; B = Vector3.Zero; C = Vector3.Zero; }
        public Triangle(Vector3 _A, Vector3 _B, Vector3 _C) { SetCCW(_A, _B, _C); }

        public override string ToString() { return $"A={A}, B={B}, C={C}"; }

        public int CompareTo(Triangle _Other)
        {
            var result = A.CompareTo(_Other.A);
            if (result != 0)
                return result;
            result = B.CompareTo(_Other.B);
            if (result != 0)
                return result;
            return C.CompareTo(_Other.C);
        }

        public void ExpandRangeZ(ref Range1 _Range)
        {
            _Range.Add(A.Z);
            _Range.Add(B.Z);
            _Range.Add(C.Z);
        }

        public void ExpandRange(ref Range3 _Range)
        {
            _Range.Add(A);
            _Range.Add(B);
            _Range.Add(C);
        }

        public IEnumerable<Vector3> Points
        {
            get
            {
                yield return A;
                yield return B;
                yield return C;
            }
        }

        public Vector3 this[int _Index]
        {
            get
            {
                return _Index switch
                {
                    0 => A,
                    1 => B,
                    2 => C,
                    _ => throw new IndexOutOfRangeException(),
                };
            }
            set
            {
                switch (_Index)
                {
                    case 0:
                        A = value;
                        break;
                    case 1:
                        B = value;
                        break;
                    case 2:
                        C = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public void SetCCW(Vector3 _A, Vector3 _B, Vector3 _C)
        {
            A = _A;
            B = _B;
            C = _C;
        }

        public double GetSignedVolume()
        {
            return A.Dot(B.Cross(C)) / 6;
        }
    }
}
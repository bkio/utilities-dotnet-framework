/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using CommonUtilities.Geometry.Numerics;
using CommonUtilities.Geometry.Tools;

namespace CommonUtilities.Geometry.Triangularization
{
    public partial class IndexedTriangleMesh : Shape
    {
        public const string POINTS_PROPERTY = "points";
        public const string INDEXED_TRIANGLES_PROPERTY = "indexedTriangles";
        public const string POINT_COUNT_PROPERTY = "pointCount";
        public const string TRIANGLE_COUNT_PROPERTY = "triangleCount";

        [JsonProperty(POINTS_PROPERTY)]
        public Vector3[] Points { get; set; }

        [JsonProperty(INDEXED_TRIANGLES_PROPERTY)]
        public IndexedTriangle[] IndexedTriangles { get; set; }

        [JsonProperty(POINT_COUNT_PROPERTY)]
        public int PointCount => Points?.Length ?? 0;

        [JsonProperty(TRIANGLE_COUNT_PROPERTY)]
        public int TriangleCount => IndexedTriangles?.Length ?? 0;

        public IndexedTriangleMesh(Vector3[] _Points, IndexedTriangle[] _IndexedTriangles)
        {
            Points = _Points;
            IndexedTriangles = _IndexedTriangles;
        }

        public override string ToString()
        {
            return $"PointCount={PointCount}, TriangleCount={TriangleCount}";
        }

        public override void ExpandRangeZ(ref Range1 _Range)
        {
            for (var i = Points.Length - 1; i >= 0; i--)
                _Range.Add(Points[i].Z);
        }

        public override void ExpandRange(ref Range3 _Range)
        {
            for (var i = Points.Length - 1; i >= 0; i--)
                _Range.Add(Points[i]);
        }

        public override void Transform(ITransformer3 _Transformer)
        {
            for (var i = Points.Length - 1; i >= 0; i--)
                _Transformer.Transform(ref Points[i]);
        }

        public bool IsEqual(IndexedTriangleMesh _Other, double _Tolerance)
        {
            if (Points.Length != _Other.Points.Length)
                return false;

            if (IndexedTriangles.Length != _Other.IndexedTriangles.Length)
                return false;

            for (var i = Points.Length - 1; i >= 0; i--)
                if (!Points[i].IsEqual(_Other.Points[i], _Tolerance))
                    return false;

            for (var i = IndexedTriangles.Length - 1; i >= 0; i--)
                if (!IndexedTriangles[i].IsEqual(_Other.IndexedTriangles[i]))
                    return false;

            return true;
        }

        public Triangle GetTriangle(IndexedTriangle _Triangle)
        {
            return new Triangle(Points[_Triangle.I], Points[_Triangle.J], Points[_Triangle.K]);
        }

        public Vector3[] CreatePointNormals(bool _AngleBased = true)
        {
            var NumPoints = PointCount;
            var Result = CoreTool.Create(NumPoints, Vector3.Zero);

            if (_AngleBased)
            {
                foreach (var IndexedTri in IndexedTriangles)
                {
                    var P0 = Points[IndexedTri.I];
                    var P1 = Points[IndexedTri.J];
                    var P2 = Points[IndexedTri.K];

                    var Cross0 = GeometryTool.Cross3(P0, P1, P2);
                    var CrossLength = Cross0.Length;

                    if (GeometryApproximationTool.IsLengthEqual(CrossLength, 0))
                        continue;

                    Cross0.Div(CrossLength);

                    var Dot0 = GeometryTool.Dot3(P0, P1, P2);
                    var Dot1 = GeometryTool.Dot3(P1, P2, P0);
                    var Dot2 = GeometryTool.Dot3(P2, P0, P1);

                    var angle0 = FunctionsTool.Atan2(Dot0, CrossLength);
                    var angle1 = FunctionsTool.Atan2(Dot1, CrossLength);
                    var angle2 = FunctionsTool.Atan2(Dot2, CrossLength);

                    var Cross1 = Cross0;
                    var Cross2 = Cross0;

                    Cross0.Mul(angle0);
                    Cross1.Mul(angle1);
                    Cross2.Mul(angle2);

                    Result[IndexedTri.I].Add(Cross0);
                    Result[IndexedTri.J].Add(Cross1);
                    Result[IndexedTri.K].Add(Cross2);
                }
            }
            else
            {
                var Tri = new Triangle();
                foreach (var IndexedTri in IndexedTriangles)
                {
                    Tri.SetCCW(Points[IndexedTri.I], Points[IndexedTri.J], Points[IndexedTri.K]);

                    var Cross = Tri.Cross;
                    Result[IndexedTri.I].Add(Cross);
                    Result[IndexedTri.J].Add(Cross);
                    Result[IndexedTri.K].Add(Cross);
                }
            }
            for (var i = 0; i < NumPoints; i++)
            {
                if (Result[i].IsAlmostNull)
                    Result[i] = Vector3.Up;
                else
                    Result[i].Normalize();
            }
            return Result;
        }

        public static IndexedTriangleMesh CreateByPointsOnly(List<Vector3> _Points, double _Tolerance = -1)
        {
            int PointCount = _Points.Count;
            if (PointCount == 0)
                return null;

            if (PointCount % 3 != 0)
                throw new Exception("Illegal number and points");

            var NumTriangles = PointCount / 3;
            if (NumTriangles == 0)
                return null;

            var Map = new int[PointCount];
            var SortedPoints = new PointSortGrid3(_Points);

            var Builder =
              new IndexedTriangleMeshBuilder(SortedPoints.CreateAndAddUnique(_Points, Map, _Tolerance))
              {
                  IndexedTriangles = { Capacity = NumTriangles }
              };

            for (int triangleIndex = 0, pointIndex = 0; triangleIndex < NumTriangles; triangleIndex++)
            {
                var index0 = Map[pointIndex++];
                var index1 = Map[pointIndex++];
                var index2 = Map[pointIndex++];

                Builder.AddTriangle(index0, index1, index2);
            }
            return Builder.CreateMesh();
        }
    }
}
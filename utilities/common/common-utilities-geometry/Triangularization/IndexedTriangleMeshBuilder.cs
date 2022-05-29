/// Copyright 2022- Burak Kara, All rights reserved.

using System.Collections.Generic;
using CommonUtilities.Geometry.Numerics;

namespace CommonUtilities.Geometry.Triangularization
{
    public class IndexedTriangleMeshBuilder
    {
        public List<Vector3> Points { get; set; }
        public List<IndexedTriangle> IndexedTriangles { get; set; }

        public bool IsEmpty => PointCount == 0 || TriangleCount == 0;
        public int PointCount => Points.Count;
        public int TriangleCount => IndexedTriangles.Count;

        public IndexedTriangleMeshBuilder(List<Vector3> _Points)
        {
            Points = _Points;
            IndexedTriangles = new List<IndexedTriangle>();
        }

        public IndexedTriangleMeshBuilder()
        {
            Points = new List<Vector3>();
            IndexedTriangles = new List<IndexedTriangle>();
        }

        public bool IsLegalTriangle(int _Index0, int _Index1, int _Index2)
        {
            var MaxIndex = PointCount - 1;
            if (_Index0 > MaxIndex || _Index1 > MaxIndex || _Index2 > MaxIndex)
                return false;
            if (_Index0 == _Index1 || _Index1 == _Index2 || _Index2 == _Index0)
                return false;
            return true;
        }

        public IndexedTriangleMesh CreateMesh()
        {
            return new IndexedTriangleMesh(Points.ToArray(), IndexedTriangles.ToArray());
        }

        public void Add(Vector3 _Point)
        {
            Points.Add(_Point);
        }

        public void AddTriangle(int _Index0, int _Index1, int _Index2)
        {
            IndexedTriangles.Add(new IndexedTriangle(_Index0, _Index1, _Index2));
        }
    }
}
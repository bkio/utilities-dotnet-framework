/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Security.Cryptography;
using CommonUtilities.Geometry.Numerics;
using CommonUtilities.Geometry.Triangularization;

namespace CommonUtilities.Geometry.Tools.Compiler
{
    public class TriangleMeshDecompiler
    {
        private readonly IndexedTriangleMesh TriangleMesh;

        //for each Vector3 float fields (x, y, z) size. if it is double, it should be 8
        private static readonly int SizeOfVector3Field = sizeof(double);

        //for each int value, size is 4
        private static readonly int SizeOfTriangleIndex = sizeof(int);

        private static readonly int SizeOfVector3 = SizeOfVector3Field * 3;

        private static readonly int SizeOfIndexedTriangle = SizeOfTriangleIndex * 3;

        private readonly byte[] CompiledGeometry;
        public byte[] GetCompiledGeometry()
        {
            return CompiledGeometry;
        }

        private readonly int CompiledGeometrySize;
        public int GetCompiledGeometrySize()
        {
            return CompiledGeometrySize;
        }

        //Color (Color4=4*1), NumberOfPoints (int=4), NumberOfIndexedTriangles (int=4)
        public static readonly int MinCompiledSize = 12;

        //NumberOfPoints, NumberOfIndexedTriangles
        public static readonly int MinCompiledSize_WithoutColor = 8;

        private readonly string HashCode;
        public string ToHash()
        {
            return HashCode;
        }

        private TriangleMeshDecompiler(IndexedTriangleMesh _Mesh, Color4 _Color)
        {
            TriangleMesh = _Mesh;

            CompiledGeometrySize = MinCompiledSize + (TriangleMesh.Points.Length * SizeOfVector3) + (TriangleMesh.IndexedTriangles.Length * SizeOfIndexedTriangle);
            CompiledGeometry = new byte[CompiledGeometrySize];

            //Vector3 Color
            Array.Copy(BitConverter.GetBytes(_Color.R), 0, CompiledGeometry, 0, 1);
            Array.Copy(BitConverter.GetBytes(_Color.G), 0, CompiledGeometry, 1, 1);
            Array.Copy(BitConverter.GetBytes(_Color.B), 0, CompiledGeometry, 2, 1);
            Array.Copy(BitConverter.GetBytes(_Color.A), 0, CompiledGeometry, 3, 1);

            //int NumberOfPoints
            Array.Copy(BitConverter.GetBytes(TriangleMesh.Points.Length), 0, CompiledGeometry, 4, 4);

            //int NumberOfIndexedTriangles
            Array.Copy(BitConverter.GetBytes(TriangleMesh.IndexedTriangles.Length), 0, CompiledGeometry, 8, 4);

            int StartIndex = MinCompiledSize;

            //Vector3[] Points
            for (int i = 0; i < TriangleMesh.Points.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(TriangleMesh.Points[i].X), 0, CompiledGeometry, StartIndex + (i * SizeOfVector3) + SizeOfVector3Field * 0, SizeOfVector3Field);
                Array.Copy(BitConverter.GetBytes(TriangleMesh.Points[i].Y), 0, CompiledGeometry, StartIndex + (i * SizeOfVector3) + SizeOfVector3Field * 1, SizeOfVector3Field);
                Array.Copy(BitConverter.GetBytes(TriangleMesh.Points[i].Z), 0, CompiledGeometry, StartIndex + (i * SizeOfVector3) + SizeOfVector3Field * 2, SizeOfVector3Field);
            }

            StartIndex += TriangleMesh.Points.Length * SizeOfVector3;

            //IndexedTriangle[] Triangles
            for (int i = 0; i < TriangleMesh.IndexedTriangles.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(TriangleMesh.IndexedTriangles[i].I), 0, CompiledGeometry, StartIndex + (i * SizeOfIndexedTriangle) + SizeOfTriangleIndex * 0, SizeOfTriangleIndex);
                Array.Copy(BitConverter.GetBytes(TriangleMesh.IndexedTriangles[i].J), 0, CompiledGeometry, StartIndex + (i * SizeOfIndexedTriangle) + SizeOfTriangleIndex * 1, SizeOfTriangleIndex);
                Array.Copy(BitConverter.GetBytes(TriangleMesh.IndexedTriangles[i].K), 0, CompiledGeometry, StartIndex + (i * SizeOfIndexedTriangle) + SizeOfTriangleIndex * 2, SizeOfTriangleIndex);
            }

            using (MD5 MD5Instance = MD5.Create())
            {
                HashCode = Convert.ToBase64String(MD5Instance.ComputeHash(CompiledGeometry));
            }
        }

        public static TriangleMeshDecompiler CreateTriangleMeshGeometry(IndexedTriangleMesh _Mesh, Color4 _Color)
        {
            if (_Mesh != null && _Color != null && _Mesh.IndexedTriangles != null && _Mesh.IndexedTriangles.Length > 0 && _Mesh.Points.Length > 0)
            {
                return new TriangleMeshDecompiler(_Mesh, _Color);
            }
            return null;
        }

        public static Tuple<IndexedTriangleMesh, Color4> Decompile(byte[] _Source, int _StartIndex)
        {
            if (_Source != null && _StartIndex >= 0 && _Source.Length >= (_StartIndex + MinCompiledSize))
            {
                //Vector Color
                Color4 Color = new Color4
                {
                    R = _Source[_StartIndex + 0],
                    G = _Source[_StartIndex + 1],
                    B = _Source[_StartIndex + 2],
                    A = _Source[_StartIndex + 3]
                };

                //int NumberOfPoints
                int NumberOfPoints = BitConverter.ToInt32(_Source, _StartIndex + 4);

                //int NumberOfIndexedTriangles
                int NumberOfIndexedTriangles = BitConverter.ToInt32(_Source, _StartIndex + 8);

                if (NumberOfPoints > 0 && NumberOfIndexedTriangles > 0 && _Source.Length >= (_StartIndex + MinCompiledSize + (NumberOfPoints * SizeOfVector3) + (NumberOfIndexedTriangles * SizeOfIndexedTriangle)))
                {
                    _StartIndex += MinCompiledSize;

                    //Vector3[] Points
                    Vector3[] Points = new Vector3[NumberOfPoints];
                    for (int i = 0; i < NumberOfPoints; i++)
                    {
                        Points[i] = new Vector3
                        {
                            X = BitConverter.ToDouble(_Source, _StartIndex + (i * SizeOfVector3) + SizeOfVector3Field * 0),
                            Y = BitConverter.ToDouble(_Source, _StartIndex + (i * SizeOfVector3) + SizeOfVector3Field * 1),
                            Z = BitConverter.ToDouble(_Source, _StartIndex + (i * SizeOfVector3) + SizeOfVector3Field * 2)
                        };
                    }

                    _StartIndex += NumberOfPoints * SizeOfVector3;

                    //IndexedTriangle[] Triangles
                    IndexedTriangle[] Triangles = new IndexedTriangle[NumberOfIndexedTriangles];
                    for (int i = 0; i < NumberOfIndexedTriangles; i++)
                    {
                        var index0 = BitConverter.ToInt32(_Source, _StartIndex + (i * SizeOfIndexedTriangle) + SizeOfTriangleIndex * 0);
                        var index1 = BitConverter.ToInt32(_Source, _StartIndex + (i * SizeOfIndexedTriangle) + SizeOfTriangleIndex * 1);
                        var index2 = BitConverter.ToInt32(_Source, _StartIndex + (i * SizeOfIndexedTriangle) + SizeOfTriangleIndex * 2);

                        Triangles[i] = new IndexedTriangle(index0, index1, index2);
                    }

                    return new Tuple<IndexedTriangleMesh, Color4>(new IndexedTriangleMesh(Points, Triangles), Color);
                }
            }
            return null;
        }
    }
}
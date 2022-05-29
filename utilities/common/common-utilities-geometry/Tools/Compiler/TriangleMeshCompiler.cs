/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using CommonUtilities.Geometry.Numerics;
using CommonUtilities.Geometry.Triangularization;

namespace CommonUtilities.Geometry.Tools.Compiler
{
    public class TriangleMeshCompiler
    {
        public static readonly byte Identifier_PolygonNode = 1;

        public Tuple<byte[], string> Compile(TriangleMeshDecompiler _MeshGeometry)
        {
            if (_MeshGeometry != null)
            {
                /*
                 * Identifier                                           1 byte                      * 1         = 1
                 * Color (Color4)                                      4 byte                      * 1         = 4
                 * int NumberOfPoints                                   1 int                       * 4         = 4                                 (Included in GetCompiledGeometrySize)
                 * int NumberOfIndexedTriangles                         1 int                       * 4         = 4                                 (Included in GetCompiledGeometrySize)
                 * Vector3[] Points                                     NumberOfPoints * 3 doubles  * 8         = NumberOfPoints * Vector3.Size               (Included in GetCompiledGeometrySize)
                 * IndexedTriangle[] Triangles                          NumberOfIndexedTriangles * 3 ints * 4   = NumberOfIndexedTriangles * IndexedTriangle.Size     (Included in GetCompiledGeometrySize)
                 *                                                                                        Total = 1 + (GetCompiledGeometrySize())
                 */
                var Buffer = new byte[1 + _MeshGeometry.GetCompiledGeometrySize()];

                //Identifier
                Buffer[0] = Identifier_PolygonNode;

                Array.Copy(_MeshGeometry.GetCompiledGeometry(), 0, Buffer, 1, _MeshGeometry.GetCompiledGeometrySize());

                return new Tuple<byte[], string>(Buffer, _MeshGeometry.ToHash());
            }
            return null;
        }

        public Tuple<IndexedTriangleMesh, Color4> Decompile(byte[] _CompiledDataWithIdentifier)
        {
            if (_CompiledDataWithIdentifier != null && _CompiledDataWithIdentifier.Length >= (1 + TriangleMeshDecompiler.MinCompiledSize))
            {
                return TriangleMeshDecompiler.Decompile(_CompiledDataWithIdentifier, 1);
            }
            return null;
        }
    }
}
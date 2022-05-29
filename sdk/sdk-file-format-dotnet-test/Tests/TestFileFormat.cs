/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CommonUtilities.Geometry.Numerics;
using CommonUtilities.Geometry.Triangularization;
using CommonUtilities.Geometry.Utilities.ObjParser;
using CommonUtilities.Geometry.Utilities.ObjParser.Types;
using SDKFileFormat.Process.RandomAccessFile;

namespace SDKFileFormat.Tests
{
    public class TestFileFormat
    {
        private static long Id = 0;

        private static ulong GetId()
        {
            return (ulong)Interlocked.Increment(ref Id);
        }

        public static void SimpleReadWriteTest(bool Compress = true)
        {
            string[][] LodList = new string[][]
            {
                new string[]{ "Content/Models/monkey.obj", "Content/Models/monkeyLod1.obj", "Content/Models/monkeyLod2.obj" },
                new string[]{ "Content/Models/squishymonkey.obj", "Content/Models/squishymonkeyLod1.obj" },
                new string[]{ "Content/Models/HDmonkey.obj", "Content/Models/HDMonkeyLod1.obj", "Content/Models/HDMonkeyLod2.obj", "Content/Models/HDMonkeyLod3.obj" }
            };

            string[] MetadataStrings = new string[] { "Name:Monkey", "Name:Squishymonkey", "Name:HDmonkey" };

            string Ext = "";
            EDeflateCompression CompressEnum;

            if (Compress)
            {
                CompressEnum = EDeflateCompression.Compress;
                Ext = "x3_c_";
            }
            else
            {
                CompressEnum = EDeflateCompression.DoNotCompress;
                Ext = "x3_p_";
            }

            List<HierarchyNode> WriteHNodes = new List<HierarchyNode>();
            List<GeometryNode> WritePolygons = new List<GeometryNode>();
            List<MetadataNode> WriteMNodes = new List<MetadataNode>();

            Random Rand = new Random();

            for (int i = 0; i < 3; ++i)
            {
                HierarchyNode HNode = new HierarchyNode();
                HNode.UniqueID = GetId();


                MetadataNode Meta = new MetadataNode();
                Meta.Metadata = MetadataStrings[i];
                Meta.UniqueID = GetId();

                var Polygon = new GeometryNode();

                for (int f = 0; f < LodList[i].Length; ++f)
                {
                    Obj ObjFile = new Obj();
                    ObjFile.LoadObj(LodList[i][f]);
                    SetPolygonData(Polygon, ObjFile, f);
                }

                Polygon.UniqueID = GetId();

                HierarchyNode.GeometryPart Part = CreateGeometryPart(Rand, i, Polygon);

                HNode.GeometryParts.Add(Part);

                HNode.MetadataID = Meta.UniqueID;

                if (i != 0)
                {
                    HNode.ParentID = WriteHNodes[i - 1].UniqueID;
                    WriteHNodes[i - 1].ChildNodes.Add(HNode.UniqueID);

                    //HierarchyNode.GeometryPart InstancePart = CreateGeometryPart(Rand, i, WritePolygons[i - 1]);
                    //HNode.GeometryParts.Add(InstancePart);
                }

                WriteHNodes.Add(HNode);
                WritePolygons.Add(Polygon);
                WriteMNodes.Add(Meta);
            }

            Dictionary<ENodeType, StreamStruct> Streams = new Dictionary<ENodeType, StreamStruct>();

            FileStream WriteFileStreamH = new FileStream($"m.{Ext}h", FileMode.Create);
            StreamStruct StreamStructH = new StreamStruct(WriteFileStreamH, CompressEnum);

            FileStream WriteFileStreamG = new FileStream($"m.{Ext}g", FileMode.Create);
            StreamStruct StreamStructG = new StreamStruct(WriteFileStreamG, CompressEnum);

            FileStream FileStreamM = new FileStream($"m.{Ext}m", FileMode.Create);
            StreamStruct StreamStructM = new StreamStruct(FileStreamM, CompressEnum);

            Streams.Add(ENodeType.Hierarchy, StreamStructH);
            Streams.Add(ENodeType.Geometry, StreamStructG);
            Streams.Add(ENodeType.Metadata, StreamStructM);

            Console.WriteLine("Writing");

            using (FileFormatStreamWriter writer = new FileFormatStreamWriter(Streams))
            {
                for (int i = 0; i < WriteHNodes.Count; ++i)
                {
                    writer.Write(WriteHNodes[i]);
                }

                for (int i = 0; i < WritePolygons.Count; ++i)
                {
                    writer.Write(WritePolygons[i]);
                }

                for (int i = 0; i < WriteMNodes.Count; ++i)
                {
                    writer.Write(WriteMNodes[i]);
                }
            }
            //Dispose of stream here
            Streams[ENodeType.Hierarchy].IOStream.Dispose();
            Streams[ENodeType.Metadata].IOStream.Dispose();
            Streams[ENodeType.Geometry].IOStream.Dispose();


            Console.WriteLine("Done writing");
            Console.WriteLine("Reading");

            List<GeometryNode> ReadPolygons = new List<GeometryNode>();
            List<HierarchyNode> ReadHNodes = new List<HierarchyNode>();
            List<MetadataNode> ReadMNodes = new List<MetadataNode>();

            ManualResetEvent WaitReads = new ManualResetEvent(false);
            int ReadsLeft = WriteHNodes.Count + WriteMNodes.Count + WritePolygons.Count;

            FileStream ReadFileStreamH = new FileStream($"m.{Ext}h", FileMode.Open);
            FileStream ReadFileStreamG = new FileStream($"m.{Ext}g", FileMode.Open);
            FileStream ReadFileStreamM = new FileStream($"m.{Ext}m", FileMode.Open);

            using (FileFormatStreamReader ReaderH = new FileFormatStreamReader(ENodeType.Hierarchy, ReadFileStreamH,
                (_Sdk) =>
                {
                },
                (_Node) =>
                {
                    Interlocked.Decrement(ref ReadsLeft);
                    lock (ReadHNodes)
                    {
                        ReadHNodes.Add((HierarchyNode)_Node);
                    }
                    if (ReadsLeft == 0)
                    {
                        WaitReads.Set();
                    }
                }, CompressEnum))
            {
                if (!ReaderH.Process(Console.WriteLine))
                {
                    throw new Exception("Failed");
                }
            }

            using (FileFormatStreamReader ReaderM = new FileFormatStreamReader(ENodeType.Metadata, ReadFileStreamM,
            (_Sdk) =>
            {
            },
            (_Node) =>
            {
                Interlocked.Decrement(ref ReadsLeft);
                lock (ReadMNodes)
                {
                    ReadMNodes.Add((MetadataNode)_Node);
                }
                if (ReadsLeft == 0)
                {
                    WaitReads.Set();
                }
            }, CompressEnum))
            {
                if (!ReaderM.Process(Console.WriteLine))
                {
                    throw new Exception("Failed");
                }
            }

            using (FileFormatStreamReader ReaderG = new FileFormatStreamReader(ENodeType.Geometry, ReadFileStreamG,
                (_Sdk) =>
                {
                },
                (_Node) =>
                {
                    Interlocked.Decrement(ref ReadsLeft);
                    lock (ReadPolygons)
                    {
                        ReadPolygons.Add((GeometryNode)_Node);
                    }
                    if (ReadsLeft == 0)
                    {
                        WaitReads.Set();
                    }
                }, CompressEnum))
            {
                if (!ReaderG.Process(Console.WriteLine))
                {
                    throw new Exception("Failed");
                }
            }

            WaitReads.WaitOne();
            Console.WriteLine("Done Reading");

            WritePolygons = WritePolygons.OrderBy(x => x.UniqueID).ToList();
            ReadPolygons = ReadPolygons.OrderBy(x => x.UniqueID).ToList();
            WriteMNodes = WriteMNodes.OrderBy(x => x.UniqueID).ToList();
            ReadMNodes = ReadMNodes.OrderBy(x => x.UniqueID).ToList();
            WriteHNodes = WriteHNodes.OrderBy(x => x.UniqueID).ToList();
            ReadHNodes = ReadHNodes.OrderBy(x => x.UniqueID).ToList();

            CheckInputVsOutput(WritePolygons, WriteHNodes, WriteMNodes, ReadPolygons, ReadHNodes, ReadMNodes);
        }

        private static HierarchyNode.GeometryPart CreateGeometryPart(Random Rand, int i, GeometryNode Polygon)
        {
            HierarchyNode.GeometryPart Part = new HierarchyNode.GeometryPart();
            Part.GeometryID = Polygon.UniqueID;
            Part.Location = new Vector3((float)Rand.Next(0, 500), (float)Rand.Next(0, 500), (float)Rand.Next(0, 500));
            Part.Rotation = new Quaternion(new Vector3((float)Rand.Next(0, 360), (float)Rand.Next(0, 360), (float)0), (float)1);
            Part.Scale = new Vector3(i, i, i);
            Part.Color = new Color4((byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255), (byte)Rand.Next(0, 255), 255);
            return Part;
        }

        private static void CheckInputVsOutput(List<GeometryNode> WritePolygons, List<HierarchyNode> WriteHNodes, List<MetadataNode> WriteMNodes, List<GeometryNode> ReadPolygons, List<HierarchyNode> ReadHNodes, List<MetadataNode> ReadMNodes)
        {
            Console.WriteLine("Checking");

            if (WritePolygons.Count != ReadPolygons.Count || WriteMNodes.Count != ReadMNodes.Count || WriteHNodes.Count != ReadHNodes.Count)
            {
                throw new Exception("Failed");
            }

            for (int i = 0; i < WriteHNodes.Count; ++i)
            {
                if (!WriteHNodes[i].Equals(ReadHNodes[i]))
                {
                    throw new Exception("Failed");
                }
            }

            for (int i = 0; i < WriteMNodes.Count; ++i)
            {
                if (!WriteMNodes[i].Equals(ReadMNodes[i]))
                {
                    throw new Exception("Failed");
                }
            }

            for (int i = 0; i < WritePolygons.Count; ++i)
            {
                if (!WritePolygons[i].Equals(ReadPolygons[i]))
                {
                    throw new Exception("Failed");
                }
            }

            for (int i = 0; i < WritePolygons.Count; ++i)
            {
                if (WritePolygons[i].LODs.Count != ReadPolygons[i].LODs.Count)
                {
                    throw new Exception("Failed");
                }

                for (int l = 0; l < WritePolygons[i].LODs.Count; ++l)
                {
                    if (WritePolygons[i].LODs[l].Indexes.Count != ReadPolygons[i].LODs[l].Indexes.Count)
                    {
                        throw new Exception("Failed");
                    }

                    if (WritePolygons[i].LODs[l].VertexNormalTangentList.Count != ReadPolygons[i].LODs[l].VertexNormalTangentList.Count)
                    {
                        throw new Exception("Failed");
                    }
                }
            }

            Console.WriteLine("Done Checking");
        }

        private static void SetPolygonData(GeometryNode Polygon, Obj obj, int LodLevel)
        {
            if (Polygon.LODs.Count <= LodLevel)
            {
                for (int i = Polygon.LODs.Count - 1; i < LodLevel; ++i)
                {
                    Polygon.LODs.Add(new LOD());
                }
            }

            var VertexTangentNormals = new VertexNormalTangent[obj.VertexList.Count];

            //Assumes triangulated faces
            for (int i = 0; i < obj.FaceList.Count; ++i)
            {
                for (int c = 0; c < 3; c++)
                {
                    Vertex v1 = obj.VertexList[obj.FaceList[i].VertexIndexList[c] - 1];
                    VertexNormal vn1 = obj.NormalList[obj.FaceList[i].VertexNormalIndexList[c] - 1];

                    int CurrentIndex = v1.Index - 1;

                    Polygon.LODs[LodLevel].Indexes.Add((uint)CurrentIndex);

                    VertexTangentNormals[CurrentIndex] = new VertexNormalTangent();

                    if (VertexTangentNormals[CurrentIndex].Vertex == null)
                    {
                        VertexTangentNormals[CurrentIndex].Vertex = new Vector3((float)v1.X *100, (float)v1.Y*100, (float)v1.Z *100);
                    }
                    else
                    {
                        VertexTangentNormals[CurrentIndex].Vertex.X = (float)v1.X * 100;
                        VertexTangentNormals[CurrentIndex].Vertex.Y = (float)v1.Y * 100;
                        VertexTangentNormals[CurrentIndex].Vertex.Z = (float)v1.Z * 100;
                    }

                    if (VertexTangentNormals[CurrentIndex].Normal == null)
                    {
                        VertexTangentNormals[CurrentIndex].Normal = new Vector3((float)vn1.X, (float)vn1.Y, (float)vn1.Z);
                    }
                    else
                    {
                        VertexTangentNormals[CurrentIndex].Normal.X = (float)vn1.X;
                        VertexTangentNormals[CurrentIndex].Normal.Y = (float)vn1.Y;
                        VertexTangentNormals[CurrentIndex].Normal.Z = (float)vn1.Z;
                    }

                }
            }

            Polygon.LODs[LodLevel].VertexNormalTangentList = new List<VertexNormalTangent>(VertexTangentNormals);
            Polygon.LODs[LodLevel].Indexes.Reverse();

        }
    }
}
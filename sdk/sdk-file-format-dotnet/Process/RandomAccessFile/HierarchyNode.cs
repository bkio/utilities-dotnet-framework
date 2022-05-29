/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using CommonUtilities.Geometry.Numerics;
using CommonUtilities.Geometry.Utilities;

namespace SDKFileFormat.Process.RandomAccessFile
{
    public class HierarchyNode : Node
    {
        public override ENodeType GetNodeType()
        {
            return ENodeType.Hierarchy;
        }

        public class GeometryPart
        {
            public const string GEOMETRY_ID_PROPERTY = "geometryId";
            public const string LOCATION_PROPERTY = "location";
            public const string ROTATION_PROPERTY = "rotation";
            public const string SCALE_PROPERTY = "scale";
            public const string COLOR_PROPERTY = "color";

            [JsonProperty(GEOMETRY_ID_PROPERTY)]
            public ulong GeometryID;

            [JsonProperty(LOCATION_PROPERTY)]
            public Vector3 Location = new Vector3();

            [JsonProperty(ROTATION_PROPERTY)]
            public Quaternion Rotation = new Quaternion();

            [JsonProperty(SCALE_PROPERTY)]
            public Vector3 Scale = new Vector3(1.0f, 1.0f, 1.0f);

            [JsonProperty(COLOR_PROPERTY)]
            public Color4 Color;
        };

        public const string PARENT_ID_PROPERTY = "parentId";
        public const string METADATA_ID_PROPERTY = "metadataId";
        public const string GEOMETRY_PARTS_PROPERTY = "geometryParts";
        public const string CHILD_NODES_PROPERTY = "childNodes";

        [JsonProperty(PARENT_ID_PROPERTY)]
        public ulong ParentID = UNDEFINED_ID;

        [JsonProperty(METADATA_ID_PROPERTY)]
        public ulong MetadataID = UNDEFINED_ID;

        [JsonProperty(GEOMETRY_PARTS_PROPERTY)]
        public List<GeometryPart> GeometryParts = new List<GeometryPart>();

        [JsonProperty(CHILD_NODES_PROPERTY)]
        public List<ulong> ChildNodes = new List<ulong>();

        public override bool Equals(object _Other)
        {
            if (!base.Equals(_Other)) return false;
            if (!(_Other is HierarchyNode Casted)) return false;

            if (ParentID != Casted.ParentID) return false;
            if (MetadataID != Casted.MetadataID) return false;

            if (GeometryParts.Count != Casted.GeometryParts.Count) return false;
            if (ChildNodes.Count != Casted.ChildNodes.Count) return false;

            for (int i = 0; i < GeometryParts.Count; ++i)
            {
                if (GeometryParts[i].GeometryID != Casted.GeometryParts[i].GeometryID) return false;
                if (!GeometryParts[i].Location.Equals(Casted.GeometryParts[i].Location)) return false;
                if (!GeometryParts[i].Rotation.Equals(Casted.GeometryParts[i].Rotation)) return false;
                if (!GeometryParts[i].Scale.Equals(Casted.GeometryParts[i].Scale)) return false;
                if (!GeometryParts[i].Color.Equals(Casted.GeometryParts[i].Color)) return false;
            }

            for (int i = 0; i < ChildNodes.Count; ++i)
            {
                if (ChildNodes[i] != Casted.ChildNodes[i]) return false;
            }
            return true;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override int GetSize()
        {
            return
                base.GetSize() +
                sizeof(ulong) +                                 // ParentID
                sizeof(ulong) +                                 // MetadataID
                sizeof(uint) +                                  // GeometryParts size
                                                                // GeometryParts:
                (sizeof(ulong) * GeometryParts.Count) +                 //    GeometryID
                (sizeof(float) * GeometryParts.Count) +                 //    Location.X
                (sizeof(float) * GeometryParts.Count) +                 //    Location.Y
                (sizeof(float) * GeometryParts.Count) +                 //    Location.Z
                (sizeof(float) * GeometryParts.Count) +                 //    Rotation.X
                (sizeof(float) * GeometryParts.Count) +                 //    Rotation.Y
                (sizeof(float) * GeometryParts.Count) +                 //    Rotation.Z
                (sizeof(float) * GeometryParts.Count) +                 //    Rotation.W
                (sizeof(float) * GeometryParts.Count) +                 //    UniformScale.X
                (sizeof(float) * GeometryParts.Count) +                 //    UniformScale.Y
                (sizeof(float) * GeometryParts.Count) +                 //    UniformScale.Z
                (sizeof(byte) * GeometryParts.Count) +                  //    Color.R
                (sizeof(byte) * GeometryParts.Count) +                  //    Color.G
                (sizeof(byte) * GeometryParts.Count) +                  //    Color.B
                                                                        //
                sizeof(uint) +                                  // ChildNodes size
                (sizeof(ulong) * ChildNodes.Count);             // ChildNodes
        }
        public override int ToBytes(byte[] _WriteToBytes, int _Head)
        {
            int Head = _Head;
            Head += base.ToBytes(_WriteToBytes, Head);

            ByteTools.ValueToBytes(BitConverter.GetBytes(ParentID), _WriteToBytes, ref Head);
            ByteTools.ValueToBytes(BitConverter.GetBytes(MetadataID), _WriteToBytes, ref Head);

            ByteTools.ValueToBytes(BitConverter.GetBytes(Convert.ToInt32(GeometryParts.Count)), _WriteToBytes, ref Head);
            for (int i = 0; i < GeometryParts.Count; ++i)
            {
                ByteTools.ValueToBytes(BitConverter.GetBytes(GeometryParts[i].GeometryID), _WriteToBytes, ref Head);

                ByteTools.ValueToBytes(BitConverter.GetBytes((float)GeometryParts[i].Location.X), _WriteToBytes, ref Head);
                ByteTools.ValueToBytes(BitConverter.GetBytes((float)GeometryParts[i].Location.Y), _WriteToBytes, ref Head);
                ByteTools.ValueToBytes(BitConverter.GetBytes((float)GeometryParts[i].Location.Z), _WriteToBytes, ref Head);

                ByteTools.ValueToBytes(BitConverter.GetBytes((float)GeometryParts[i].Rotation.V.X), _WriteToBytes, ref Head);
                ByteTools.ValueToBytes(BitConverter.GetBytes((float)GeometryParts[i].Rotation.V.Y), _WriteToBytes, ref Head);
                ByteTools.ValueToBytes(BitConverter.GetBytes((float)GeometryParts[i].Rotation.V.Z), _WriteToBytes, ref Head);
                ByteTools.ValueToBytes(BitConverter.GetBytes((float)GeometryParts[i].Rotation.W), _WriteToBytes, ref Head);

                ByteTools.ValueToBytes(BitConverter.GetBytes((float)GeometryParts[i].Scale.X), _WriteToBytes, ref Head);
                ByteTools.ValueToBytes(BitConverter.GetBytes((float)GeometryParts[i].Scale.Y), _WriteToBytes, ref Head);
                ByteTools.ValueToBytes(BitConverter.GetBytes((float)GeometryParts[i].Scale.Z), _WriteToBytes, ref Head);

                ByteTools.ValueToBytes(GeometryParts[i].Color.R, _WriteToBytes, ref Head);
                ByteTools.ValueToBytes(GeometryParts[i].Color.G, _WriteToBytes, ref Head);
                ByteTools.ValueToBytes(GeometryParts[i].Color.B, _WriteToBytes, ref Head);
            }

            ByteTools.ValueToBytes(BitConverter.GetBytes(System.Convert.ToInt32(ChildNodes.Count)), _WriteToBytes, ref Head);
            for (int i = 0; i < ChildNodes.Count; ++i)
            {
                ByteTools.ValueToBytes(BitConverter.GetBytes(ChildNodes[i]), _WriteToBytes, ref Head);
            }

            return Head - _Head;
        }
        public override int FromBytes(byte[] _FromBytes, int _Head)
        {
            int Head = _Head;
            Head += base.FromBytes(_FromBytes, Head);

            GeometryParts = new List<GeometryPart>();

            ByteTools.BytesToValue(out ParentID, _FromBytes, ref Head);
            ByteTools.BytesToValue(out MetadataID, _FromBytes, ref Head);

            ByteTools.BytesToValue(out int Size, _FromBytes, ref Head);

            for (int i = 0; i < Size; ++i)
            {
                var NewPart = new GeometryPart();
                ByteTools.BytesToValue(out NewPart.GeometryID, _FromBytes, ref Head);

                NewPart.Location = Vector3.FromBytes(_FromBytes, ref Head, true);
                NewPart.Rotation = Quaternion.FromBytes(_FromBytes, ref Head, true);
                NewPart.Scale = Vector3.FromBytes(_FromBytes, ref Head, true);
                NewPart.Color = Color4.FromBytes(_FromBytes, ref Head);

                GeometryParts.Add(NewPart);
            }

            ByteTools.BytesToValue(out Size, _FromBytes, ref Head);
            ChildNodes = new List<ulong>();
            for (int i = 0; i < Size; ++i)
            {
                ByteTools.BytesToValue(out ulong NewChild, _FromBytes, ref Head);
                ChildNodes.Add(NewChild);
            }

            return Head - _Head;
        }
    }
}
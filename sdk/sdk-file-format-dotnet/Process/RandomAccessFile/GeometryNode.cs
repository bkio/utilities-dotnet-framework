/// Copyright 2022- Burak Kara, All rights reserved.

using System.Collections.Generic;
using Newtonsoft.Json;
using CommonUtilities.Geometry.Triangularization;
using CommonUtilities.Geometry.Utilities;

namespace SDKFileFormat.Process.RandomAccessFile
{
    public class GeometryNode : Node
    {
        public override ENodeType GetNodeType()
        {
            return ENodeType.Geometry;
        }

        public const string LODS_PROPERTY = "lods";

        [JsonProperty(LODS_PROPERTY)]
        public List<LOD> LODs = new List<LOD>();

        public override bool Equals(object _Other)
        {
            if (!base.Equals(_Other)) return false;
            if (!(_Other is GeometryNode Casted)) return false;

            if (LODs.Count != Casted.LODs.Count) return false;

            for (var i = 0; i < LODs.Count; i++)
            {
                if (!LODs[i].Equals(Casted.LODs[i])) return false;
            }

            return true;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override int GetSize()
        {
            var Size = base.GetSize();
            Size += sizeof(byte); //Number of LODs
            foreach (var LOD in LODs)
            {
                Size += LOD.GetSize();
            }
            return Size;
        }
        public override int ToBytes(byte[] _WriteToBytes, int _Head)
        {
            int Head = _Head;
            Head += base.ToBytes(_WriteToBytes, Head);

            ByteTools.ValueToBytes((byte)LODs.Count, _WriteToBytes, ref Head);
            foreach (var LOD in LODs)
            {
                LOD.ToBytes(_WriteToBytes, ref Head);
            }

            return Head - _Head;
        }
        public override int FromBytes(byte[] _FromBytes, int _Head)
        {
            int Head = _Head;
            Head += base.FromBytes(_FromBytes, Head);

            ByteTools.BytesToValue(out byte LODsCount, _FromBytes, ref Head);
            for (byte i = 0; i < LODsCount; ++i)
            {
                LODs.Add(LOD.FromBytes(_FromBytes, ref Head));
            }

            return Head - _Head;
        }
    };
}
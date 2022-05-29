/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using Newtonsoft.Json;
using CommonUtilities.Geometry.Utilities;

namespace SDKFileFormat.Process.RandomAccessFile
{
    public enum ENodeType : byte
    {
        Hierarchy = 0,
        Geometry = 1,
        Metadata = 2
    };

    public abstract class Node
    {
        public const string ID_PROPERTY = "id";
        public const string SIZE_PROPERTY = "size";

        public const ulong UNDEFINED_ID = 0xFFFFFFFF00000000;

        public byte CustomData { get; set; }

        [JsonProperty(ID_PROPERTY)]
        public ulong UniqueID = UNDEFINED_ID;
        [JsonProperty(SIZE_PROPERTY)]
        public int Size = 0;

        public abstract ENodeType GetNodeType();

        public override bool Equals(object _Other)
        {
            if (!(_Other is Node Casted)) return false;
            return Casted.UniqueID == UniqueID;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public virtual int GetSize()
        {
            return sizeof(ulong) + sizeof(int);
        }
        public virtual int ToBytes(byte[] _WriteToBytes, int _Head)
        {
            int Head = _Head;
            ByteTools.ValueToBytes(BitConverter.GetBytes(UniqueID), _WriteToBytes, ref Head);
            ByteTools.ValueToBytes(BitConverter.GetBytes(Size), _WriteToBytes, ref Head);
            return Head - _Head;
        }
        public virtual int FromBytes(byte[] _FromBytes, int _Head)
        {
            int Head = _Head;
            ByteTools.BytesToValue(out UniqueID, _FromBytes, ref Head);
            ByteTools.BytesToValue(out Size, _FromBytes, ref Head);
            return Head - _Head;
        }
    };
}
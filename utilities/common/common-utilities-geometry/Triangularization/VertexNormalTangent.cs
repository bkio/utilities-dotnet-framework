/// Copyright 2022- Burak Kara, All rights reserved.

using Newtonsoft.Json;
using CommonUtilities.Geometry.Numerics;

namespace CommonUtilities.Geometry.Triangularization
{
    public struct VertexNormalTangent
    {
        public const string VERTEX_PROPERTY = "vertex";
        public const string NORMAL_PROPERTY = "normal";
        public const string TANGENT_PROPERTY = "tangent";

        [JsonProperty(VERTEX_PROPERTY)]
        public Vector3 Vertex;

        [JsonProperty(NORMAL_PROPERTY)]
        public Vector3 Normal;

        [JsonProperty(TANGENT_PROPERTY)]
        public Vector3 Tangent;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object _Other)
        {
            if (!(_Other is VertexNormalTangent Casted)) return false;

            if (!Vertex.Equals(Casted.Vertex)) return false;
            if (!Normal.Equals(Casted.Normal)) return false;
            if (!Tangent.Equals(Casted.Tangent)) return false;

            return true;
        }

        public static VertexNormalTangent FromBytes(byte[] _Bytes, ref int _Head)
        {
            return new VertexNormalTangent()
            {
                Vertex = Vector3.FromBytes(_Bytes, ref _Head, true),
                Normal = Vector3.FromBytes(_Bytes, ref _Head, true),
                Tangent = Vector3.FromBytes(_Bytes, ref _Head, true)
            };
        }
    }
}
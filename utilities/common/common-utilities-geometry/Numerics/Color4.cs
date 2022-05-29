/// Copyright 2022- Burak Kara, All rights reserved.

using Newtonsoft.Json;
using CommonUtilities.Geometry.Utilities;

namespace CommonUtilities.Geometry.Numerics
{
    public class Color4
    {
        public const string R_PROPERTY = "r";
        public const string G_PROPERTY = "g";
        public const string B_PROPERTY = "b";
        public const string A_PROPERTY = "a";

        [JsonProperty(R_PROPERTY)]
        public byte R = 0;

        [JsonProperty(G_PROPERTY)]
        public byte G = 0;

        [JsonProperty(B_PROPERTY)]
        public byte B = 0;

        [JsonProperty(B_PROPERTY)]
        public byte A = 255;

        public Color4() { }
        public Color4(Color4 _Other)
        {
            R = _Other.R;
            G = _Other.G;
            B = _Other.B;
            A = _Other.A;
        }
        public Color4(byte _R, byte _G, byte _B, byte _A)
        {
            R = _R;
            G = _G;
            B = _B;
            A = _A;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object _Other)
        {
            if (!(_Other is Color4 Casted)) return false;

            if (R != Casted.R) return false;
            if (G != Casted.G) return false;
            if (B != Casted.B) return false;
            if (A != Casted.A) return false;

            return true;
        }

        public static Color4 FromBytes(byte[] _Bytes, ref int _Head)
        {
            var Result = new Color4();
            ByteTools.BytesToValue(out Result.R, _Bytes, ref _Head);
            ByteTools.BytesToValue(out Result.G, _Bytes, ref _Head);
            ByteTools.BytesToValue(out Result.B, _Bytes, ref _Head);
            ByteTools.BytesToValue(out Result.A, _Bytes, ref _Head);
            return Result;
        }
    }
}
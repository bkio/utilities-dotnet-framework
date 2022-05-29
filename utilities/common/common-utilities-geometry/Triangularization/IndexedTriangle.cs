/// Copyright 2022- Burak Kara, All rights reserved.

using CommonUtilities.Geometry.Numerics;
using Newtonsoft.Json;

namespace CommonUtilities.Geometry.Triangularization
{
    public class IndexedTriangle
    {
        public Index3 Index;

        public const string I_PROPERTY = "i";
        public const string J_PROPERTY = "j";
        public const string K_PROPERTY = "k";

        [JsonProperty(I_PROPERTY)]
        public int I { get => Index.I; set => Index.I = value; }
        [JsonProperty(J_PROPERTY)]
        public int J { get => Index.J; set => Index.J = value; }
        [JsonProperty(K_PROPERTY)]
        public int K { get => Index.K; set => Index.K = value; }

        public IndexedTriangle(int _Index0, int _Index1, int _Index2)
        {
            Index = new Index3(_Index0, _Index1, _Index2);
        }

        public int this[int _Index] { get => Index[_Index]; set => Index[_Index] = value; }

        public override string ToString()
        {
            return $"Index= {Index}";
        }

        public bool IsDegenerated => I == J || J == K || K == I;

        public bool IsEqual(IndexedTriangle _Other)
        {
            if (I == _Other.I && J == _Other.J && K == _Other.K)
                return true;
            if (I == _Other.J && J == _Other.K && K == _Other.I)
                return true;
            return I == _Other.K && J == _Other.I && K == _Other.J;
        }

        public static int Compare(IndexedTriangle _A, IndexedTriangle _B)
        {
            if (_A.I != _B.I)
                return _A.I.CompareTo(_B.I);

            if (_A.J != _B.J)
                return _A.J.CompareTo(_B.J);

            return _A.K.CompareTo(_B.K);
        }

        public void Map(int[] _Map)
        {
            Index.I = _Map[Index.I];
            Index.J = _Map[Index.J];
            Index.K = _Map[Index.K];
        }

        public int[] ToArray()
        {
            int[] Map = { Index.I, Index.J, Index.K };
            return Map;
        }

        public void AddToIndex(int _Index)
        {
            Index.Add(_Index);
        }
    }
}
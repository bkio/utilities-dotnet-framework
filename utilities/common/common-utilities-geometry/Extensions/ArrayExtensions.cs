/// Copyright 2022- Burak Kara, All rights reserved.

using System;

namespace CommonUtilities.Geometry.Extensions
{
    public static class ArrayExtensions
    {
        public static void Clear(this Array _Array)
        {
            Array.Clear(_Array, 0, _Array.Length);
        }

        public static void Clear<T>(this T[] _Array)
        {
            Array.Clear(_Array, 0, _Array.Length);
        }

        public static T[] AddRange<T>(this T[] _Array, T[] _Other)
        {
            var OtherSize = _Other.Length;
            var OldSize = _Array.Length;
            Array.Resize(ref _Array, OldSize + OtherSize);
            Array.ConstrainedCopy(_Other, 0, _Array, OldSize, OtherSize);
            return _Array;
        }

        public static bool FindRange<T>(this T[] _Array, T _Value, ComparisonComparer<T> _Comparer, out int _StartIndex, out int _EndIndex)
        {
            var OtherEdgeIndex = Array.BinarySearch(_Array, _Value, _Comparer);
            if (OtherEdgeIndex < 0)
            {
                _StartIndex = -1;
                _EndIndex = -1;
                return false;
            }
            _StartIndex = OtherEdgeIndex - 1;
            for (; _StartIndex >= 0; _StartIndex--)
            {
                if (_Comparer.Compare(_Array[_StartIndex], _Value) != 0)
                    break;
            }
            _StartIndex++;
            _EndIndex = OtherEdgeIndex + 1;
            for (; _EndIndex < _Array.Length; _EndIndex++)
            {
                if (_Comparer.Compare(_Array[_EndIndex], _Value) != 0)
                    break;
            }
            _EndIndex--;
            return true;
        }

        public static T[] RemoveAll<T>(this T[] _Array, Predicate<T> _Match)
        {
            int NewIndex = 0;
            int OldSize = _Array.Length;
            for (int OldIndex = 0; OldIndex < OldSize; OldIndex++)
            {
                var Item = _Array[OldIndex];
                if (!_Match(Item))
                    _Array[NewIndex++] = Item;
            }
            Array.Resize(ref _Array, NewIndex);
            return _Array;
        }

        public static T[] RemoveAllByIndex<T>(this T[] _Array, Predicate<int> _Match)
        {
            int NewIndex = 0;
            int OldSize = _Array.Length;
            for (int OldIndex = 0; OldIndex < OldSize; OldIndex++)
            {
                if (!_Match(OldIndex))
                    _Array[NewIndex++] = _Array[OldIndex];
            }
            Array.Resize(ref _Array, NewIndex);
            return _Array;
        }

        public static int GetClosestIndex<T>(this T[] _Array, T _Value)
        {
            var Index = Array.BinarySearch(_Array, _Value);
            if (Index < 0)
            {
                Index = ~Index;
                if (Index < 0 || Index >= _Array.Length)
                    Index = _Array.Length / 2;
            }
            return Index;
        }

        public static T GetClosestValue<T>(this T[] _Array, T _Value)
        {
            return _Array[_Array.GetClosestIndex(_Value)];
        }

        public static void ForEach(this Array _Array, Action<Array, int[]> _Action)
        {
            if (_Array.LongLength == 0)
                return;
            var Traverser = new ArrayTraverse(_Array);
            do
                _Action(_Array, Traverser.Position);
            while (Traverser.Step());
        }
    }
}
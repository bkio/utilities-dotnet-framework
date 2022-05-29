/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CommonUtilities.Geometry.Tools;

namespace CommonUtilities.Geometry.Extensions
{
    public static class ListExtensions
    {
        public static bool AddUnique<T>(this List<T> _List, T _Element) where T : IComparable<T>
        {
            var n = _List.Count;
            if (n <= 1)
            {
                if (n == 0)
                {
                    _List.Add(_Element);
                    return true;
                }
                if (_List[0].CompareTo(_Element) == 0)
                    return false;
            }
            var Index = _List.BinarySearch(_Element);
            if (Index >= 0)
                return false;

            _List.Insert(~Index, _Element);
            return true;
        }

        public static void RemoveRest<T>(this List<T> _List, int _FromIndex)
        {
            if (_FromIndex >= _List.Count)
                return;
            _List.RemoveRange(_FromIndex, _List.Count - _FromIndex);
        }

        public static T First<T>(this IList<T> _List)
        {
            return _List.Count <= 0 ? default : _List[0];
        }

        public static T SecondFirst<T>(this IList<T> _List)
        {
            return _List.Count <= 1 ? default : _List[1];
        }

        public static T Last<T>(this IList<T> _List)
        {
            return _List.Count <= 0 ? default : _List[_List.Count - 1];
        }

        public static T SecondLast<T>(this IList<T> _List)
        {
            return _List.Count <= 1 ? default : _List[_List.Count - 2];
        }

        public static bool IsEmpty<T>(this IList<T> _List)
        {
            return _List.Count == 0;
        }

        public static void Initialize<T>(this IList<T> _List, T value)
        {
            for (var i = _List.Count - 1; i >= 0; i--)
                _List[i] = value;
        }

        public static void Shuffle<T>(this IList<T> _List, Random _Random = null)
        {
            var n = _List.Count;
            if (_Random == null)
                _Random = new Random(n);
            for (var i = 0; i < n; i++)
            {
                var j = _Random.Next(i, n);
                _List.Swap(i, j);
            }
        }

        public static void Split<T>(this IList<T> _List, IList<T> _List1, IList<T> _List2, double fraction)
        {
            var numTrain = (int)(_List.Count * fraction);
            var i = 0;
            for (; i < numTrain; i++)
                _List1.Add(_List[i]);

            for (; i < _List.Count; i++)
                _List2.Add(_List[i]);
            _List.Clear();
        }

        public static List<T> Split<T>(this List<T> _List, double _Fraction)
        {
            var n = (int)(_List.Count * (1 - _Fraction));
            var Result = new List<T>();
            for (var i = n; i < _List.Count; i++)
                Result.Add(_List[i]);
            _List.RemoveRest(n);
            return Result;
        }

        public static void Shuffle<T>(this T[] _Array, Random _Random = null)
        {
            var n = _Array.Length;
            if (_Random == null)
                _Random = new Random(n);
            for (var i = 0; i < n; i++)
            {
                var j = _Random.Next(i, n);
                CoreTool.Swap(ref _Array[i], ref _Array[j]);
            }
        }

        public static void Swap<T>(this IList<T> _List, int _Index1, int _Index2)
        {
            var Tmp = _List[_Index1];
            _List[_Index1] = _List[_Index2];
            _List[_Index2] = Tmp;
        }

        public static void Move<T>(this IList<T> _List, int _OldIndex, int _NewIndex)
        {
            if (_OldIndex == -1 || _OldIndex == _NewIndex)
                return;
            var Item = _List[_OldIndex];
            _List.RemoveAt(_OldIndex);
            if (_NewIndex > _OldIndex)
                _NewIndex--;
            _List.Insert(_NewIndex, Item);
        }

        public static void Move<T>(this IList<T> _List, T _Item, int _NewIndex)
        {
            _List.Move(_List.IndexOf(_Item), _NewIndex);
        }

        public static void Move<T>(this IList<T> _List, T _Item, T _Neighbor, bool _After = false)
        {
            var Index = _List.IndexOf(_Neighbor);
            if (Index == -1)
                return;
            if (_After)
                ++Index;
            _List.Move(_List.IndexOf(_Item), Index);
        }

        public static List<T> Clone<T>(this IList<T> _List)
        {
            return new List<T>(_List);
        }

        public static bool IsGenericList(this object _O)
        {
            var bIsGenericList = false;

            var OType = _O.GetType();

            if (OType.IsGenericType && OType.GetGenericTypeDefinition() == typeof(List<>))
                bIsGenericList = true;

            return bIsGenericList;
        }

        public static bool IsEqual(this IList _List, IList _Other)
        {
            if (_List == null && _Other == null)
                return true;
            if (_List == null || _Other == null)
                return false;
            if (_List.Count != _Other.Count)
                return false;

            for (var i = 0; i < _List.Count; i++)
                if (!_List[i].Equals(_Other[i]))
                    return false;

            return true;
        }

        public static void Resize<T>(this List<T> _List, int _NewSize, T _Element)
        {
            var Delta = _NewSize - _List.Count;
            if (Delta < 0)
                _List.RemoveRange(_NewSize, -Delta);
            else if (Delta > 0)
            {
                if (_NewSize > _List.Capacity)
                    _List.Capacity = _NewSize;
                _List.AddRange(Enumerable.Repeat(_Element, Delta));
            }
        }

        public static void EnsureSize<T>(this List<T> _List, int _MinimumSize, T _Element)
        {
            if (_List.Count < _MinimumSize)
                _List.Resize(_MinimumSize, _Element);
        }

        public static void Resize<T>(this List<T> _List, int _NewSize) where T : new() { Resize(_List, _NewSize, new T()); }
        public static void EnsureSize<T>(this List<T> _List, int _NewSize) where T : new() { EnsureSize(_List, _NewSize, new T()); }

        public static void RemoveDuplicates<T>(this List<T> _List) where T : IComparable<T>
        {
            if (_List.Count <= 1)
                return;

            _List.Sort((a, b) => b.CompareTo(a));
            var UniqueList = new List<T>(_List.Count) { _List[0] };

            for (var index = 1; index < _List.Count; index++)
            {
                var item = _List[index];
                if (_List[index - 1].CompareTo(item) != 0)
                    UniqueList.Add(item);
            }
            _List.Clear();
            _List.AddRange(UniqueList);
        }

        public static bool HasDuplicates<T>(this List<T> _List) where T : IComparable<T>
        {
            if (_List.Count <= 1)
                return false;

            _List.Sort((a, b) => b.CompareTo(a));
            for (var index = 1; index < _List.Count; index++)
            {
                if (_List[index - 1].CompareTo(_List[index]) == 0)
                    return true;
            }
            return false;
        }
    }
}
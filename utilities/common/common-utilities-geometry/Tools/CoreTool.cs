/// Copyright 2022- Burak Kara, All rights reserved.

using System;

namespace CommonUtilities.Geometry.Tools
{
    public static class CoreTool
    {
        public static void Swap<T>(ref T _A, ref T _B) { var Tmp = _A; _A = _B; _B = Tmp; }

        public static void Order<T>(ref T _A, ref T _B) where T : IComparable { if (_A.CompareTo(_B) > 0) Swap(ref _A, ref _B); }

        public static void Dispose<T>(ref T _Disposable) where T : class, IDisposable
        {
            if (_Disposable != null)
            {
                _Disposable.Dispose();
                _Disposable = null;
            }
        }

        public static void Clear<T>(T[] _Array) { Array.Clear(_Array, 0, _Array.Length); }

        public static T[] Create<T>(int _Count, T _Value)
        {
            var Arr = new T[_Count];
            Initialize(Arr, _Value);
            return Arr;
        }

        public static T[,] Create<T>(int _M, int _N, T _Value)
        {
            var Arr = new T[_M, _N];
            Initialize(Arr, _Value);
            return Arr;
        }

        public static T[,,] Create<T>(int _M, int _N, int _O, T _Value)
        {
            var Arr = new T[_M, _N, _O];
            Initialize(Arr, _Value);
            return Arr;
        }

        public static T Clone<T>(T _Source) where T : class, ICloneable
        {
            if (_Source == null)
                return default;
            return (T)_Source.Clone();
        }

        public static T[] Clone<T>(T[] _Array)
        {
            if (_Array == null)
                return null;
            return (T[])_Array.Clone();
        }

        public static T[,] Clone<T>(T[,] _Array)
        {
            if (_Array == null)
                return null;
            return (T[,])_Array.Clone();
        }

        public static T[,,] Clone<T>(T[,,] _Array)
        {
            if (_Array == null)
                return null;
            return (T[,,])_Array.Clone();
        }

        public static void Copy<T>(T[] _Dest, T[] _Src)
        {
            if (_Src == null)
                return;
            Buffer.BlockCopy(_Src, 0, _Dest, 0, Math.Min(Buffer.ByteLength(_Dest), Buffer.ByteLength(_Src)) * sizeof(byte));
        }

        public static void Copy<T>(T[,] _Dest, T[,] _Src)
        {
            if (_Src == null)
                return;

            for (var i = _Src.GetLength(0) - 1; i >= 0; i--)
                for (var j = _Src.GetLength(1) - 1; j >= 0; j--)
                    _Dest[i, j] = _Src[i, j];
        }

        public static void Copy<T>(T[,,] _Dest, T[,,] _Src)
        {
            if (_Src == null)
                return;

            for (var i = _Src.GetLength(0) - 1; i >= 0; i--)
                for (var j = _Src.GetLength(1) - 1; j >= 0; j--)
                    for (var k = _Src.GetLength(2) - 1; k >= 0; k--)
                        _Dest[i, j, k] = _Src[i, j, k];
        }

        public static void Initialize<T>(bool[] _Array, bool _Value)
        {
            if (!_Value)
                Array.Clear(_Array, 0, _Array.Length);
            else
                Initialize<bool>(_Array, _Value);
        }

        public static void Initialize<T>(T[] _Array, T _Value)
        {
            for (var i = _Array.Length - 1; i >= 0; i--)
                _Array[i] = _Value;
            return;
        }

        public static void Initialize(bool[,] _Array, bool _Value)
        {
            if (!_Value)
                Array.Clear(_Array, 0, _Array.Length);
            else
                Initialize<bool>(_Array, _Value);
        }

        public static void Initialize<T>(T[,] _Array, T _Value)
        {
            for (var i = _Array.GetLength(0) - 1; i >= 0; i--)
                for (var j = _Array.GetLength(1) - 1; j >= 0; j--)
                    _Array[i, j] = _Value;
        }

        public static void Initialize<T>(T[,,] _Array, T _Value)
        {
            for (var i = _Array.GetLength(0) - 1; i >= 0; i--)
                for (var j = _Array.GetLength(1) - 1; j >= 0; j--)
                    for (var k = _Array.GetLength(2) - 1; k >= 0; k--)
                        _Array[i, j, k] = _Value;
        }
    }
}
/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using CommonUtilities.Geometry.Numerics;

namespace CommonUtilities.Geometry.Triangularization
{
    public partial class SortGrid3<T> : RegularGrid3 where T : IComparable<T>
    {
        private List<T>[,,] Values;

        public SortGrid3(int _NumCells, Range3 _Range)
          : base(GetNodeSize(_Range, _NumCells), _Range, Angle.Zero)
        {
            MakeBuffer();
        }

        public void Add(int I, int J, int K, T _Value)
        {
            if (Values[I, J, K] == null)
            {
                Values[I, J, K] = new List<T>();
            }
            Values[I, J, K].Add(_Value);
        }

        public void Add(Index3 _Cell, T _Value)
        {
            Add(_Cell.I, _Cell.J, _Cell.K, _Value);
        }

        public List<T> GetResult(Index3 cell) { return Values[cell.I, cell.J, cell.K]; }

        private void MakeBuffer() { Values = CreateCellBuffer<List<T>>(); }

        static Index3 GetNodeSize(Range3 _Range, int _NumCells)
        {
            var Delta = _Range.Delta;
            var A = Math.Pow(_NumCells / Delta.Volume, 1.0 / 3.0);
            Delta.Mul(A);
            return Delta.Ceiling;
        }
    }
}
/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using CommonUtilities.Geometry.Numerics;

namespace CommonUtilities.Geometry.Triangularization
{
    public class PointSortGrid3 : SortGrid3<int>
    {
        public PointSortGrid3(IEnumerable<Vector3> _Points)
          : this(_Points.Count(), Range3.Create(_Points, 1))
        {
        }

        public PointSortGrid3(int _NumCells, Range3 _Range)
          : base(_NumCells, _Range)
        {
        }

        public bool AddUnique(Vector3 _Point, IList<Vector3> _Result, out int _NewIndex, double _Tolerance = -1)
        {
            if (!GetCellByPoint(_Point, out var Cell))
            {
                _NewIndex = -1;
                return false;
            }
            var Indexes = GetResult(Cell);
            if (Indexes != null)
            {
                foreach (var Ix in Indexes)
                {
                    if (_Tolerance < 0)
                    {
                        if (!_Result[Ix].IsEqual(_Point))
                            continue;
                    }
                    else
                    {
                        if (!_Result[Ix].IsEqual(_Point, _Tolerance))
                            continue;
                    }
                    _NewIndex = Ix;
                    return false;
                }
            }
            _NewIndex = _Result.Count;
            Add(Cell, _NewIndex);
            _Result.Add(_Point);
            return true;
        }

        public List<Vector3> CreateAndAddUnique(IEnumerable<Vector3> _InputPoint, int[] _Map, double _Tolerance = -1)
        {
            int Index = 0;
            var Results = new List<Vector3>();
            var PointDictionary = new Dictionary<Index3, List<PointIndexPair>>();

            foreach (var Point in _InputPoint)
            {
                Index3 Key = Point.Floor;
                PointIndexPair Pair = new PointIndexPair()
                {
                    Vertex = Point,
                    Index = 0
                };

                if (_Tolerance > 0 && PointDictionary.ContainsKey(Key))
                {
                    List<PointIndexPair> PointList = PointDictionary[Key];
                    bool bFound = false;
                    for (int i = 0; i < PointList.Count; ++i)
                    {
                        float Distance = GetDistance(PointList[i].Vertex, Point);

                        if (Distance <= _Tolerance)
                        {
                            _Map[Index] = PointList[i].Index;
                            bFound = true;
                            break;
                        }
                    }
                    if (!bFound)
                    {
                        Pair.Index = Results.Count;
                        PointList.Add(Pair);
                        Results.Add(Point);
                        _Map[Index] = Results.Count - 1;
                    }
                }
                else
                {
                    Pair.Index = Results.Count;
                    PointDictionary.Add(Key, new List<PointIndexPair>() { Pair });
                    Results.Add(Point);
                    _Map[Index] = Results.Count - 1;
                }

                Index++;
            }
            return Results;
        }

        private float GetDistance(Vector3 _Point1, Vector3 _Point2)
        {
            double DeltaX = _Point1.X - _Point2.X;
            double DeltaY = _Point1.Y - _Point2.Y;
            double DeltaZ = _Point1.Z - _Point2.Z;

            return (float)Math.Sqrt(DeltaX * DeltaX + DeltaY * DeltaY + DeltaZ * DeltaZ);
        }
    }

    public struct PointIndexPair
    {
        public Vector3 Vertex;
        public int Index;
    }
}
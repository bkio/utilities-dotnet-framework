/// Copyright 2022- Burak Kara, All rights reserved.

namespace CommonUtilities.Geometry.Numerics
{
    public interface IMatrix
    {
        int NumRows { get; }
        int NumCols { get; }
        bool IsSymmetric { get; }
        double GetAt(int _Row, int _Col);
    }
}

/// Copyright 2022- Burak Kara, All rights reserved.

using CommonUtilities.Geometry.Numerics;

namespace CommonUtilities.Geometry.Triangularization
{
    public interface IShape3
    {
        void ExpandRange(ref Range3 _Range);
        void ExpandRangeZ(ref Range1 _Range);
    }
}
/// Copyright 2022- Burak Kara, All rights reserved.

using System.Collections.Generic;
using CommonUtilities.Geometry.Numerics;

namespace CommonUtilities.Geometry.Triangularization
{
    public interface IPolygon3
    {
        IEnumerable<Vector3> Points { get; }
    }
}
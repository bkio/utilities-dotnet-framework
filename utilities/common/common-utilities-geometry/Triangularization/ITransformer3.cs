/// Copyright 2022- Burak Kara, All rights reserved.

using CommonUtilities.Geometry.Numerics;

namespace CommonUtilities.Geometry.Triangularization
{
    public interface ITransformer3
    {
        void Transform(ref Vector3 _Point, bool _AsVector = false);
    }
}
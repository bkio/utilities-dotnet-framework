/// Copyright 2022- Burak Kara, All rights reserved.

using System.Collections.Generic;

namespace CommonUtilities.Geometry.Numerics
{
    public interface IValueAdder
    {
        void Clear();
        void Add(double _Value);
        void AddSimple(double _Value);
        void Add(IEnumerable<double> _Value);
    }
}
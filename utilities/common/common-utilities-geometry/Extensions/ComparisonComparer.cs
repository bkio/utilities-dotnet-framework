/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;

namespace CommonUtilities.Geometry.Extensions
{
    public class ComparisonComparer<T> : IComparer<T>
    {
        private readonly Comparison<T> ComparisonObject;
        public ComparisonComparer(Comparison<T> _ComparisonObject)
        {
            ComparisonObject = _ComparisonObject;
        }

        public int Compare(T X, T Y)
        {
            return ComparisonObject(X, Y);
        }
    }
}
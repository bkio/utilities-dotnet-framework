/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using Newtonsoft.Json;
using CommonUtilities.Geometry.Numerics;

namespace CommonUtilities.Geometry.Triangularization
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Shape : IShape3
    {
        public const string NAME_PROPERTY = "name";

        [JsonProperty(NAME_PROPERTY)]
        public string Name;

        public Range3 Range { get { var _Range = Range3.Empty; ExpandRange(ref _Range); return _Range; } }

        protected Shape()
        {
        }

        protected Shape(string _Name)
        {
            Name = _Name;
        }

        protected Shape(Shape _Source)
        {
            Name = _Source.Name;
        }

        public virtual void ExpandRange(ref Range3 _Range) { throw new NotImplementedException(); }
        public virtual void ExpandRangeZ(ref Range1 _Range) { throw new NotImplementedException(); }
        public virtual void Transform(ITransformer3 _Transformer) { throw new NotImplementedException(); }
        public virtual void Expand(IValueAdder _Adder) { throw new NotImplementedException(); }
    }
}
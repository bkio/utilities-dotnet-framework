/// Copyright 2022- Burak Kara, All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace CommonUtilities.Geometry.Extensions
{
    public static class ObjectExtensions
    {
        public static T Copy<T>(this T _Original)
        {
            return (T)Copy((object)_Original);
        }

        private static readonly MethodInfo CloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        private static bool IsPrimitive(this Type _Type)
        {
            if (_Type == typeof(string))
                return true;

            return _Type.IsValueType;
        }

        public static object Copy(this object _OriginalObject)
        {
            return InternalCopy(_OriginalObject, new Dictionary<object, object>(new ReferenceEqualityComparer()));
        }

        private static object InternalCopy(object _OriginalObject, IDictionary<object, object> _Visited)
        {
            if (_OriginalObject == null)
                return null;

            var TypeToReflect = _OriginalObject.GetType();

            if (IsPrimitive(TypeToReflect))
                return _OriginalObject;

            if (_Visited.ContainsKey(_OriginalObject))
                return _Visited[_OriginalObject];

            if (typeof(Delegate).IsAssignableFrom(TypeToReflect))
                return null;

            var CloneObject = CloneMethod.Invoke(_OriginalObject, null);
            if (TypeToReflect.IsArray)
            {
                var arrayType = TypeToReflect.GetElementType();
                if (!IsPrimitive(arrayType))
                {
                    var ClonedArray = (Array)CloneObject;
                    ClonedArray.ForEach((Arr, Indices) => Arr.SetValue(InternalCopy(ClonedArray.GetValue(Indices), _Visited), Indices));
                }

            }

            _Visited.Add(_OriginalObject, CloneObject);

            CopyFields(_OriginalObject, _Visited, CloneObject, TypeToReflect);
            RecursiveCopyBaseTypePrivateFields(_OriginalObject, _Visited, CloneObject, TypeToReflect);

            return CloneObject;
        }

        private static void RecursiveCopyBaseTypePrivateFields(object _OriginalObject, IDictionary<object, object> _Visited, object _CloneObject, Type _TypeToReflect)
        {
            if (_TypeToReflect.BaseType != null)
            {
                RecursiveCopyBaseTypePrivateFields(_OriginalObject, _Visited, _CloneObject, _TypeToReflect.BaseType);
                CopyFields(_OriginalObject, _Visited, _CloneObject, _TypeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, Info => Info.IsPrivate);
            }
        }

        private static void CopyFields(object _OriginalObject, IDictionary<object, object> _Visited, object _CloneObject, Type _TypeToReflect, BindingFlags _BindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool> _Filter = null)
        {
            foreach (var FieldInfo in _TypeToReflect.GetFields(_BindingFlags))
            {
                if (_Filter != null && _Filter(FieldInfo) == false)
                    continue;
                if (IsPrimitive(FieldInfo.FieldType))
                    continue;

                FieldInfo.SetValue(_CloneObject, InternalCopy(FieldInfo.GetValue(_OriginalObject), _Visited));
            }
        }
    }

    internal class ReferenceEqualityComparer : EqualityComparer<object>
    {
        public override bool Equals(object X, object Y)
        {
            return ReferenceEquals(X, Y);
        }
        public override int GetHashCode(object _Object)
        {
            if (_Object == null)
                return 0;
            return _Object.GetHashCode();
        }
    }

    internal class ArrayTraverse
    {
        public int[] Position;
        private readonly int[] MaxLengths;

        public ArrayTraverse(Array _Array)
        {
            MaxLengths = new int[_Array.Rank];
            for (var i = 0; i < _Array.Rank; i++)
            {
                MaxLengths[i] = _Array.GetLength(i) - 1;
            }
            Position = new int[_Array.Rank];
        }

        public bool Step()
        {
            for (var i = 0; i < Position.Length; i++)
            {
                if (Position[i] < MaxLengths[i])
                {
                    Position[i]++;
                    for (var j = 0; j < i; j++)
                    {
                        Position[j] = 0;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}